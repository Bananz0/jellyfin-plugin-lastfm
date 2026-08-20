namespace Jellyfin.Plugin.Lastfm.Configuration
{
    using Models;
    using MediaBrowser.Model.Plugins;
    using Resources;

    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        public LastfmUser[] LastfmUsers { get; set; }
        public string LastfmApiHost { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration" /> class.
        /// </summary>
        public string ApiKey    { get; set; }
        public string ApiSecret { get; set; }

        public PluginConfiguration()
        {
            LastfmUsers = new LastfmUser[] { };
            LastfmApiHost = Strings.Endpoints.LastfmApi;
            ApiKey    = string.Empty;
            ApiSecret = string.Empty;
        }
    }
}
