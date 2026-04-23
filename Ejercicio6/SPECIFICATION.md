# SPECIFICATION: Migración de almacenamiento de planos a Azure Blob Storage

## Resumen ejecutivo
Objetivo: migrar los archivos binarios de planos técnicos actualmente alojados en SQL Server hacia Azure Blob Storage, garantizando Zero Downtime para usuarios productivos y minimizando riesgo por latencia y permisos.

## Alcance y capas afectadas
- Configuración: variables de entorno, secretos, connection strings, permisos RBAC y plantillas IaC (Terraform/Bicep/ARM).
- Persistencia: repositorios que guardan/recuperan planos y metadatos; esquema en SQL Server (posible reducción de BLOBs en la base de datos).
- APIs: endpoints de subida/descarga; contratos de respuesta; generación de SAS/URLs de acceso; validación y logs.

## Contratos de datos (entrada/salida)
- Input (upload): multipart/form-data o stream binario; headers/metadatos: `blueprintId`, `version`, `author`, `checksum` (SHA256), `content-type`.
- Output (download): stream binario con `content-type`, cabeceras de integridad (`x-checksum-sha256`), y metadatos JSON opcional.
- Index/search: metadatos de planos permanecen en SQL (id, referenciaBlob, version, estado, fecha), o bien migración total de metadatos a un índice separado si se requiere desacoplar SQL.

## Requisitos funcionales y no funcionales
- Zero Downtime: las APIs deben soportar lectura y escritura durante migración. Las operaciones deben ser idempotentes.
- Seguridad: uso de Azure AD y Managed Identities; cuando proceda, SAS tokens con expiración corta para accesos temporales controlados.
- Rendimiento: latencia de descarga comparable a la actual (SLA interno). Páginas de 95th percentile < 300 ms para metadatos; BLOB streaming optimizado para descargas mayores.
- Consistencia: garantizar que el vínculo entre metadato en SQL y blob en Storage sea consistente; reconciliación eventual para fallos.
- Observabilidad: tracing distribuido (Application Insights), métricas (ingresos, latencia, errores), logs de auditoría para accesos a planos.

## Requisitos para migración con Zero Downtime
1. Dual-Write: durante la fase de corte, nuevas escrituras deben escribirse simultáneamente en SQL y en Blob Storage (o en el nuevo esquema preferido).
2. Read-Path adaptativo: lecturas deben preferir Blob Storage cuando exista; fallback a SQL si blob no disponible.
3. Backfill asíncrono: migración histórica por lotes con verificación checksum y registro de estado (pendiente/ok/error).
4. Feature flag / toggle: activar/desactivar uso de Blob en runtime para permitir rollback inmediato.
5. Idempotencia y compensación: cada operación tiene idempotency key; reclamo/reintentos limitados y reconciliación por Outbox.

## Gestión de riesgos
- Latencia de red: habilitar CDN o Azure Front Door para tráfico público; usar proximidad regional; pruebas de carga y benchmarks.
- Permisos y seguridad: usar Managed Identities para servicios; emitir SAS de corta duración para clientes; roles mínimos (`Storage Blob Data Contributor` para servicios que escriben).
- Pérdida de datos durante migración: validar checksums (SHA256) tras copia; mantener copia en SQL hasta verificación completa; realizar pruebas en staging con dataset representativo.
- Fallos parciales (escritura sólo en SQL o sólo en Blob): implementar Outbox + reconciliador que detecte discrepancias y vuelva a aplicar transferencias fallidas.
- Rollback: mantener soporte para servir planos desde SQL hasta que migración completa verificada; feature flag rápida para revertir a la ruta antigua.

## Requisitos operativos
- IaC: plantillas versionadas y pipelines para crear Storage Account, containers, roles RBAC y redes (NSG, firewall si aplica).
- Backups y retención: políticas de soft-delete y snapshot en Blob Storage; reglas de lifecycle (tiering a cool/archive si aplica).
- Auditoría: habilitar logs de almacenamiento (Azure Monitor) y retención por cumplimiento.

## Entregables de la especificación
- Documentación de contratos API actualizados.
- Plan de migración dual-write y backfill.
- Plantillas IaC y runbooks de rollback.
