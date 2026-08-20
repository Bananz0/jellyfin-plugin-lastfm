namespace Jellyfin.Plugin.Lastfm.Resources
{
    public static class Strings
    {
        public static class Endpoints
        {
            public static string LastfmApi  = "ws.audioscrobbler.com";
        }

        public static class Methods
        {
            // Last.FM API specs located at https://www.last.fm/api
            public static string Scrobble         = "track.scrobble";
            public static string NowPlaying       = "track.updateNowPlaying";
            public static string GetMobileSession = "auth.getMobileSession";
            public static string TrackLove        = "track.love";
            public static string TrackUnlove      = "track.unlove";
            public static string GetLovedTracks   = "user.getLovedTracks";
            public static string GetTopTracks     = "user.getTopTracks";
        }

        public static class Keys
        {
            public static string LastfmApiKey    => Plugin.Instance?.PluginConfiguration?.ApiKey    ?? string.Empty;
            public static string LastfmApiSecret => Plugin.Instance?.PluginConfiguration?.ApiSecret ?? string.Empty;
        }
    }
}
