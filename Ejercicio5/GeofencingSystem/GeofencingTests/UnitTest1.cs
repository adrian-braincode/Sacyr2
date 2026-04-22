using GeofencingLogic;
using Moq;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace GeofencingTests;

/// <summary>
/// Suite completa de pruebas para el sistema de geofencing.
/// Cubre lógica geométrica, flujo de alertas y casos de borde según SPECIFICATION.md.
/// Utiliza xUnit con theories para validar criterios de aceptación y fallos de seguridad.
/// </summary>
public class GeofencingTests
{
    private readonly GeofencingService _geofencingService = new();

    #region Tests de Lógica Core (Distancia Euclidiana)

    [Theory]
    [InlineData(0, 0, 0, 0, 0)] // Mismo punto
    [InlineData(0, 0, 0, 1, 111195)] // 1 grado longitud ≈ 111km
    [InlineData(0, 0, 1, 0, 111195)] // 1 grado latitud ≈ 111km
    [InlineData(40.4168, -3.7038, 40.4168, -3.7038, 0)] // Madrid mismo punto
    public void CalculateDistance_ReturnsCorrectDistance(double lat1, double lon1, double lat2, double lon2, double expectedMeters)
    {
        // Arrange
        var pointA = new Point(lat1, lon1);
        var pointB = new Point(lat2, lon2);

        // Act
        double distance = _geofencingService.CalculateDistance(pointA, pointB);

        // Assert
        Assert.InRange(distance, expectedMeters - 10, expectedMeters + 10); // Tolerancia de 10m por aproximación
    }

