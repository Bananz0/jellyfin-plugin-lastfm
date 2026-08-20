namespace Jellyfin.Plugin.Lastfm
{
    using System;
    using System.Collections.Generic;
    using Configuration;
    using MediaBrowser.Common.Configuration;
    using MediaBrowser.Common.Plugins;
    using MediaBrowser.Model.Plugins;
    using MediaBrowser.Model.Serialization;


    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        /// <summary>
        /// Flag set when an Import Syncing task is running
        /// </summary>
        public static bool Syncing { get; internal set; }


        public PluginConfiguration PluginConfiguration => Configuration;

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public override Guid Id { get; } = new Guid("f5cc9733-e4df-42f3-a950-12d62d5819cc");

        public override string Name
            => "Last.fm (Bananz0)";

        public override string Description
            => "Scrobble your music collection to Last.fm";

        public static Plugin Instance { get; private set; }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "lastfm",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html"
                },
                new PluginPageInfo
                {
                    Name = "lastfm-controller",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.configController.js"
                }
            };
        }
    }
}
