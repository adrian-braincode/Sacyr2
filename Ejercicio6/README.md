# Ejercicio6 - AzureBlobBlueprintRepository

Proyecto demo que implementa `AzureBlobBlueprintRepository` usando `Azure.Storage.Blobs`.

Requisitos para ejecutar tests de integración:
- .NET 7 SDK
- Variable de entorno `AZURE_STORAGE_CONNECTION_STRING` apuntando a una cuenta de Azure Storage o a Azurite.

Ejecutar tests (desde la raíz `Ejercicio6`):

```bash
dotnet test
```

Si `AZURE_STORAGE_CONNECTION_STRING` no está definida, los tests de integración serán omitidos automáticamente.