    [Theory]
    [InlineData(91, 0)] // Latitud inválida
    [InlineData(-91, 0)]
    [InlineData(0, 181)] // Longitud inválida
    [InlineData(0, -181)]
    public void CalculateDistance_ThrowsExceptionForInvalidCoordinates(double lat, double lon)
    {
        // Arrange
        var invalidPoint = new Point(lat, lon);
        var validPoint = new Point(0, 0);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _geofencingService.CalculateDistance(invalidPoint, validPoint));
        Assert.Throws<ArgumentException>(() => _geofencingService.CalculateDistance(validPoint, invalidPoint));
    }

    #endregion

    #region Tests de Verificación de Zona de Peligro

    [Theory]
    [InlineData(0, 0, 50, 0, 0, true)] // Centro exacto
    [InlineData(0, 0, 50, 0, 0.00044, true)] // ~49m (dentro)
    [InlineData(0, 0, 50, 0, 0.00046, false)] // ~51m (fuera)
    [InlineData(40.4168, -3.7038, 50, 40.4168, -3.7038, true)] // Madrid centro
    public void IsInDangerZone_ReturnsCorrectResult(double centerLat, double centerLon, double radius, double currentLat, double currentLon, bool expectedInZone)
    {
        // Arrange
        var center = new Point(centerLat, centerLon);
        var current = new Point(currentLat, currentLon);

        // Act
        bool isInZone = _geofencingService.IsInDangerZone(center, radius, current);

        // Assert - Crítico para seguridad: debe alertar correctamente
        Assert.Equal(expectedInZone, isInZone);
    }

    [Theory]
    [InlineData(0, 0, 0, 0, 0, true)] // Radio cero, centro exacto
    [InlineData(0, 0, 0, 0, 0.00001, false)] // Fuera del centro
    public void IsInDangerZone_HandlesZeroRadius(double centerLat, double centerLon, double radius, double currentLat, double currentLon, bool expectedInZone)
    {
        // Arrange
        var center = new Point(centerLat, centerLon);
        var current = new Point(currentLat, currentLon);

        // Act
        bool isInZone = _geofencingService.IsInDangerZone(center, radius, current);

        // Assert
        Assert.Equal(expectedInZone, isInZone);
    }

    #endregion

    #region Tests de Escenarios de Límite

    [Fact]
    public void IsInDangerZone_Limit49_9Meters_ShouldAlert()
    {
        // Arrange - Límite inferior: 49.9m debe alertar
        var center = new Point(0, 0);
        var position49_9m = new Point(0, 0.0004488); // Aproximadamente 49.9m

        // Act
        bool isInZone = _geofencingService.IsInDangerZone(center, 50, position49_9m);

        // Assert - Crítico: debe alertar en límite inferior
        Assert.True(isInZone, "Debe alertar cuando está a 49.9m del centro");
    }

    [Fact]
    public void IsInDangerZone_Limit50_1Meters_ShouldNotAlert()
    {
        // Arrange - Límite superior: 50.1m no debe alertar
        var center = new Point(0, 0);
        var position50_1m = new Point(0, 0.0004507); // Aproximadamente 50.1m

        // Act
        bool isInZone = _geofencingService.IsInDangerZone(center, 50, position50_1m);

        // Assert - Crítico: no debe alertar fuera del límite
        Assert.False(isInZone, "No debe alertar cuando está a 50.1m del centro");
    }

    [Fact]
    public void ProcessPosition_FiltersSuddenJump()
    {
        // Arrange - Simular salto brusco por error GPS (>10m/s)
        var mockAlert = new Mock<IAlertService>();
        var alertService = new AlertService(mockAlert.Object, _geofencingService);

        var center = new Point(0, 0);
        var previous = new Point(0, 0);
        var current = new Point(0, 0.001); // ~111m de salto en 1 segundo = 111m/s (irreal)

        // Act
        alertService.ProcessPosition(center, 50, previous, current, 1);

        // Assert - No debe alertar por salto brusco
        mockAlert.Verify(a => a.SendAlert(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region Tests de Flujo de Alerta y Notificaciones

    [Fact]
    public void ProcessPosition_SendsAlertWhenEnteringDangerZone()
    {
        // Arrange
        var mockAlert = new Mock<IAlertService>();
        var alertService = new AlertService(mockAlert.Object, _geofencingService);

        var center = new Point(0, 0);
        var current = new Point(0, 0.00044); // Dentro de 50m

        // Act
        alertService.ProcessPosition(center, 50, null, current, 1);

        // Assert - Debe enviar alerta
        mockAlert.Verify(a => a.SendAlert(It.Is<string>(msg => msg.Contains("Alerta de seguridad"))), Times.Once);
    }

    [Fact]
    public void ProcessPosition_DoesNotSendAlertWhenOutsideZone()
    {
        // Arrange
        var mockAlert = new Mock<IAlertService>();
        var alertService = new AlertService(mockAlert.Object, _geofencingService);

        var center = new Point(0, 0);
        var current = new Point(0, 0.00046); // Fuera de 50m

        // Act
        alertService.ProcessPosition(center, 50, null, current, 1);

        // Assert - No debe alertar
        mockAlert.Verify(a => a.SendAlert(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ProcessPosition_IgnoresNullCoordinates()
    {
        // Arrange
        var mockAlert = new Mock<IAlertService>();
        var alertService = new AlertService(mockAlert.Object, _geofencingService);

        var center = new Point(0, 0);

        // Act
        alertService.ProcessPosition(center, 50, null, null, 1);

        // Assert - No debe alertar por coordenadas nulas
        mockAlert.Verify(a => a.SendAlert(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ProcessPosition_IgnoresInvalidCoordinates()
    {
        // Arrange
        var mockAlert = new Mock<IAlertService>();
        var alertService = new AlertService(mockAlert.Object, _geofencingService);

        var center = new Point(0, 0);
        var invalidPosition = new Point(91, 0); // Latitud inválida

        // Act
        alertService.ProcessPosition(center, 50, null, invalidPosition, 1);

        // Assert - No debe alertar por coordenadas inválidas
        mockAlert.Verify(a => a.SendAlert(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region Tests de Integración y Rendimiento

    [Fact]
    public void ProcessPosition_HandlesMultipleOperators()
    {
        // Arrange - Simular 100 operarios procesando posiciones
        var mockAlert = new Mock<IAlertService>();
        var alertService = new AlertService(mockAlert.Object, _geofencingService);

        var center = new Point(0, 0);
        var positions = new List<Point>();

        // Generar 100 posiciones aleatorias
        var random = new Random(42); // Seed para reproducibilidad
        for (int i = 0; i < 100; i++)
        {
            positions.Add(new Point(random.NextDouble() * 180 - 90, random.NextDouble() * 360 - 180));
        }

        // Act & Measure time
        var stopwatch = Stopwatch.StartNew();
        foreach (var pos in positions)
        {
            alertService.ProcessPosition(center, 50, null, pos, 1);
        }
        stopwatch.Stop();

        // Assert - Debe procesar en menos de 1 segundo total
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, $"Procesamiento tomó {stopwatch.ElapsedMilliseconds}ms, debe ser <1000ms");
    }

    #endregion
}
