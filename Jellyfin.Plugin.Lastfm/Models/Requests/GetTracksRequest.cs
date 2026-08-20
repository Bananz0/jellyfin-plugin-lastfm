namespace Jellyfin.Plugin.Lastfm.Models.Requests
{
    using System.Collections.Generic;

    public class GetTracksRequest : BaseRequest, IPagedRequest
    {
        public string User   { get; set; }
        public string Artist { get; set; }
        public string Period { get; set; }
        public int    Limit  { get; set; }
        public int    Page   { get; set; }

        public override Dictionary<string, string> ToDictionary()
        {
            var dict = new Dictionary<string, string>(base.ToDictionary())
            {
                { "user",  User  },
                { "limit", Limit.ToString() },
                { "page",  Page.ToString()  }
            };
            if (!string.IsNullOrEmpty(Artist)) dict["artist"] = Artist;
            if (!string.IsNullOrEmpty(Period)) dict["period"] = Period;
            return dict;
        }
    }
}
