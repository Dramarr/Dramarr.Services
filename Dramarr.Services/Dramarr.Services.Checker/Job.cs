using Dramarr.Core.Retry;
using Dramarr.Data.Model;
using Dramarr.Data.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Dramarr.Core.Enums.EnumsHelpers;
using static Dramarr.Core.Enums.SourceHelpers;

namespace Dramarr.Services.Checker
{
    public class Job
    {
        public string ConnectionString { get; set; }
        public TimeSpan Timeout { get; set; }

        private Scrapers.MyAsianTv.Manager MATScraper;
        private Scrapers.EstrenosDoramas.Manager ESScraper;
        private LogRepository LogRepository;

        public Job(string connectionString, TimeSpan timeout)
        {
            ConnectionString = connectionString;
            Timeout = timeout;

            var MATEpisodeUrl = $"https://myasiantv.to/drama/<dorama>/download/";
            var MATAllShowsUrl = $"https://myasiantv.to/";
            var MATLatestEpisodesUrl = $"https://myasiantv.to/";
            MATScraper = new Scrapers.MyAsianTv.Manager(MATEpisodeUrl, MATAllShowsUrl, MATLatestEpisodesUrl);

            var ESShowUrl = "https://www.estrenosdoramas.net/";
            ESScraper = new Scrapers.EstrenosDoramas.Manager(ESShowUrl);

            LogRepository = new LogRepository(ConnectionString);
        }

        public void Run() => TaskHelpers.Retry(Logic, Timeout);

        public bool Logic()
        {
            try
            {
                LogRepository.Create(new Log(Core.Enums.LogHelpers.LogType.INFO, "Starting Checker logic", null));

                var showRepo = new ShowRepository(ConnectionString);
                var episodeRepo = new EpisodeRepository(ConnectionString);

                LogRepository.Create(new Log(Core.Enums.LogHelpers.LogType.DEBUG, "Getting enabled shows in database", null));
                var showsInDatabase = showRepo.Select().Where(x => x.Enabled == true).ToList();
                LogRepository.Create(new Log(Core.Enums.LogHelpers.LogType.DEBUG, $"Found {showsInDatabase.Count} shows in database", null));

                LogRepository.Create(new Log(Core.Enums.LogHelpers.LogType.DEBUG, "Getting episodes in database", null));
                var episodesInDatabase = episodeRepo.Select();
                LogRepository.Create(new Log(Core.Enums.LogHelpers.LogType.DEBUG, $"Found {episodesInDatabase.Count} episodes in database", null));

                LogRepository.Create(new Log(Core.Enums.LogHelpers.LogType.INFO, "Looping through shows", null));
                foreach (var show in showsInDatabase)
                {
                    // Get total episodes and status
                    LogRepository.Create(new Log(Core.Enums.LogHelpers.LogType.DEBUG, "Getting show data from source", null));
                    var status = GetStatus(show.Source, show.Url);

                    // Check if current amount of episodes == total episodes and status is downloaded
                    LogRepository.Create(new Log(Core.Enums.LogHelpers.LogType.DEBUG, "Getting episodes downloaded", null));
                    var episodesByShow = episodesInDatabase.Where(x => x.ShowId == show.Id && x.Status == EpisodeStatus.DOWNLOADED).ToList();
                    LogRepository.Create(new Log(Core.Enums.LogHelpers.LogType.DEBUG, $"Found {episodesByShow} episodes downloaded", null));

                    // if yes then disable drama -> Enabled = false
                    LogRepository.Create(new Log(Core.Enums.LogHelpers.LogType.INFO, "Checking if it has to be disabled", null));
                    if (episodesByShow.Count == status.Item1 && status.Item2)
                    {
                        LogRepository.Create(new Log(Core.Enums.LogHelpers.LogType.INFO, $"Disabling show {show.Title}", null));
                        show.Enabled = false;
                        showRepo.Update(show);
                    }
                }
            }
            catch (Exception e)
            {
                LogRepository.Create(new Log(Core.Enums.LogHelpers.LogType.ERROR, e.Message, e.StackTrace));
            }

            return true;
        }

        private Tuple<int, bool> GetStatus(Source source, string url)
        {
            return source switch
            {
                Source.MYASIANTV => MATScraper.GetStatus(url),
                Source.ESTRENOSDORAMAS => ESScraper.GetStatus(url),
                _ => null,
            };
        }
    }
}
