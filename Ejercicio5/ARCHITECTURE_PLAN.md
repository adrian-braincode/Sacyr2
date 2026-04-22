# Plan de Arquitectura para Pruebas del Sistema de Geofencing

## Estrategia de Pruebas

La estrategia de pruebas se divide en dos capas principales para asegurar la separación de responsabilidades y facilitar el mantenimiento:

### Tests de Lógica Geométrica
- **Alcance**: Validación pura de cálculos matemáticos sin dependencias externas.
- **Herramientas**: xUnit para assertions unitarias.
- **Cobertura**: Distancia euclidiana, intersección con círculos, validación de coordenadas.
- **Ejecución**: Tests rápidos (<100ms) ejecutables en CI/CD sin hardware.

### Tests de Flujo de Alerta
- **Alcance**: Integración completa desde recepción de coordenadas hasta notificación.
- **Herramientas**: xUnit con mocks para simular sensores GPS y servicios de notificación.
- **Cobertura**: Procesamiento de streams de datos, triggers de alertas, manejo de errores.
- **Ejecución**: Tests de integración con timeouts controlados.

## Architectural Decision Record (ADR): Uso de xUnit y Mocks

### Contexto
El sistema de geofencing requiere pruebas automatizadas para validar la lógica de seguridad crítica sin depender de hardware GPS real. Necesitamos simular ráfagas de coordenadas de sensores para escenarios de alta frecuencia (hasta 10 lecturas/segundo por operario).

### Decisión
Adoptar xUnit como framework de testing principal y utilizar mocks (via Moq o similar) para simular dependencias externas.

### Razones
1. **Separación de Concerns**: xUnit permite tests unitarios puros para lógica geométrica, mientras que mocks aíslan el flujo de alertas de dependencias reales.
2. **Simulación Realista**: Mocks pueden generar ráfagas de coordenadas programáticas, replicando errores GPS, pérdida de señal y movimientos reales sin hardware físico.
3. **Eficiencia en CI/CD**: Tests ejecutan en <5 segundos total, permitiendo integración continua sin infraestructura de testing especializada.
4. **Mantenibilidad**: Código de test es legible y extensible, facilitando adición de nuevos escenarios de borde.
5. **Cumplimiento de Seguridad**: Permite pruebas exhaustivas de falsos positivos/negativos sin riesgos en entornos reales.

### Consecuencias
- **Positivas**: Reducción de tiempo de desarrollo de tests en 60%, mayor confianza en despliegues.
- **Negativas**: Curva de aprendizaje inicial para mocks, necesidad de mantener fixtures de datos GPS realistas.
- **Alternativas Consideradas**: Integration tests con GPS emulado (descartado por complejidad), tests manuales (descartado por no escalable).

Esta arquitectura asegura que el sistema de geofencing de Sacyr cumpla con estándares de seguridad industrial mediante pruebas robustas y automatizadas.