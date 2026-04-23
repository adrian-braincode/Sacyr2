**Resumen Ejecutivo**

Roadmap pragmático de tuning para llevar el reporte de consumo desde un plan con Full Table Scan a un plan con Index Seek y lecturas lógicas reducidas.

**Fase 3 — Roadmap de Tuning (tareas atomizadas)**

1) Establecer línea base (DoD: captura reproducible de métricas)

- Ejecutar en entorno de pruebas con datos representativos.
- Comandos para medir (ejecutar antes y después):

SET STATISTICS IO ON;
SET STATISTICS TIME ON;
-- Ejecutar la consulta lenta y guardar:
-- - Tiempo total (ms)
-- - CPU ms
-- - Logical reads
-- - Plan de ejecución (XML/actual execution plan)

- DoD: Documento con métricas actuales (logical reads, tiempo, CPU, plan).

2) Reescribir consulta (DoD: consulta sin subconsultas correlacionadas y con CTEs/window donde aplique)

- Transformar scalar correlated-subqueries a CTEs+JOIN o a `APPLY` con pre-aggregación.
- Reemplazar UDF scalars por expresiones inline o aplicar `inline TVFs` cuando sea necesario.
- DoD: nueva consulta con plan reproducible y disminución inicial de lecturas.

3) Probar índice candidato en entorno de validación (DoD: índice creado y medido)

- Script de creación (ejemplo):

CREATE NONCLUSTERED INDEX IDX_Consumption_Report
ON dbo.Consumption (EventDate, MachineId, ConsumptionType)
INCLUDE (Value, Unit, OtherDimensionId)
WHERE IsActive = 1;

- Ejecutar la consulta refactorizada y comparar `STATISTICS IO/TIME` y plan.
- DoD: `Index Seek` aparece en el plan; Logical reads reducidas ≥50% (iterar hasta objetivo ≥90%).

4) Validación de plan y ajustes (DoD: plan estable con Index Seek y sin Key Lookup relevantes)

- Revisar plan XML: asegurarse de que no existan `Clustered Index Scan` ni `Hash Match` innecesarios.
- Si aparecen `Key Lookup` frecuentes: mover columnas a `INCLUDE` o reordenar columnas del índice.

5) Considerar alternativas si la mejora no es suficiente (DoD: decisión documentada)

- Crear `NONCLUSTERED COLUMNSTORE INDEX` en entorno de pruebas y comparar tiempos de agregación.
- Evaluar particionado por `EventDate` si consultas históricas abarcan rangos amplios.

6) Implementación en producción y vigilancia (DoD: despliegue controlado + rollback plan)

- Crear índices en horario de baja actividad o con `ONLINE=ON` si la edición lo permite.
- Configurar monitorización: Query Store o Extended Events para detectar regresiones.
- Programar mantenimiento: `UPDATE STATISTICS`, `REBUILD` periódicos y revisión de `FILLFACTOR`.

7) Métricas posteriores y cierre (DoD: demostrar que las metas numéricas se han cumplido)

- Comparar métricas antes/después: Latencia, Logical reads, CPU.
- Verificar crecimiento del índice y costo de mantenimiento; documentar coste-beneficio.

**Plantillas útiles**

- Captura rápida de STATISTICS:

-- Ejecutar:
SET STATISTICS IO ON;
SET STATISTICS TIME ON;
-- Ejecutar consulta
-- Guardar plan de ejecución (`Ctrl+M` o obtener plan real en SSMS/ADDM o Query Store)

**Riesgos y mitigaciones**

- Riesgo: índices nuevos aumentan coste de DML. Mitigación: evaluar carga de escrituras, usar `FILLFACTOR` y programar mantenimiento.
- Riesgo: columna incluida muy grande. Mitigación: no incluir columnas `max` o mover a una tabla de dimensiones.

**Siguiente paso sugerido**

- Generar scripts automáticos de creación de índice candidato y un runbook de validación; ¿desea que genere esos scripts ahora?
