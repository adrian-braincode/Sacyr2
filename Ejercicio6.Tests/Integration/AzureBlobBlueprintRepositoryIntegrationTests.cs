using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Ejercicio6.Tests.Integration
{
    public class AzureBlobBlueprintRepositoryIntegrationTests
    {
        private static string? GetConn() => Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");

        [Fact(Skip = "Requires AZURE_STORAGE_CONNECTION_STRING env var; set it to run integration tests")]
        public async Task SaveAndGetBlueprint_Stream_Works()
        {
            var conn = GetConn();
            if (string.IsNullOrEmpty(conn))
                return;

            var svc = new BlobServiceClient(conn);
            var container = "test-blueprints-" + Guid.NewGuid().ToString("n");
            var repo = new Ejercicio6.AzureBlobBlueprintRepository(svc, container, new NullLogger<Ejercicio6.AzureBlobBlueprintRepository>());

            var blueprintId = Guid.NewGuid();
            var contentText = "Hola mundo - contenido de prueba" + DateTime.UtcNow;
            var bytes = Encoding.UTF8.GetBytes(contentText);

            using (var ms = new MemoryStream(bytes))
            {
                var refInfo = await repo.SaveBlueprintAsync(blueprintId, ms, metadata: new System.Collections.Generic.Dictionary<string,string>{{"content-type","text/plain"}});
                Assert.NotNull(refInfo);
            }

            // Recuperar
            using var stream = await repo.GetBlueprintAsync(blueprintId);
            using var sr = new StreamReader(stream);
            var read = await sr.ReadToEndAsync();
            Assert.Contains("Hola mundo", read);
        }
    }
}
