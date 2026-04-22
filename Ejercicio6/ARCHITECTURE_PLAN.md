# Plan de Arquitectura para Migración a Azure Blob Storage

## Patrones de Diseño Aplicados
Aplicamos principios SOLID y Inyección de Dependencias para desacoplar el sistema:

- **Inyección de Dependencias (DI):** El repositorio AzureBlobBlueprintRepository se inyecta en la capa de APIs, permitiendo cambiar la implementación sin afectar el código cliente.
- **Principio de Responsabilidad Única (SRP):** Cada clase tiene una sola responsabilidad (ej. AzureBlobRepository solo maneja blobs).
- **Principio de Inversión de Dependencias (DIP):** Las APIs dependen de abstracciones (interfaces IBlueprintRepository), no de concretas.

El sistema es agnóstico a la fuente de datos: la lógica de negocio no conoce si los datos vienen de SQL o Azure Blob.

## Gestión de Errores y Resiliencia
- **Estrategias de Retry:** Para operaciones de red, implementar exponential backoff con máximo 3 reintentos.
- **Timeouts:** 30 segundos para uploads/downloads; 10 segundos para metadatos.
- **Circuit Breakers:** Si el 50% de las operaciones fallan en 1 minuto, abrir el circuito por 5 minutos para evitar sobrecarga.

## Architecture Decision Records (ADRs)

### ADR 1: Uso de Identidades Administradas de Azure
**Contexto:** Necesitamos autenticación segura sin credenciales hardcodeadas.  
**Decisión:** Usar Managed Identities para acceder a Azure Blob Storage desde VMs/App Services.  
**Justificación:** Mejora la seguridad (no claves en código), simplifica gestión (Azure maneja rotación), cumple con principios de "Security by Design".  
**Impacto:** Requiere configuración en Azure AD; compatible con zero downtime.

### ADR 2: Estrategia de "Dual Write" para Migración
**Contexto:** Migrar datos históricos sin downtime.  
**Decisión:** Implementar dual write: escribir en SQL Server y Azure Blob simultáneamente durante la migración.  
**Justificación:** Garantiza consistencia; permite rollback si falla; minimiza riesgos de pérdida de datos.  
**Impacto:** Aumenta carga temporal en SQL; requiere monitoreo de sincronización.

## Diagrama de Flujo Lógico
```
[Cliente] -> [API Controller] -> [IBlueprintRepository] 
                                      |
                                      +-> [SqlBlueprintRepository] (temporal)
                                      +-> [AzureBlobBlueprintRepository] (futuro)
                                      |
                                      +-> [Config Layer] (SAS, Managed Identity)
```