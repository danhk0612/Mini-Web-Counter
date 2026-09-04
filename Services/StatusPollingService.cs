using System.Net.Http.Json;
using JCMS_Mini_Monitoring.Models;

namespace JCMS_Mini_Monitoring.Services;

public sealed class StatusPollingService : IDisposable
{
    private readonly HttpClient _httpClient = new();

    public async Task<StatusData?> GetStatusAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        return await _httpClient.GetFromJsonAsync<StatusData>(url, cancellationToken);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
