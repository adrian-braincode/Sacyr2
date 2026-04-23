**Resumen Ejecutivo**

Reporte de consumo lento sobre una tabla fact (`Consumption`) con ~50 millones de filas. Objetivo: reducir tiempo de consulta de minutos a subsegundos/centenas de milisegundos en operaciones críticas de reporte, llevando la complejidad de O(N) a un comportamiento logarítmico O(log N) mediante reescritura de consultas y diseño de índices.

**Fase 1 — Especificación técnica detallada**

- **Entrada (inputs):** parámetros del reporte (rango de fechas: `StartDate`, `EndDate`), identificadores (`MachineId`, `SiteId`, `ConsumptionType`), filtros booleanos (`IsActive`, `HasAlerts`), paginación/orden (`ORDER BY` y `TOP/N`).
- **Salida (outputs):** filas agregadas o detalladas por máquina/fecha con columnas numéricas (`Value`, `Unit`), métricas agregadas (`SUM`, `AVG`, `MAX`) y columnas de dimensión (`MachineName`, `Location`). Formato: JSON/CSV/Tabla.
- **Tipos y formatos:** fechas en `datetime`/`date`; IDs en `int`/`bigint`; valores numéricos en `decimal(18,4)`.

**Anti-patrones a buscar y corregir**

- Subconsultas correlacionadas en el `SELECT` (scalar subqueries por fila) — generan ejecución por fila y produzcan nested loops costosos.
- Funciones escalares definidas por usuario (UDFs) llamadas por fila — impiden paralelismo y provocan CPU-bound por cada fila.
- Predicados no-sargables: llamadas a funciones sobre columnas (`CONVERT`, `CAST`, `DATEPART(col)`) y operadores `LIKE '%x'`.
- `SELECT *` desde la tabla fact — provoca lectura de columnas no necesarias, aumento de I/O y de tamaño de fila.
- Filtros en columnas sin índices adecuados o con conversiones implícitas (p. ej. `varchar` vs `nvarchar`) — provocan Full Table Scan.
- Joins sin condiciones de unión SARGable o con tipos distintos — pueden forzar Hash Match sobre la tabla completa.

**Operaciones costosas previstas**

- Full Table Scan / Clustered Index Scan sobre 50M filas: operaciones I/O en orden de millones de páginas.
- Key Lookup multiplicado (bookmark lookup) si el índice no cubre las columnas proyectadas — cada seek puede provocar muchas lecturas adicionales.
- Sort masivo en memoria y spills a disco si `ORDER BY` sobre columnas sin índices adecuados.
- Hash aggregate / Hash join con alta cardinalidad que consuma memoria y desplace a tempdb.

**Metas numéricas (baseline → objetivo)**

- Complejidad algorítmica: de O(N) (escaneo completo de 50M) a O(log N) por búsqueda/seek; referencia: log2(50e6) ≈ 26 niveles.
- Latencia: de minutos (p. ej. 120s) → objetivo < 0.5s para consultas puntuales con filtros selectivos; para agregaciones completas objetivo < 5s.
- Lecturas lógicas (`STATISTICS IO`): reducir lecturas totales en ≥90% (p. ej. de millones → <100k lecturas lógicas).
- CPU: reducir tiempo CPU en ≥80% en paths críticos.
- Plan: lograr `Index Seek` como operación primaria, evitar `Clustered Index Scan` y minimizar `Key Lookup` (idealmente 0, aceptable <100 lookups por ejecución en consultas agregadas).

**Restricciones y supuestos**

- Cambios permitidos: creación de índices nonclustered, índices filtrados, índices con INCLUDE, creación de columnas computadas persistentes y cambios a consultas SQL. No asumimos cambios inmediatos en esquema OLTP ni migración a otro motor.
- Espacio adicional para índices es posible (se debe evaluar coste de almacenamiento y mantenimiento).

**Entregable de esta fase**

- Documento de especificación (este archivo) con metas cuantificables y lista de anti-patrones a corregir.
