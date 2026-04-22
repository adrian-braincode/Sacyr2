using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AzureBlobRepository;

/// <summary>
/// Representa la información de un plano.
/// </summary>
public class BlueprintInfo
{
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string Author { get; set; } = string.Empty;
    public long Size { get; set; }
}

/// <summary>
/// Interfaz para el repositorio de planos técnicos.
/// </summary>
public interface IBlueprintRepository
{
    /// <summary>
    /// Sube un plano al repositorio.
    /// </summary>
    Task UploadAsync(string projectId, string blueprintName, Stream content, Dictionary<string, string> metadata);

    /// <summary>
    /// Descarga un plano del repositorio.
    /// </summary>
    Task<Stream> DownloadAsync(string projectId, string blueprintName);

    /// <summary>
    /// Elimina un plano del repositorio.
    /// </summary>
    Task DeleteAsync(string projectId, string blueprintName);

    /// <summary>
    /// Lista los planos de un proyecto.
    /// </summary>
    Task<IEnumerable<BlueprintInfo>> ListAsync(string projectId);

    /// <summary>
    /// Obtiene una URL SAS temporal para acceso seguro al plano.
    /// </summary>
    Task<string> GetSasUrlAsync(string projectId, string blueprintName, TimeSpan expiry);
}
