using Google.Cloud.SecretManager.V1;
using System;
using System.Threading.Tasks;

public class SecretManagerService
{
    private readonly SecretManagerServiceClient _client;

    public SecretManagerService()
    {
        _client = SecretManagerServiceClient.Create();
    }

    public async Task<string> GetSecretAsync(string projectId, string secretId, string versionId = "latest")
    {
        try
        {
            // Construct the name of the secret version
            SecretVersionName secretVersionName = new SecretVersionName(projectId, secretId, versionId);

            // Access the secret version
            AccessSecretVersionResponse result = await _client.AccessSecretVersionAsync(secretVersionName);

            // Return the secret payload as a string
            return result.Payload.Data.ToStringUtf8();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error accessing secret {secretId}: {ex.Message}");
            throw;
        }
    }
}
