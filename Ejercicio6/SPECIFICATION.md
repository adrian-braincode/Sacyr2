# Especificación Técnica de Migración a Azure Blob Storage

## Análisis de Contrato
El sistema actual almacena planos técnicos en SQL Server, donde los planos son archivos binarios (PDF, DWG) asociados a metadatos (ID, nombre, fecha, proyecto). La migración requiere definir claramente las interfaces de entrada y salida:

- **Entradas del Sistema:**
  - Archivos de planos: Formatos soportados (PDF, DWG, JPEG). Tamaño máximo: 100MB por archivo.
  - Metadatos: JSON con campos obligatorios (id_proyecto, nombre_plano, fecha_creacion, autor).
  - Operaciones: Upload, Download, Delete, List por proyecto.

- **Salidas del Sistema:**
  - Archivos planos: Retorno directo del blob.
  - Metadatos: JSON con información del plano y URL SAS temporal para acceso seguro.
  - Respuestas de error: Códigos HTTP estándar (200 OK, 404 Not Found, 500 Internal Server Error).

- **Formatos de Datos:**
  - Entrada: Multipart/form-data para uploads; JSON para metadatos.
  - Salida: JSON para listados; Stream binario para downloads.

## Regla de Dependencia
Para evitar acoplamientos estrechos, definimos capas con permisos de importación estrictos:

- **Capa de Configuración:** Gestiona variables de entorno (connection strings, SAS tokens). No importa otras capas.
- **Capa de Persistencia:** Implementa repositorios (SQL Server actual, Azure Blob futuro). Puede importar Configuración.
- **Capa de APIs:** Controladores REST. Puede importar Persistencia y Configuración.
- **Capa de Aplicación:** Lógica de negocio. Puede importar APIs y Persistencia.

Regla: Una capa superior puede importar capas inferiores, pero no al revés. Prohibido importar directamente de Persistencia a APIs sin abstracción.

## Límites Físicos y de Negocio
- **Zero Downtime:** La migración debe mantener el sistema operativo durante todo el proceso. Estrategia de "Dual Write" para sincronizar datos.
- **Riesgos de Latencia de Red:** Azure Blob Storage puede tener latencias variables. Implementar timeouts de 30 segundos para operaciones.
- **Permisos y Seguridad:** Uso de Shared Access Signatures (SAS) para acceso temporal y seguro. Evitar hardcodeo de claves.
- **Umbrales de Seguridad:** Máximo 1000 operaciones por minuto por contenedor. Alertas si se supera.
- **Límites de Negocio:** Planos confidenciales; encriptación en tránsito (HTTPS) y en reposo (Azure Storage Encryption).