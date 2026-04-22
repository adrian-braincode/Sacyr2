# Especificación de Optimización de Consulta de Reporte de Consumo

## Análisis de Anti-Patrones

### Subconsultas Correlacionadas en SELECT
La consulta actual utiliza subconsultas correlacionadas en la cláusula SELECT para calcular `TotalConsumo` y `UltimoMovimiento`:
- `(SELECT SUM(Consumo) FROM Movimientos WHERE MaquinaId = m.Id) AS TotalConsumo`
- `(SELECT MAX(Fecha) FROM Movimientos WHERE MaquinaId = m.Id) AS UltimoMovimiento`

**Problema**: Para cada fila en la tabla `Maquinas` (aproximadamente miles de filas), se ejecuta una subconsulta completa sobre `Movimientos` (50 millones de filas). Esto resulta en un número de operaciones proporcional al producto cartesiano de ambas tablas, lo que es ineficiente y causa lentitud extrema.

### Predicción de Operaciones Costosas
- **Full Table Scans**: Sin índices apropiados en `Movimientos` sobre `MaquinaId`, cada subconsulta realizará un escaneo completo de la tabla de 50 millones de filas.
- **Lecturas Físicas Excesivas**: Se esperan miles de lecturas de páginas de disco, lo que explica los minutos de ejecución.
- **Bloqueos y Contención**: En entornos concurrentes, esto puede causar bloqueos prolongados.

## Metas Numéricas de Optimización

### Objetivos de Rendimiento
- **Reducción de Complejidad**: Pasar de complejidad **O(n × m)** (lineal en el producto de filas) a **O(n × log m)** (logarítmica en la tabla grande).
- **Tiempo de Ejecución**: De minutos (>60 segundos) a segundos (<5 segundos).
- **Lecturas Lógicas**: De millones de lecturas a miles (reducción >90%).
- **Uso de CPU**: Reducción del 80% en tiempo de CPU dedicado a la consulta.
- **Plan de Ejecución**: Cambiar de "Full Table Scan" a "Index Seek" en `Movimientos`.

### Métricas de Validación
- STATISTICS IO: Lecturas lógicas < 10,000.
- STATISTICS TIME: CPU time < 2 segundos.
- Plan de Ejecución: Operadores "Index Seek" en lugar de "Table Scan".