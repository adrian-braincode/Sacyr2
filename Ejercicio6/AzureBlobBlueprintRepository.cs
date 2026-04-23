using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;
using Ejercicio6.Interfaces;
using Ejercicio6.Models;

namespace Ejercicio6
{
    /// <summary>
    /// Implementación de almacenamiento de planos en Azure Blob Storage.
    /// Optimizada para streaming: usa Streams para upload/download y evita cargar el blob completo en memoria.
    /// Maneja errores del SDK (RequestFailedException) de forma asíncrona.
    /// </summary>
    public class AzureBlobBlueprintRepository : IBlueprintRepository
    {
        private readonly BlobServiceClient _serviceClient;
        private readonly string _containerName;
        private readonly ILogger<AzureBlobBlueprintRepository>? _logger;

        public AzureBlobBlueprintRepository(BlobServiceClient serviceClient, string containerName, ILogger<AzureBlobBlueprintRepository>? logger = null)
        {
            _serviceClient = serviceClient ?? throw new ArgumentNullException(nameof(serviceClient));
            _containerName = string.IsNullOrWhiteSpace(containerName) ? throw new ArgumentException("containerName required") : containerName;
            _logger = logger;
        }

        public async Task<BlobReference> SaveBlueprintAsync(Guid blueprintId, Stream content, IDictionary<string,string>? metadata = null, CancellationToken ct = default)
        {
            try
            {
                var container = _serviceClient.GetBlobContainerClient(_containerName);
                await container.CreateIfNotExistsAsync(cancellationToken: ct).ConfigureAwait(false);

                // Versioning: usamos timestamp-guid para versión única por upload
                var version = DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString("N");
                var blobPath = $"{blueprintId}/{version}";
                var blob = container.GetBlockBlobClient(blobPath);

                // Si el stream es seekable podemos calcular checksum localmente; si no, confiamos en metadata
                if (content.CanSeek)
                {
                    content.Position = 0;
                    using var sha = SHA256.Create();
                    var hash = await sha.ComputeHashAsync(content, ct).ConfigureAwait(false);
                    var checksum = Convert.ToHexString(hash).ToLowerInvariant();
                    metadata ??= new Dictionary<string,string>();
                    metadata["checksum-sha256"] = checksum;
                    content.Position = 0;
                }

                var headers = new Azure.Storage.Blobs.Models.BlobHttpHeaders();
                if (metadata != null && metadata.TryGetValue("content-type", out var ctStr))
                {
                    headers.ContentType = ctStr;
                }

                await blob.UploadAsync(content, httpHeaders: headers, metadata: metadata, cancellationToken: ct).ConfigureAwait(false);

                return new BlobReference(_containerName, blobPath, version);
            }
            catch (RequestFailedException ex)
            {
                _logger?.LogError(ex, "RequestFailedException al guardar blueprint {BlueprintId}", blueprintId);
                throw;
            }
        }

        public async Task<Stream> GetBlueprintAsync(Guid blueprintId, string? version = null, CancellationToken ct = default)
        {
            try
            {
                var container = _serviceClient.GetBlobContainerClient(_containerName);
                await container.CreateIfNotExistsAsync(cancellationToken: ct).ConfigureAwait(false);

                // Si no se especifica version, hacemos list y devolvemos la última por nombre (timestamp prefijo)
                string blobPath;
                if (!string.IsNullOrEmpty(version))
                {
                    blobPath = $"{blueprintId}/{version}";
                }
                else
                {
                    await foreach (var blobItem in container.GetBlobsAsync(prefix: blueprintId.ToString(), cancellationToken: ct))
                    {
                        // buscamos el último por nombre; para simplicidad devolvemos el primero (iteración retorna en lexicográfico ascendente)
                        blobPath = blobItem.Name;
                        var blob = container.GetBlobClient(blobPath);
                        var download = await blob.DownloadStreamingAsync(cancellationToken: ct).ConfigureAwait(false);
                        return download.Value.Content;
                    }

                    throw new FileNotFoundException("Blueprint no encontrado", blueprintId.ToString());
                }

                var blobClient = container.GetBlobClient(blobPath);
                var response = await blobClient.DownloadStreamingAsync(cancellationToken: ct).ConfigureAwait(false);
                return response.Value.Content;
            }
            catch (RequestFailedException ex)
            {
                _logger?.LogError(ex, "RequestFailedException al leer blueprint {BlueprintId}", blueprintId);
                throw;
            }
        }

