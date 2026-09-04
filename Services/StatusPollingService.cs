using System.Text.Json;
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

        var json = await _httpClient.GetStringAsync(url, cancellationToken);
        using var document = JsonDocument.Parse(json);

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var result = new StatusData();

        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetDecimal(out var number))
            {
                result.SetValue(property.Name, number);
            }
        }

        return result;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
