using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace One.Inception.Api;

public class MonitorClient
{
    private readonly JsonSerializerOptions options;

    private readonly HttpClient _client;

    private static readonly ILogger logger = InceptionLogger.CreateLogger(typeof(InceptionApi));

    public MonitorClient(HttpClient client)
    {
        _client = client;
        options = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<string>> GetBoundedContextListAsync()
    {
        try
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/Monitor/GetLiveServices");

            var response = await ExecuteRequestAsync<List<string>>(request);

            return response.Data;
        }
        catch (Exception ex)
        {
            logger.LogInformation($"Probably the domain for Inception Client is not the right one. {ex.Message}");
            return new List<string>();
        }
    }

    public async Task<List<string>> GetTenantListAsync()
    {
        try
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/Monitor/GetTenants");

            var response = await ExecuteRequestAsync<List<string>>(request);

            return response.Data;
        }
        catch (Exception ex)
        {
            logger.LogInformation($"Probably the domain for Inception Client is not the right one. {ex.Message}");
            return new List<string>();
        }
    }

    protected async Task<(HttpResponseMessage Response, T Data)> ExecuteRequestAsync<T>(HttpRequestMessage request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        try
        {
            using (HttpResponseMessage response = await _client.SendAsync(request).ConfigureAwait(false))
            {
                if (response.IsSuccessStatusCode)
                {
                    using (var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    {
                        contentStream.Position = 0;
                        T responseObject = await JsonSerializer.DeserializeAsync<T>(contentStream, options).ConfigureAwait(false);
                        return (response, responseObject);
                    }
                }
                else
                {
                    return (response, default);
                }
            }
        }
        catch (Exception)
        {

            return (new HttpResponseMessage(), default);
        }

    }
}
