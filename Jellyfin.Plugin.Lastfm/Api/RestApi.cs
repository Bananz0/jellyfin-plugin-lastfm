using Microsoft.Extensions.Logging;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Jellyfin.Plugin.Lastfm.Api
{

    [ApiController]
    [Route("Lastfm/Login")]
    public class RestApi : ControllerBase
    {
        private readonly LastfmApiClient _apiClient;
        private readonly ILogger<RestApi> _logger;
        private static readonly object _apiHostLock = new();

        public RestApi(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<RestApi>();
            _apiClient = new LastfmApiClient(httpClientFactory, _logger);
        }

        [HttpPost]
        [Consumes("application/json")]
        public object CreateMobileSession([FromBody] LastFMUser lastFMUser)
        {
            _logger.LogInformation("Fetching Last.fm mobilesession auth for Username={0}", lastFMUser.Username);
            return ExecuteWithConfigOverride(lastFMUser, () => _apiClient.RequestSession(lastFMUser.Username, lastFMUser.Password).Result);
        }

        private static object ExecuteWithConfigOverride(LastFMUser request, Func<object> action)
        {
            lock (_apiHostLock)
            {
                var config = Plugin.Instance?.PluginConfiguration;
                if (config == null)
                {
                    return action();
                }

                var originalHost   = config.LastfmApiHost;
                var originalKey    = config.ApiKey;
                var originalSecret = config.ApiSecret;

                if (!string.IsNullOrWhiteSpace(request.ApiHost))
                    config.LastfmApiHost = request.ApiHost;
                if (!string.IsNullOrWhiteSpace(request.ApiKey))
                    config.ApiKey = request.ApiKey;
                if (!string.IsNullOrWhiteSpace(request.ApiSecret))
                    config.ApiSecret = request.ApiSecret;

                try
                {
                    var result = action();
                    // Persist so all future API calls use the supplied key/secret
                    Plugin.Instance.SaveConfiguration();
                    return result;
                }
                catch
                {
                    config.LastfmApiHost = originalHost;
                    config.ApiKey        = originalKey;
                    config.ApiSecret     = originalSecret;
                    throw;
                }
            }
        }
    }

    public class LastFMUser
    {
        public string Username  { get; set; }
        public string Password  { get; set; }
        public string ApiHost   { get; set; }
        public string ApiKey    { get; set; }
        public string ApiSecret { get; set; }
    }
}
