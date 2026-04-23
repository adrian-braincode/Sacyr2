# TASK_ROADMAP: Roadmap de tareas técnicas y criterios de aceptación

## Resumen
Roadmap desglosado en tres líneas: Infra (DevOps/IaC), Código (repositorio y refactor), Migración y Verificación (tests y monitoreo). Cada tarea incluye una breve Definición de Hecho (DoD).

## Fase A — Infraestructura Cloud (DevOps)
1. Provisionar Storage Account (entornos dev/staging/prod) con soft-delete y lifecycle.
   - DoD: Storage creado por IaC, soft-delete activado, container `blueprints-prod` creado.
2. Configurar Managed Identities y asignar roles RBAC (prod/staging).
   - DoD: Identidades asignadas y role `Storage Blob Data Contributor` aplicado a recurso.
3. Implementar red y seguridad (firewall, Private Endpoint si necesario).
   - DoD: Private Endpoint probado desde VNet; reglas de NSG/Firewall aplicadas.
4. Crear pipelines CI/CD para despliegue de IaC y runtime (dev->staging->prod).
   - DoD: Pipeline automatizado que aplica cambios mediante PR y plantillas versionadas.

## Fase B — Refactorización y SDK (Capa Aplicación)
5. Diseñar y documentar la interfaz `IBlueprintRepository` y `AzureBlobBlueprintRepository`.
   - DoD: Interface en repo de código, documentación con contratos y ejemplos.
6. Implementar cliente Azure Blob (SDK) en `AzureBlobBlueprintRepository`.
   - DoD: Operaciones `save`, `get`, `generateSas`, `delete`, con pruebas unitarias simuladas (mocks).
7. Refactorizar la capa de persistencia para inyectar la nueva implementación (DI) y evitar imports directos a infra.
   - DoD: Código sin imports circulares; configuración por DI; pruebas unitarias pasan.

## Fase C — Migración en vivo y backfill
8. Implementar Dual-Write orchestration y tabla Outbox en SQL.
   - DoD: Nuevas escrituras producen registros Outbox; reconciliador procesa entradas con idempotencia.
9. Desarrollar herramienta de backfill por lotes para migrar BLOBs históricos.
   - DoD: Backfill puede ejecutarse en modo dry-run; cada lote valida checksum y actualiza estado.
10. Implementar reconciliador (background worker) para detectar y corregir discrepancias.
   - DoD: Reconciliador reporta métricas y corrige >95% de errores automátimente en primera pasada.

## Fase D — Verificación, observabilidad y rollout
11. Escribir tests de integración que cubran upload/download, generación de SAS y fallos simulados.
   - DoD: Tests reproducibles en pipeline que usan un Storage Account de testing o emulador.
12. Pruebas de rendimiento y latencia (perfiles de descarga con CDN/Front Door si aplica).
   - DoD: Resultados documentados y thresholds validados (95p < 300ms para metadatos).
13. Despliegue gradual (canary/corte por porcentaje) y verificación en producción.
   - DoD: Feature flag activada para 10% de tráfico, monitoreo OK por 48h, escalado a 100%.
14. Documentación operativa y runbooks (rollback, incidente, reconciliación manual).
   - DoD: Runbooks validados en tabletop exercise.

## Criterios generales de aceptación (DoD global)
- Sin secretos embebidos; uso de Managed Identities o Key Vault.
- Todo cambio debe pasar por PR y pipelines automatizados.
- Tests unitarios e integración en pipeline; métricas y alertas configuradas.
- Rollback sencillo vía feature flag.

## Estimación y dependencias
- Dependencias clave: aprovisionamiento de Storage y RBAC antes de desplegar cambios de código; feature flag service disponible antes de activar dual-write.
- Estimación inicial: 6–10 semanas (infra + desarrollo + migración por lotes), variará según volumen histórico y pruebas de rendimiento.
