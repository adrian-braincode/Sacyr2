# Roadmap de Tareas para Pruebas del Sistema de Geofencing

## Roadmap Granular

### Fase de Modelos de Prueba (Semana 1-2)
- **Tarea 1.1**: Crear modelos de datos para coordenadas GPS (latitud, longitud, timestamp, precisión).
- **Tarea 1.2**: Implementar modelo de zona de peligro (centro, radio=50m, tipo de peligro).
- **Tarea 1.3**: Desarrollar fixtures de prueba con datos GPS realistas (movimientos lineales, circulares, erráticos).
- **Tarea 1.4**: Configurar proyecto de tests con xUnit y dependencias de mocking.

### Fase de Tests de Lógica Core (Semana 3-4)
- **Tarea 2.1**: Test de distancia euclidiana - validar cálculo preciso entre dos puntos GPS.
- **Tarea 2.2**: Test de intersección circular - determinar si punto está dentro/fuera del radio 50m.
- **Tarea 2.3**: Test de validación de coordenadas - rechazar valores null, fuera de rango, inconsistentes.
- **Tarea 2.4**: Test de filtrado de ruido GPS - suavizar lecturas erráticas sin perder precisión.

### Fase de Escenarios de Límite (Semana 5-6)
- **Tarea 3.1**: Test límite inferior - posición a 49.9m (debe alertar al entrar).
- **Tarea 3.2**: Test límite superior - posición a 50.1m (no debe alertar si está fuera).
- **Tarea 3.3**: Test transición de límite - movimiento gradual cruzando el umbral.
- **Tarea 3.4**: Test saltos bruscos - validar que no alerta por errores GPS temporales.

### Fase de Verificación de Notificaciones (Semana 7-8)
- **Tarea 4.1**: Test de trigger de alerta - generación de notificación al entrar en zona.
- **Tarea 4.2**: Test de no-alerta - confirmación de silencio cuando fuera de zona.
- **Tarea 4.3**: Test de recuperación de señal - manejo correcto tras pérdida temporal de GPS.
- **Tarea 4.4**: Test de rendimiento - procesamiento de 100 operarios simultáneos sin latencia >1s.

### Fase de Integración y Validación Final (Semana 9-10)
- **Tarea 5.1**: Ejecutar suite completa de tests en entorno CI/CD.
- **Tarea 5.2**: Generar reportes de cobertura (>90% en lógica crítica).
- **Tarea 5.3**: Validar cumplimiento con criterios de aceptación definidos.
- **Tarea 5.4**: Documentar hallazgos y recomendaciones para producción.

Este roadmap asegura una implementación incremental y verificable del plan de pruebas, priorizando la seguridad de los operarios en obras de Sacyr.