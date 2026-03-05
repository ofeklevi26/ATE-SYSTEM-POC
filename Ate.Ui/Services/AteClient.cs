using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Ate.Contracts;

namespace Ate.Ui.Services;

public sealed class AteClient
{
    private readonly HttpClient _httpClient;

    public AteClient(string baseAddress = "http://localhost:9000/")
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseAddress) };
    }

    public async Task<DeviceCommandResponse?> SendCommandAsync(DeviceCommandRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/command", request).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DeviceCommandResponse>().ConfigureAwait(false);
    }

    public async Task<EngineStatus?> GetStatusAsync()
    {
        return await _httpClient.GetFromJsonAsync<EngineStatus>("api/status").ConfigureAwait(false);
    }

    public Task PauseAsync() => _httpClient.PostAsync("api/engine/pause", null);

    public Task ResumeAsync() => _httpClient.PostAsync("api/engine/resume", null);

    public Task ClearAsync() => _httpClient.PostAsync("api/engine/clear", null);

    public Task AbortCurrentAsync() => _httpClient.PostAsync("api/engine/abort-current", null);
}
