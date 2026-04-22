# Sistema de Geofencing para Seguridad Industrial en Obras de Sacyr

## Descripción
Este proyecto implementa un sistema de geofencing que alerta cuando un operario entra en zonas de peligro circulares de 50 metros en obras de construcción. El sistema utiliza GPS para monitorear posiciones en tiempo real y genera alertas de seguridad críticas.

## Arquitectura
- **GeofencingLogic**: Biblioteca core con lógica de cálculo de distancias y verificación de zonas.
- **GeofencingTests**: Suite completa de pruebas unitarias con xUnit y Moq.

## Características Principales
- Cálculo preciso de distancias usando fórmula Haversine
- Validación de coordenadas GPS para prevenir errores
- Filtrado de ruido GPS (saltos bruscos >10m/s)
- Alertas configurables via interfaz IAlertService
- Cobertura completa de casos de borde y límites de seguridad

## Requisitos
- .NET 10.0
- xUnit para tests
- Moq para mocking

## Instalación y Ejecución
1. Clonar el repositorio
2. Navegar a la carpeta del proyecto: `cd Ejercicio5/GeofencingSystem`
3. Ejecutar tests: `dotnet test`

## Uso del Código
```csharp
// Crear servicio de geofencing
var geofencingService = new GeofencingService();

// Verificar zona de peligro
var center = new Point(40.4168, -3.7038); // Centro de Madrid
var operatorPosition = new Point(40.4168, -3.7038);
bool inDanger = geofencingService.IsInDangerZone(center, 50, operatorPosition);

// Procesar posición con alertas
var alertService = new AlertService(mockAlertService, geofencingService);
alertService.ProcessPosition(center, 50, previousPosition, currentPosition, timeDelta);
```

## Cobertura de Tests
- ✅ Lógica geométrica (distancia euclidiana)
- ✅ Verificación de zonas de peligro
- ✅ Escenarios de límite (49.9m vs 50.1m)
- ✅ Flujo de alertas con mocks
- ✅ Casos de borde (coordenadas nulas, inválidas, saltos GPS)
- ✅ Rendimiento (100 operarios simultáneos <1s)

## Criterios de Aceptación Cumplidos
Según SPECIFICATION.md:
- Alertas positivas cuando entra en zona
- Prevención de falsas alarmas por ruido GPS
- Recuperación correcta tras pérdida de señal
- Manejo robusto de coordenadas inválidas

## ADR Implementado
- Uso de xUnit para tests unitarios puros
- Mocks con Moq para simular sensores GPS sin hardware
- Patrón de inyección de dependencias para testabilidad

Este sistema asegura la seguridad de los operarios en obras de Sacyr mediante monitoreo GPS automatizado y alertas en tiempo real.