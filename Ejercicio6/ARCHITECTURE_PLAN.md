# ARCHITECTURE_PLAN: Diseño para `AzureBlobBlueprintRepository` y ADR

## Resumen del diseño
Se introduce `AzureBlobBlueprintRepository` como implementación de la abstracción de almacenamiento de planos. Su responsabilidad: almacenar/recuperar blobs, exponer URLs seguras (SAS) y proporcionar operaciones atómicas a nivel de negocio (upload, download, delete, copy, verifyChecksum).

## Estructura del repositorio (API pública)
- `saveBlueprint(blueprintId: UUID, stream: Stream, metadata: Map<String,String>) -> BlobReference`
- `getBlueprint(blueprintId, version?) -> Stream` (streaming)
- `generateSasUrl(blueprintId, expiry, permissions) -> URL`
- `deleteBlueprint(blueprintId, version?) -> void`
- `copyFromSql(recordId) -> MigrationResult`
- `verifyChecksum(blueprintId, expectedChecksum) -> boolean`

## Layout de containers y naming
- Container por entorno: `blueprints-dev`, `blueprints-staging`, `blueprints-prod`.
- Prefijo por cliente/obra: `{obraId}/{blueprintId}/{version}`. Objetivo: granularidad, políticas de lifecycle y políticas de acceso por carpeta.

## Metadatos y búsqueda
- Mantener índice relacional en SQL Server con referencia al blob (`container`, `blobPath`, `version`, `checksum`, `status`). Esto facilita búsquedas complejas y mantiene integridad referencial.
- Alternativa: uso de Azure Cognitive Search o Cosmos DB para búsquedas si se decide desacoplar SQL más adelante.

## Consistencia y transacciones
- No hay transacciones distribuidas entre SQL y Blob; se usa patrón Dual-Write + Outbox:
  - Paso de escritura: API escribe blob en Storage y registra mensaje en tabla Outbox en SQL (o viceversa) con estado pendiente.
  - Reconciliador (background worker) procesa Outbox: intenta idempotentemente confirmar metadata y marcar como completado.

## Dual-Write y estrategia de migración histórica
- Dual-Write en caliente: las nuevas operaciones escriben en ambos sistemas mediante una abstracción `BlueprintService` que orquesta ambas operaciones y aplica idempotencia.
- Backfill: herramienta por lotes que lee BLOBs en SQL, los copia a Blob Storage, calcula checksum y actualiza índice con estado. La herramienta debe soportar throttling y reintentos exponenciales.

## ADR: Uso de Managed Identities de Azure
### Contexto
La solución requiere que servicios (APIs, workers, pipelines) accedan a Blob Storage de forma segura y sin secretos embebidos.

### Alternativas consideradas
- a) Service Principal con secret en Key Vault.
- b) Managed Identity (MSI) asignada a recursos (App Service, VM, AKS).
- c) SAS tokens rotados por sistema propio.

### Decisión
Se elegirá Managed Identity (System-assigned o User-assigned según el recurso) + roles RBAC mínimos (`Storage Blob Data Contributor` para escritura, `Storage Blob Data Reader` para lectura). SAS tokens solo para casos puntuales de cliente y con expiración corta.

### Justificación
- Seguridad: elimina gestión de secretos y rotación manual.
- Principio de menor privilegio: RBAC granulado por recurso y scope.
- Operacional: integración nativa con Azure AD y soporte para tokens cortos.

### Impacto
- Requiere configuración de identidades en IaC y asignación de roles en Storage Account.
- Los pipelines CI/CD deberán autenticarse con identidades/Service Principal para despliegues.

## Observabilidad y seguridad
- Habilitar diagnostic logs para Storage y enviar a Log Analytics.
- Habilitar soft-delete, versioning y reglas de lifecycle.

## Requisitos de IaC
- Plantillas para Storage Account con firewall, soft-delete, containers y políticas de lifecycle.
- Roles RBAC y asignación de Managed Identities.

## Migración incremental propuesta
1. Provisionar storage y asignar roles (staging).
2. Deploy de `AzureBlobBlueprintRepository` en staging con feature flag off.
3. Activar dual-write en staging y ejecutar backfill de prueba.
4. Ejecutar pruebas de integración y rendimiento.
5. Activar dual-write en prod y empezar backfill por lotes.
6. Monitorizar, reconciliar y cortar lecturas de SQL tras verificación completa.
