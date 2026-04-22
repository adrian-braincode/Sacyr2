# Roadmap de Tuning para Optimización de Consulta

## Fase 1: Establecer Línea Base (Baseline)

### Paso 1.1: Habilitar Estadísticas de Rendimiento
Ejecutar la consulta original con mediciones activadas:
```sql
SET STATISTICS IO ON;
SET STATISTICS TIME ON;
-- Ejecutar consulta original
SELECT 
    m.Nombre AS Maquina,
    p.Nombre AS Proyecto,
    (SELECT SUM(Consumo) FROM Movimientos WHERE MaquinaId = m.Id) AS TotalConsumo,
    (SELECT MAX(Fecha) FROM Movimientos WHERE MaquinaId = m.Id) AS UltimoMovimiento
FROM Maquinas m
JOIN Proyectos p ON m.ProyectoId = p.Id
WHERE m.Estado = 'Activa' 
  AND m.FechaAlta > '2020-01-01'
ORDER BY TotalConsumo DESC;
SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;
```

**Métricas a Registrar**:
- Tiempo de ejecución total.
- CPU time.
- Lecturas lógicas (logical reads).
- Lecturas físicas (physical reads).
- Plan de ejecución actual (usar `SET SHOWPLAN_XML ON` o SQL Server Management Studio).

**Objetivo**: Confirmar Full Table Scans y tiempos >60 segundos.

### Paso 1.2: Analizar Plan de Ejecución
- Verificar operadores: Table Scan en `Movimientos`.
- Estimar costo relativo de subconsultas correlacionadas.

## Fase 2: Creación de Índices Optimizados

### Paso 2.1: Crear Índice Non-Clustered Cubriente
```sql
CREATE NONCLUSTERED INDEX IX_Movimientos_MaquinaId_Incluye_Consumo_Fecha
ON Movimientos (MaquinaId)
INCLUDE (Consumo, Fecha)
WITH (FILLFACTOR = 90, SORT_IN_TEMPDB = ON);
```

**Parámetros**:
- FILLFACTOR = 90: Deja espacio para actualizaciones.
- SORT_IN_TEMPDB = ON: Reduce contención en tempdb durante creación.

### Paso 2.2: Actualizar Estadísticas
```sql
UPDATE STATISTICS Movimientos WITH FULLSCAN;
```

**Razón**: Asegurar que el optimizador tenga estadísticas precisas del nuevo índice.

### Paso 2.3: Verificar Creación del Índice
- Usar `sp_helpindex 'Movimientos'` para confirmar.
- Verificar espacio usado con `sp_spaceused 'Movimientos'`.

## Fase 3: Validación del Nuevo Plan de Ejecución

### Paso 3.1: Ejecutar Consulta Refactorizada con Baseline
```sql
SET STATISTICS IO ON;
SET STATISTICS TIME ON;
-- Consulta refactorizada
WITH MovimientosAgregados AS (
    SELECT 
        MaquinaId,
        SUM(Consumo) AS TotalConsumo,
        MAX(Fecha) AS UltimoMovimiento
    FROM Movimientos
    GROUP BY MaquinaId
)
SELECT 
    m.Nombre AS Maquina,
    p.Nombre AS Proyecto,
    ma.TotalConsumo,
    ma.UltimoMovimiento
FROM Maquinas m
JOIN Proyectos p ON m.ProyectoId = p.Id
LEFT JOIN MovimientosAgregados ma ON m.Id = ma.MaquinaId
WHERE m.Estado = 'Activa' 
  AND m.FechaAlta > '2020-01-01'
ORDER BY ma.TotalConsumo DESC;
SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;
```

**Validar**:
- Lecturas lógicas < 10,000.
- CPU time < 2 segundos.
- Tiempo total < 5 segundos.

### Paso 3.2: Inspeccionar Plan de Ejecución
- Usar `SET SHOWPLAN_XML ON` o SSMS para visualizar el plan.
- **Verificación Clave**: Operador "Index Seek" en `Movimientos` para el CTE.
- Confirmar que no hay "Table Scan" o "Clustered Index Scan".

### Paso 3.3: Pruebas de Carga y Concurrencia
- Ejecutar con diferentes volúmenes de datos.
- Simular concurrencia con múltiples usuarios.
- Monitorear bloqueos con `sys.dm_exec_requests`.

### Paso 3.4: Rollback Plan (si necesario)
Si el rendimiento no mejora:
- Dropear índice: `DROP INDEX IX_Movimientos_MaquinaId_Incluye_Consumo_Fecha ON Movimientos;`
- Revertir a consulta original.
- Investigar otras estrategias (particionamiento, etc.).

## Métricas de Éxito
- ✅ Index Seek en plan de ejecución.
- ✅ Reducción >90% en lecturas lógicas.
- ✅ Tiempo de ejecución <5 segundos.
- ✅ CPU time <2 segundos.
- ✅ Sin degradación en otras consultas.

## Notas Adicionales
- Ejecutar en entorno de desarrollo primero.
- Monitorear impacto en mantenimiento (INSERT/UPDATE/DELETE).
- Considerar particionamiento si la tabla crece más.