# Roadmap de Tareas para Migración a Azure Blob Storage

## Tareas Granulares y Orden Lógico

1. **Configurar Infraestructura Cloud (DevOps):**
   - Crear Storage Account en Azure con redundancia GRS.
   - Configurar Managed Identity en App Service/VM.
   - Definir políticas de acceso y SAS tokens.
   - **DoD:** Storage Account accesible vía Managed Identity; logs de acceso habilitados.

2. **Refactorizar Capa de Abstracción:**
   - Crear interfaz IBlueprintRepository con métodos Upload, Download, Delete, List.
   - Implementar SqlBlueprintRepository actual como adapter.
   - **DoD:** Código compila sin imports circulares; interfaz inyectable.

3. **Implementar SDK de Azure:**
   - Crear AzureBlobBlueprintRepository usando Azure.Storage.Blobs SDK.
   - Implementar métodos con retry y circuit breaker.
   - Integrar SAS para URLs temporales.
   - **DoD:** Unit tests pasan; integración con DI container.

4. **Implementar Estrategia Dual Write:**
   - Modificar lógica de negocio para escribir en ambos repositorios.
   - Agregar flags de configuración para activar/desactivar dual write.
   - **DoD:** Datos sincronizados; monitoreo de discrepancias implementado.

5. **Verificación mediante Tests de Integración:**
   - Crear tests que simulen uploads/downloads con Azure Emulator.
   - Validar zero downtime: tests con tráfico simulado.
   - **DoD:** Cobertura >80%; tests pasan en CI/CD pipeline.

6. **Migración de Datos Históricos:**
   - Script para copiar planos de SQL a Azure Blob.
   - Verificar integridad con checksums.
   - **DoD:** Todos los planos migrados; validación de metadatos.

7. **Desactivar SQL Storage:**
   - Cambiar configuración para usar solo Azure Blob.
   - Monitorear rendimiento post-migración.
   - **DoD:** Sistema operativo solo con Azure; SQL deshabilitado.

## Orden Lógico
Ejecutar en secuencia: 1 (infra) -> 2 (abstracción) -> 3 (SDK) -> 4 (dual write) -> 5 (tests) -> 6 (migración) -> 7 (desactivar). Evita bloqueos al preparar infra primero.