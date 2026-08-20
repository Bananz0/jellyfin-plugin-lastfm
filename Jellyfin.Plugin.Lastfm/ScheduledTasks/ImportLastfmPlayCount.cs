namespace Jellyfin.Plugin.Lastfm.ScheduledTasks
{
    using Api;
    using Jellyfin.Database.Implementations.Entities;
    using Jellyfin.Data.Enums;
    using MediaBrowser.Controller.Entities;
    using MediaBrowser.Controller.Entities.Audio;
    using MediaBrowser.Controller.Library;
    using MediaBrowser.Model.Entities;
    using MediaBrowser.Model.Tasks;
    using Microsoft.Extensions.Logging;
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Utils;

    /// <summary>
    /// Task that imports Last.fm play counts into the Jellyfin library.
    /// </summary>
    public class ImportLastfmPlayCount : IScheduledTask
    {
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataManager;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger<ImportLastfmPlayCount> _logger;
        private readonly LastfmApiClient _apiClient;

        public ImportLastfmPlayCount(IHttpClientFactory httpClientFactory, IUserManager userManager, IUserDataManager userDataManager, ILibraryManager libraryManager, ILoggerFactory loggerFactory)
        {
            _userManager = userManager;
            _userDataManager = userDataManager;
            _libraryManager = libraryManager;
            _logger = loggerFactory.CreateLogger<ImportLastfmPlayCount>();
            _apiClient = new LastfmApiClient(httpClientFactory, loggerFactory.CreateLogger<ImportLastfmPlayCount>());
        }

        public string Name        => "Import Last.fm Play Counts";
        public string Category    => "Last.fm";
        public string Key         => "ImportLastfmPlayCount";
        public string Description => "Import play counts from Last.fm library for each configured user";

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => Enumerable.Empty<TaskTriggerInfo>();

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var users = _userManager.GetUsers().Where(u =>
            {
                var user = UserHelpers.GetUser(u);
                return user != null && !string.IsNullOrWhiteSpace(user.SessionKey);
            }).ToList();

            if (users.Count == 0)
            {
                _logger.LogInformation("No users found");
                return;
            }

            Plugin.Syncing = true;

            var usersProcessed = 0;
            var totalUsers = users.Count;

            foreach (var user in users)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var progressOffset      = (double)usersProcessed++ / totalUsers;
                var maxProgressForStage = (double)usersProcessed   / totalUsers;

                await SyncPlayCountForUser(user, progress, cancellationToken, maxProgressForStage, progressOffset);
            }

            Plugin.Syncing = false;
        }

        private async Task SyncPlayCountForUser(User user, IProgress<double> progress, CancellationToken cancellationToken, double maxProgress, double progressOffset)
        {
            var lastFmUser = UserHelpers.GetUser(user);
            if (!lastFmUser.Options.SyncPlayCount)
            {
                _logger.LogInformation("Play count sync disabled for {User}, skipping", user.Username);
                return;
            }

            _logger.LogInformation("Starting Last.fm play count sync for {User}", user.Username);

            // Fetch all tracks from Last.fm library (paginated)
            var lastfmTracks = await FetchAllTracks(lastFmUser, progress, cancellationToken, maxProgress, progressOffset);

            if (lastfmTracks.Count == 0)
            {
                _logger.LogInformation("No tracks found in Last.fm library for {User}", user.Username);
                return;
            }

            // Build lookup by MusicBrainz artist ID, skipping tracks without one
            var tracksByArtist = lastfmTracks
                .Where(t => !string.IsNullOrEmpty(t.Artist?.MusicBrainzId))
                .GroupBy(t => t.Artist.MusicBrainzId)
                .ToDictionary(g => g.Key, g => g.ToList());

            _logger.LogInformation("Retrieved {TrackCount} tracks from Last.fm for {User} ({ArtistCount} artists with MusicBrainz IDs)",
                lastfmTracks.Count, user.Username, tracksByArtist.Count);

            var artists = _libraryManager.GetArtists(new InternalItemsQuery(user))
                .Items
                .Select(i => i.Item1)
                .Cast<MusicArtist>()
                .ToList();

            int matched = 0;
            int updated = 0;

            foreach (var artist in artists)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var artistMBid = Helpers.GetMusicBrainzArtistId(artist);
                if (string.IsNullOrEmpty(artistMBid) || !tracksByArtist.ContainsKey(artistMBid))
                    continue;

                var artistLastfmTracks = tracksByArtist[artistMBid];

                foreach (var song in artist.GetTaggedItems(new InternalItemsQuery(user)
                {
                    IncludeItemTypes = new[] { BaseItemKind.Audio },
                    EnableTotalRecordCount = false
                }).OfType<Audio>())
                {
                    var lastfmTrack = artistLastfmTracks.FirstOrDefault(t => StringHelper.IsLike(song.Name, t.Name));
                    if (lastfmTrack == null || lastfmTrack.PlayCount <= 0)
                        continue;

                    matched++;

                    var userData = _userDataManager.GetUserData(user, song);
                    if (userData.PlayCount == lastfmTrack.PlayCount)
                        continue;

                    _logger.LogDebug("Updating play count for {Artist} - {Track}: {Old} -> {New}",
                        artist.Name, song.Name, userData.PlayCount, lastfmTrack.PlayCount);

                    userData.PlayCount = lastfmTrack.PlayCount;
                    _userDataManager.SaveUserData(user, song, userData, UserDataSaveReason.UpdateUserRating, cancellationToken);
                    updated++;
                }
            }

            _logger.LogInformation("Finished Last.fm play count sync for {User}. Matched: {Matched}, Updated: {Updated}", user.Username, matched, updated);
        }

        private async Task<List<LastfmTrack>> FetchAllTracks(LastfmUser lastfmUser, IProgress<double> progress, CancellationToken cancellationToken, double maxProgress, double progressOffset)
        {
            var tracks = new List<LastfmTrack>();
            int page = 1;

            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                var response = await _apiClient.GetTopTracks(lastfmUser, cancellationToken, page++).ConfigureAwait(false);

                if (response == null || !response.HasTracks())
                    break;

                var resolved = response.ResolvedTracks;
                tracks.AddRange(resolved.Tracks);

                var currentProgress = ((double)resolved.Metadata.Page / resolved.Metadata.TotalPages)
                    * (maxProgress - progressOffset) + progressOffset;
                progress.Report(currentProgress * 100);

                if (resolved.Metadata.IsLastPage())
                    break;

            } while (true);

            return tracks;
        }
    }
}
