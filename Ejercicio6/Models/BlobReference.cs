using System;

namespace Ejercicio6.Models
{
    /// <summary>
    /// Referencia ligera a un blob almacenado (container + path + version)
    /// </summary>
    public record BlobReference(string Container, string BlobPath, string? Version = null)
    {
        public Uri ToUri(string accountBlobEndpoint)
        {
            return new Uri($"{accountBlobEndpoint.TrimEnd('/')}/{Container}/{BlobPath}");
        }
    }
}
