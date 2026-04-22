namespace GeofencingLogic;

/// <summary>
/// Representa un punto geográfico con coordenadas GPS.
/// Se utiliza para calcular distancias y verificar zonas de peligro.
/// </summary>
public record Point(double Latitude, double Longitude);

/// <summary>
/// Servicio principal de geofencing para calcular distancias y verificar entradas en zonas de peligro.
/// Implementa lógica geométrica pura sin dependencias externas para facilitar tests unitarios.
/// </summary>
public class GeofencingService
{
    private const double EarthRadiusMeters = 6371000; // Radio de la Tierra en metros

    /// <summary>
    /// Calcula la distancia euclidiana aproximada entre dos puntos GPS en metros.
    /// Utiliza fórmula de Haversine para precisión en distancias cortas (zonas de peligro).
    /// </summary>
    /// <param name="pointA">Punto de origen</param>
    /// <param name="pointB">Punto de destino</param>
    /// <returns>Distancia en metros</returns>
    public double CalculateDistance(Point pointA, Point pointB)
    {
        // Se valida que las coordenadas sean válidas para evitar cálculos erróneos
        if (!IsValidCoordinate(pointA) || !IsValidCoordinate(pointB))
            throw new ArgumentException("Coordenadas inválidas");

        double lat1Rad = DegreesToRadians(pointA.Latitude);
        double lon1Rad = DegreesToRadians(pointA.Longitude);
        double lat2Rad = DegreesToRadians(pointB.Latitude);
        double lon2Rad = DegreesToRadians(pointB.Longitude);

        double deltaLat = lat2Rad - lat1Rad;
        double deltaLon = lon2Rad - lon1Rad;

        double a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                   Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                   Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusMeters * c;
    }

    /// <summary>
    /// Verifica si un punto está dentro de una zona circular de peligro.
    /// Retorna true si la distancia al centro es menor o igual al radio (alerta de seguridad).
    /// </summary>
    /// <param name="center">Centro de la zona de peligro</param>
    /// <param name="radiusMeters">Radio en metros (50m para este sistema)</param>
    /// <param name="currentPosition">Posición actual del operario</param>
    /// <returns>True si está en zona de peligro</returns>
    public bool IsInDangerZone(Point center, double radiusMeters, Point currentPosition)
    {
        double distance = CalculateDistance(center, currentPosition);
        return distance <= radiusMeters;
    }

    /// <summary>
    /// Valida que las coordenadas GPS estén dentro de rangos geográficos válidos.
    /// Previene cálculos erróneos por datos corruptos o nulos.
    /// </summary>
    /// <param name="point">Punto a validar</param>
    /// <returns>True si válido</returns>
    private bool IsValidCoordinate(Point point)
    {
        return point.Latitude >= -90 && point.Latitude <= 90 &&
               point.Longitude >= -180 && point.Longitude <= 180;
    }

    private double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
}

/// <summary>
/// Interfaz para servicio de notificaciones de alertas.
/// Se inyecta para permitir mocking en tests y desacoplar de implementaciones reales.
/// </summary>
public interface IAlertService
{
    void SendAlert(string message);
}

/// <summary>
/// Servicio de alertas que procesa posiciones y genera notificaciones.
/// Incluye lógica de filtrado para evitar falsas alarmas por ruido GPS.
/// </summary>
public class AlertService
{
    private readonly IAlertService _alertService;
    private readonly GeofencingService _geofencingService;

    public AlertService(IAlertService alertService, GeofencingService geofencingService)
    {
        _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        _geofencingService = geofencingService ?? throw new ArgumentNullException(nameof(geofencingService));
    }

    /// <summary>
    /// Procesa una nueva posición GPS y genera alerta si entra en zona de peligro.
    /// Filtra ruido GPS validando consistencia temporal (velocidad máxima realista).
    /// </summary>
    /// <param name="dangerCenter">Centro de zona de peligro</param>
    /// <param name="radiusMeters">Radio de zona</param>
    /// <param name="previousPosition">Posición anterior (puede ser null)</param>
    /// <param name="currentPosition">Posición actual</param>
    /// <param name="timeDeltaSeconds">Tiempo transcurrido desde última posición</param>
    public void ProcessPosition(Point dangerCenter, double radiusMeters, Point? previousPosition, Point? currentPosition, double timeDeltaSeconds)
    {
        // Validar coordenadas nulas o inválidas
        if (currentPosition == null || !IsValidCoordinate(currentPosition))
            return; // Ignorar lecturas inválidas sin alertar

        // Filtrar saltos bruscos irrealistas (>10m/s velocidad máxima razonable)
        if (previousPosition != null && timeDeltaSeconds > 0)
        {
            double distanceMoved = _geofencingService.CalculateDistance(previousPosition, currentPosition);
            double speedMs = distanceMoved / timeDeltaSeconds;
            if (speedMs > 10) // Salto brusco por error GPS
                return; // No procesar, esperar siguiente lectura consistente
        }

        // Verificar zona de peligro
        if (_geofencingService.IsInDangerZone(dangerCenter, radiusMeters, currentPosition))
        {
            _alertService.SendAlert($"¡Alerta de seguridad! Operario entró en zona de peligro a {DateTime.Now}");
        }
    }

    private bool IsValidCoordinate(Point point)
    {
        return point.Latitude >= -90 && point.Latitude <= 90 &&
               point.Longitude >= -180 && point.Longitude <= 180;
    }
}
