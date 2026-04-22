-- Consulta de Reporte de Consumo: Lenta y costosa (Original)
-- SELECT
--     m.Nombre AS Maquina,
--     p.Nombre AS Proyecto,
--     (SELECT SUM(Consumo) FROM Movimientos WHERE MaquinaId = m.Id) AS TotalConsumo,
--     (SELECT MAX(Fecha) FROM Movimientos WHERE MaquinaId = m.Id) AS UltimoMovimiento
-- FROM Maquinas m
-- JOIN Proyectos p ON m.ProyectoId = p.Id
-- WHERE m.Estado = 'Activa'
--   AND m.FechaAlta > '2020-01-01'
-- ORDER BY TotalConsumo DESC;

-- =====================================================
-- SOLUCIÓN DE OPTIMIZACIÓN SQL
-- =====================================================

-- Comandos de auditoría: Habilitar estadísticas de IO y tiempo
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

-- Script de creación del índice Non-Clustered cubriente
-- (Ejecutar una vez para optimizar la consulta)
CREATE NONCLUSTERED INDEX IX_Movimientos_MaquinaId_Incluye_Consumo_Fecha
ON Movimientos (MaquinaId)
INCLUDE (Consumo, Fecha)
WITH (FILLFACTOR = 90, SORT_IN_TEMPDB = ON);

-- Actualizar estadísticas para el nuevo índice
UPDATE STATISTICS Movimientos WITH FULLSCAN;

-- Consulta refactorizada utilizando CTE para agregación previa
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

-- Deshabilitar estadísticas después de la ejecución
SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;
