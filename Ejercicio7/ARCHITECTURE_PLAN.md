**Resumen Ejecutivo**

Estrategia de refactorización para el reporte de consumo en SQL Server: reescribir subconsultas correlacionadas con JOINs/CTEs/Window Functions, crear índices nonclustered cubrientes (con `INCLUDE`) y, cuando proceda, usar índices filtrados o columnstore para cargas analíticas. Mantener integridad y minimizar impacto en OLTP.

**Fase 2 — Diseño y decisiones de arquitectura**

1) Reescritura de consulta

- Evitar subconsultas correlacionadas por fila. Reemplazar por:
  - Pre-aggregación en CTEs/tabla temporal: agrupar por `MachineId, Fecha` y luego hacer JOIN a la tabla fact.
  - `APPLY` (CROSS/OUTER APPLY) con top-agregados precomputados cuando la lógica sea por fila, pero calculando en batch.
  - Window Functions (`ROW_NUMBER() OVER (PARTITION BY MachineId ORDER BY Date DESC)`) para obtener último valor o rank sin subqueries.

Ejemplo (esqueleto):

WITH agg AS (
  SELECT MachineId, CAST(EventDate AS date) AS Day, SUM(Value) AS TotalValue
  FROM dbo.Consumption
  WHERE EventDate BETWEEN @StartDate AND @EndDate
    AND IsActive = 1
  GROUP BY MachineId, CAST(EventDate AS date)
)
SELECT m.MachineName, a.Day, a.TotalValue
FROM agg a
JOIN dbo.Machines m ON m.MachineId = a.MachineId;

2) Diseño de índices (Non-Clustered Covering Index)

- Principios:
  - Orden de columnas: columnas con predicado de igualdad → columnas con rango/orden.
  - Evitar incluir columnas grandes (varchar(max), nvarchar(max)) en el índice; dejarlas fuera si no son necesarias para el reporte.
  - Usar `INCLUDE` para columnas proyectadas que no participan en filtros/join/orden.
  - Considerar índice filtrado si el reporte usa `IsActive = 1` (reduce tamaño del índice y mejora selectividad).

- Ejemplo de índice recomendado:

CREATE NONCLUSTERED INDEX IDX_Consumption_Report
ON dbo.Consumption (EventDate, MachineId, ConsumptionType)
INCLUDE (Value, Unit, OtherDimensionId)
WHERE IsActive = 1;

Explicación: `EventDate` como primera columna soporta rangos por fecha; `MachineId` y `ConsumptionType` son columnas de filtro/agrupación; `INCLUDE` evita Key Lookups al devolver `Value` y dimensiones mostradas.

3) Alternativas avanzadas

- Columnstore: Si el workload es predominantemente analítico con agregaciones sobre la tabla entera, evaluar `NONCLUSTERED COLUMNSTORE INDEX` o `CLUSTERED COLUMNSTORE INDEX` (gran compresión, excelente para agregaciones masivas). Trade-off: menor velocidad en DML y mayor coste de mantenimiento.
- Partitioning por rango de fecha: particionar la tabla por `EventDate` para acelerar purgas y mejorar operaciones de mantenimiento.

4) Estadísticas y mantenimiento

- Asegurar `AUTO_UPDATE_STATISTICS` y ejecutar `UPDATE STATISTICS` tras cargas masivas.
- Políticas de mantenimiento del índice: `REBUILD` / `REORGANIZE` según fragmentación; considerar `FILLFACTOR` para evitar page splits en escrituras intensivas.

**ADRs (Architecture Decision Records)**

- ADR-01: Usar índices nonclustered cubrientes + INCLUDE como primera opción. Razonamiento: reduce Key Lookups y obliga a Index Seek para filtros selectivos; impacto en almacenamiento aceptable y menor impacto en DML comparado con columnstore.
- ADR-02: Evaluar Columnstore en carga analítica. Razonamiento: si las consultas agregan sobre la mayor parte de la tabla, Columnstore reduce I/O y CPU. Impacto: DML más costoso y mayor complejidad operacional.
- ADR-03: Emplear índices filtrados cuando `IsActive=1` (o condición estable). Razonamiento: reduce tamaño del índice y mejora selectividad si el filtro es estable y usado por la mayoría de consultas.

**Checklist de aceptación**

- Consultas refactorizadas sin subconsultas correlacionadas.
- Índice recomendado creado en entorno de pruebas.
- Estadísticas actualizadas y plan de mantenimiento definido.
