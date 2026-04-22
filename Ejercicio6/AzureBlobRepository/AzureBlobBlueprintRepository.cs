using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AzureBlobRepository;

/// <summary>
/// Implementación del repositorio de planos usando Azure Blob Storage.
/// </summary>
public class AzureBlobBlueprintRepository : IBlueprintRepository
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;

    /// <summary>
    /// Constructor que inyecta el cliente de Blob Service y el nombre del contenedor.
    /// </summary>
    public AzureBlobBlueprintRepository(BlobServiceClient blobServiceClient, string containerName)
    {
        _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        _containerName = containerName ?? throw new ArgumentNullException(nameof(containerName));
    }

    /// <summary>
    /// Sube un plano al repositorio usando Stream para optimizar memoria.
    /// </summary>
    public async Task UploadAsync(string projectId, string blueprintName, Stream content, Dictionary<string, string> metadata)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync();

            string blobName = $"{projectId}/{blueprintName}";
            var blobClient = containerClient.GetBlobClient(blobName);

            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = GetContentType(blueprintName)
            };

            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders,
                Metadata = metadata,
                TransferOptions = new StorageTransferOptions
                {
                    MaximumConcurrency = 4, // Optimizar para archivos grandes
                    InitialTransferSize = 8 * 1024 * 1024, // 8MB chunks
                    MaximumTransferSize = 4 * 1024 * 1024 // 4MB max chunk
                }
            };

            await blobClient.UploadAsync(content, uploadOptions);
        }
        catch (RequestFailedException ex)
        {
            // Manejar excepciones específicas de Azure
            throw new ApplicationException($"Error al subir el plano {blueprintName}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Descarga un plano del repositorio usando Stream.
    /// </summary>
    public async Task<Stream> DownloadAsync(string projectId, string blueprintName)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            string blobName = $"{projectId}/{blueprintName}";
            var blobClient = containerClient.GetBlobClient(blobName);

            var response = await blobClient.DownloadAsync();
            return response.Value.Content;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            throw new FileNotFoundException($"El plano {blueprintName} no existe en el proyecto {projectId}.", ex);
        }
        catch (RequestFailedException ex)
        {
            throw new ApplicationException($"Error al descargar el plano {blueprintName}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Elimina un plano del repositorio.
    /// </summary>
    public async Task DeleteAsync(string projectId, string blueprintName)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            string blobName = $"{projectId}/{blueprintName}";
            var blobClient = containerClient.GetBlobClient(blobName);

            await blobClient.DeleteIfExistsAsync();
        }
        catch (RequestFailedException ex)
        {
            throw new ApplicationException($"Error al eliminar el plano {blueprintName}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Lista los planos de un proyecto.
    /// </summary>
    public async Task<IEnumerable<BlueprintInfo>> ListAsync(string projectId)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobs = containerClient.GetBlobsAsync(prefix: $"{projectId}/");

            var blueprintInfos = new List<BlueprintInfo>();
            await foreach (var blobItem in blobs)
            {
                var info = new BlueprintInfo
                {
                    Name = Path.GetFileName(blobItem.Name),
                    CreatedDate = blobItem.Properties.CreatedOn?.DateTime ?? DateTime.MinValue,
                    Author = blobItem.Metadata != null && blobItem.Metadata.TryGetValue("author", out var author) ? author : "Desconocido",
                    Size = blobItem.Properties.ContentLength ?? 0
                };
                blueprintInfos.Add(info);
            }

            return blueprintInfos;
        }
        catch (RequestFailedException ex)
        {
            throw new ApplicationException($"Error al listar planos del proyecto {projectId}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Obtiene una URL SAS temporal para acceso seguro al plano.
    /// </summary>
    public async Task<string> GetSasUrlAsync(string projectId, string blueprintName, TimeSpan expiry)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            string blobName = $"{projectId}/{blueprintName}";
            var blobClient = containerClient.GetBlobClient(blobName);

            // Verificar que el blob existe
            await blobClient.GetPropertiesAsync();

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.Add(expiry)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasToken = blobClient.GenerateSasUri(sasBuilder).ToString();
            return sasToken;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            throw new FileNotFoundException($"El plano {blueprintName} no existe en el proyecto {projectId}.", ex);
        }
        catch (RequestFailedException ex)
        {
            throw new ApplicationException($"Error al generar SAS para {blueprintName}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Determina el tipo de contenido basado en la extensión del archivo.
    /// </summary>
    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".dwg" => "application/acad",
            ".jpeg" or ".jpg" => "image/jpeg",
            _ => "application/octet-stream"
        };
    }
}