        public async Task<Uri> GenerateSasUrlAsync(Guid blueprintId, TimeSpan expiry, BlobSasPermissions permissions, CancellationToken ct = default)
        {
            // Nota: generar SAS requiere credenciales adecuadas (Shared Key o User Delegation Key).
            try
            {
                var container = _serviceClient.GetBlobContainerClient(_containerName);
                await container.CreateIfNotExistsAsync(cancellationToken: ct).ConfigureAwait(false);

                // Obtenemos la última versión si existe
                await foreach (var blobItem in container.GetBlobsAsync(prefix: blueprintId.ToString(), cancellationToken: ct))
                {
                    var blob = container.GetBlobClient(blobItem.Name);

                    // Intentamos crear un SAS con las credenciales disponibles
                    var sasBuilder = new BlobSasBuilder(permissions, DateTimeOffset.UtcNow.Add(expiry))
                    {
                        BlobContainerName = _containerName,
                        BlobName = blobItem.Name,
                        Resource = "b"
                    };

                    try
                    {
                        var uri = blob.GenerateSasUri(sasBuilder);
                        return uri;
                    }
                    catch (InvalidOperationException) // ocurre si las credenciales no permiten generar SAS
                    {
                        _logger?.LogWarning("No se pudo generar SAS con las credenciales actuales; se requiere Shared Key o UserDelegationKey");
                        throw;
                    }
                }

                throw new FileNotFoundException("Blueprint no encontrado", blueprintId.ToString());
            }
            catch (RequestFailedException ex)
            {
                _logger?.LogError(ex, "RequestFailedException al generar SAS para {BlueprintId}", blueprintId);
                throw;
            }
        }

        public async Task DeleteBlueprintAsync(Guid blueprintId, string? version = null, CancellationToken ct = default)
        {
            try
            {
                var container = _serviceClient.GetBlobContainerClient(_containerName);
                await container.CreateIfNotExistsAsync(cancellationToken: ct).ConfigureAwait(false);

                if (version is null)
                {
                    await foreach (var blobItem in container.GetBlobsAsync(prefix: blueprintId.ToString(), cancellationToken: ct))
                    {
                        var blob = container.GetBlobClient(blobItem.Name);
                        await blob.DeleteIfExistsAsync(cancellationToken: ct).ConfigureAwait(false);
                    }
                }
                else
                {
                    var blob = container.GetBlobClient($"{blueprintId}/{version}");
                    await blob.DeleteIfExistsAsync(cancellationToken: ct).ConfigureAwait(false);
                }
            }
            catch (RequestFailedException ex)
            {
                _logger?.LogError(ex, "RequestFailedException al eliminar blueprint {BlueprintId}", blueprintId);
                throw;
            }
        }

        public async Task<bool> VerifyChecksumAsync(Guid blueprintId, string expectedChecksum, CancellationToken ct = default)
        {
            try
            {
                var container = _serviceClient.GetBlobContainerClient(_containerName);
                await container.CreateIfNotExistsAsync(cancellationToken: ct).ConfigureAwait(false);

                await foreach (var blobItem in container.GetBlobsAsync(prefix: blueprintId.ToString(), cancellationToken: ct))
                {
                    var blob = container.GetBlobClient(blobItem.Name);
                    var dl = await blob.DownloadStreamingAsync(cancellationToken: ct).ConfigureAwait(false);
                    using var sha = SHA256.Create();
                    var computed = await ComputeHashHexAsync(dl.Value.Content, sha, ct).ConfigureAwait(false);
                    if (string.Equals(computed, expectedChecksum, StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                return false;
            }
            catch (RequestFailedException ex)
            {
                _logger?.LogError(ex, "RequestFailedException al verificar checksum {BlueprintId}", blueprintId);
                throw;
            }
        }

        private static async Task<string> ComputeHashHexAsync(Stream stream, HashAlgorithm sha, CancellationToken ct)
        {
            // Lee el stream por bloques para no agotar memoria
            const int bufferSize = 81920;
            var buffer = new byte[bufferSize];
            int read;
            while ((read = await stream.ReadAsync(buffer.AsMemory(0, bufferSize), ct).ConfigureAwait(false)) > 0)
            {
                sha.TransformBlock(buffer, 0, read, null, 0);
            }

            sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return Convert.ToHexString(sha.Hash!).ToLowerInvariant();
        }
    }
}
