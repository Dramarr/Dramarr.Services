using Dramarr.Core.Download;
using Dramarr.Core.Retry;
using Dramarr.Data.Model;
using Dramarr.Data.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Dramarr.Core.Enums.EnumsHelpers;
using static Dramarr.Core.Enums.SourceHelpers;

namespace Dramarr.Services.Downloader
{
    public class Job
    {
        public string ConnectionString { get; set; }
        public TimeSpan Timeout { get; set; }
        public string Path { get; set; }

        private LogRepository LogRepository;

        public Job(string connectionString, TimeSpan timeout, string path)
        {
            ConnectionString = connectionString;
            Timeout = timeout;
            Path = path;

            LogRepository = new LogRepository(ConnectionString);
        }

        public void Run() => TaskHelpers.Retry(Logic, Timeout);

        public bool Logic()
        {
            try
            {
                LogRepository.Create(new Log(Core.Enums.LogHelpers.LogType.INFO, "Starting Downloader logic", null));

                var showRepo = new ShowRepository(ConnectionString);
                var episodeRepo = new EpisodeRepository(ConnectionString);

                LogRepository.Create(new Log(Core.Enums.LogHelpers.LogType.DEBUG, $"Getting shows from database", null));
                var showsInDatabase = showRepo.Select().Where(x => x.Download == true && x.Enabled == true).ToList();
                LogRepository.Create(new Log(Core.Enums.LogHelpers.LogType.DEBUG, $"Found {showsInDatabase.Count} shows in database", null));

                LogRepository.Create(new Log(Core.Enums.LogHelpers.LogType.DEBUG, $"Getting episodes from database", null));
                var episodesInDatabase = episodeRepo.Select();
                LogRepository.Create(new Log(Core.Enums.LogHelpers.LogType.DEBUG, $"Found {episodesInDatabase.Count} in database", null));

                LogRepository.Create(new Log(Core.Enums.LogHelpers.LogType.INFO, $"Looping through shows that are enabled to download", null));
                foreach (var show in showsInDatabase)
                {
                    LogRepository.Create(new Log(Core.Enums.LogHelpers.LogType.DEBUG, $"Checking episodes scraped", null));
                    var episodesByShow = episodesInDatabase.Where(x => x.ShowId == show.Id && x.Status == EpisodeStatus.SCRAPED).OrderBy(y => y.Filename).ToList();
                    LogRepository.Create(new Log(Core.Enums.LogHelpers.LogType.DEBUG, $"Found {episodesByShow.Count} to download", null));

                    foreach (var episode in episodesByShow)
                    {
                        LogRepository.Create(new Log(Core.Enums.LogHelpers.LogType.INFO, $"Downloading {episode.Filename}", null));
                        var path = System.IO.Path.Combine(Path, show.Title);
                        episode.Status = DownloadHelpers.DownloadFile(episode.Url, path, episode.Filename) ? EpisodeStatus.DOWNLOADED : EpisodeStatus.FAILED;

                        var downloadMessage = episode.Status == EpisodeStatus.DOWNLOADED ? "successfully" : "failed";
                        LogRepository.Create(new Log(Core.Enums.LogHelpers.LogType.INFO, $"Downladed {downloadMessage}", null));

                        episodeRepo.Update(episode);
                        LogRepository.Create(new Log(Core.Enums.LogHelpers.LogType.DEBUG, $"Updated show", null));
                    }
                }
            }
            catch (Exception e)
            {
                LogRepository.Create(new Log(Core.Enums.LogHelpers.LogType.INFO, e.Message, e.StackTrace));
            }

            return true;
        }

    }
}
