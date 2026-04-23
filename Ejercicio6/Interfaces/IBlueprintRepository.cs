using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Ejercicio6.Models;
using Azure.Storage.Sas;

namespace Ejercicio6.Interfaces
{
    public interface IBlueprintRepository
    {
        Task<BlobReference> SaveBlueprintAsync(Guid blueprintId, Stream content, IDictionary<string,string>? metadata = null, CancellationToken ct = default);
        Task<Stream> GetBlueprintAsync(Guid blueprintId, string? version = null, CancellationToken ct = default);
        Task<Uri> GenerateSasUrlAsync(Guid blueprintId, TimeSpan expiry, BlobSasPermissions permissions, CancellationToken ct = default);
        Task DeleteBlueprintAsync(Guid blueprintId, string? version = null, CancellationToken ct = default);
        Task<bool> VerifyChecksumAsync(Guid blueprintId, string expectedChecksum, CancellationToken ct = default);
    }
}
