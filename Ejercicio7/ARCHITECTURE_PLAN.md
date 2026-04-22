# Plan de Arquitectura para Optimización de Consulta

## Estrategia de Refactorización

### Uso de CTEs (Common Table Expressions)
Refactorizaremos la consulta utilizando CTEs para precalcular los agregados de `Movimientos` antes del JOIN principal. Esto elimina las subconsultas correlacionadas y permite optimizaciones del motor de consultas.

**Nueva Estructura de Consulta**:
```sql
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
```

**Ventajas**:
- El CTE agrupa y calcula una sola vez por `MaquinaId`.
- Reduce la complejidad de O(n×m) a O(m + n), donde m es el GROUP BY y n el JOIN final.
- Permite al optimizador usar índices de manera más eficiente.

### Alternativa con Window Functions (si aplica)
Si se requiere mantener el contexto de filas individuales, podríamos usar Window Functions, pero para agregados por grupo, el CTE es más apropiado y eficiente.

## Propuesta de Índice Non-Clustered Cubriente

### Definición del Índice
Crearemos un índice Non-Clustered en la tabla `Movimientos` que cubra completamente la consulta refactorizada:

```sql
CREATE NONCLUSTERED INDEX IX_Movimientos_MaquinaId_Incluye_Consumo_Fecha
ON Movimientos (MaquinaId)
INCLUDE (Consumo, Fecha);
```

**Explicación**:
- **Columna de Clave**: `MaquinaId` para filtrado eficiente (Index Seek).
- **Cláusula INCLUDE**: `Consumo` y `Fecha` para que el índice contenga todos los datos necesarios, evitando lookups adicionales a la tabla clustered.
- **Cubriente**: El índice satisface completamente la consulta del CTE sin acceder a la tabla base.

### Beneficios Esperados
- **Index Seek**: El motor usará Index Seek en lugar de Table Scan.
- **Reducción de IO**: Lecturas solo desde el índice, no desde la tabla completa.
- **Mejora en GROUP BY**: El índice ordenado por `MaquinaId` acelera las operaciones de agregación.

### Consideraciones de Mantenimiento
- **Espacio en Disco**: El índice agregará ~20-30% del tamaño de la tabla (dependiendo de la cardinalidad de `MaquinaId`).
- **Actualizaciones**: Costo moderado en INSERT/UPDATE/DELETE en `Movimientos`.
- **Estadísticas**: Asegurar que las estadísticas del índice estén actualizadas para planes óptimos.