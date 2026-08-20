namespace Jellyfin.Plugin.Lastfm.Models.Responses
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class GetTracksResponse : BaseResponse
    {
        // library.getTracks wraps in "tracks"; user.getTopTracks wraps in "toptracks"
        [JsonPropertyName("tracks")]
        public GetTracksTracks Tracks { get; set; }

        [JsonPropertyName("toptracks")]
        public GetTracksTracks TopTracks { get; set; }

        public GetTracksTracks ResolvedTracks => Tracks ?? TopTracks;

        public bool HasTracks()
        {
            var t = ResolvedTracks;
            return t != null && t.Tracks != null && t.Tracks.Count > 0;
        }
    }

    public class GetTracksTracks
    {
        [JsonPropertyName("track")]
        public List<LastfmTrack> Tracks { get; set; }

        [JsonPropertyName("@attr")]
        public GetTracksMeta Metadata { get; set; }
    }

    public class GetTracksMeta
    {
        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("total")]
        public int TotalTracks { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; }

        public bool IsLastPage()
        {
            return Page.Equals(TotalPages);
        }
    }
}
