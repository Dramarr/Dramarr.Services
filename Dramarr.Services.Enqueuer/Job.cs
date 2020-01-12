using Dramarr.Core.Download;
using Dramarr.Core.Retry;
using Dramarr.Data.Model;
using Dramarr.Data.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Dramarr.Core.Enums.EpisodeHelpers;
using static Dramarr.Core.Enums.SourceHelpers;

namespace Dramarr.Services.Enqueuer
{
    public class Job
    {
        public string ConnectionString { get; set; }
        public TimeSpan Timeout { get; set; }

        private Scrapers.MyAsianTv.Manager MATScraper;
        private Scrapers.EstrenosDoramas.Manager ESScraper;
        private Scrapers.Kshow.Manager KSScraper;
        private LogRepository LogRepository;

        public Job(string connectionString, TimeSpan timeout)
        {
            ConnectionString = connectionString;
            Timeout = timeout;

            LogRepository = new LogRepository(ConnectionString);

            var MATEpisodeUrl = $"https://myasiantv.to/drama/<dorama>/download/";
            var MATAllShowsUrl = $"https://myasiantv.to/";
            var MATLatestEpisodesUrl = $"https://myasiantv.to/";
            MATScraper = new Scrapers.MyAsianTv.Manager(MATEpisodeUrl, MATAllShowsUrl, MATLatestEpisodesUrl);

            var ESShowUrl = "https://www.estrenosdoramas.net/";
            ESScraper = new Scrapers.EstrenosDoramas.Manager(ESShowUrl);

            var KSShowUrl = "https://kshow.to/";
            KSScraper = new Scrapers.Kshow.Manager(KSShowUrl);
        }

        public void Run() => TaskHelpers.Retry(Logic, Timeout);

        public bool Logic()
        {
            var logs = new List<Log>();

            try
            {
                logs.Add(new Log(Core.Enums.LogHelpers.LogType.INFO, "Starting Enqueuer logic", null));

                var showRepo = new ShowRepository(ConnectionString);
                var episodeRepo = new EpisodeRepository(ConnectionString);

                logs.Add(new Log(Core.Enums.LogHelpers.LogType.DEBUG, $"Getting shows from database", null));
                var showsInDatabase = showRepo.Select().Where(x => x.Download ?? true).ToList();
                logs.Add(new Log(Core.Enums.LogHelpers.LogType.DEBUG, $"Found {showsInDatabase.Count} shows in database", null));

                logs.Add(new Log(Core.Enums.LogHelpers.LogType.INFO, "Looping through shows", null));
                foreach (var show in showsInDatabase)
                {
                    logs.Add(new Log(Core.Enums.LogHelpers.LogType.INFO, $"Checking {show.Title}", null));

                    logs.Add(new Log(Core.Enums.LogHelpers.LogType.DEBUG, $"Getting episodes in database of show: {show.Title}", null));
                    var episodesInDatabase = episodeRepo.Select().Where(x => x.ShowId == show.Id).ToList();
                    logs.Add(new Log(Core.Enums.LogHelpers.LogType.DEBUG, $"Found {episodesInDatabase.Count} episodes in database of show: {show.Title}", null));

                    logs.Add(new Log(Core.Enums.LogHelpers.LogType.DEBUG, $"Getting episodes of show: {show.Title} that status is not downloaded", null));
                    var episodesByShow = episodesInDatabase.Where(x => x.Status == EpisodeStatus.UNKNOWN || x.Status == EpisodeStatus.FAILED && x.Status != EpisodeStatus.DOWNLOADED).ToList();
                    logs.Add(new Log(Core.Enums.LogHelpers.LogType.DEBUG, $"Found {episodesByShow} episodes in database of show: {show.Title}", null));

                    logs.Add(new Log(Core.Enums.LogHelpers.LogType.DEBUG, $"Getting episodes of show: {show.Title} in sources", null));
                    var newepisodes = GetEpisodesBySource(episodesByShow, show);
                    logs.Add(new Log(Core.Enums.LogHelpers.LogType.DEBUG, $"Found {newepisodes.Count} episodes of show: {show.Title} in sources", null));


                    logs.Add(new Log(Core.Enums.LogHelpers.LogType.INFO, $"Looping through {show.Title} episodes", null));
                    foreach (var item in newepisodes)
                    {
                        logs.Add(new Log(Core.Enums.LogHelpers.LogType.INFO, $"Checking {item.Filename}", null));

                        logs.Add(new Log(Core.Enums.LogHelpers.LogType.DEBUG, $"Checking if episode exists", null));
                        var ep = episodesByShow.SingleOrDefault(x => x.Filename == item.Filename);

                        logs.Add(new Log(Core.Enums.LogHelpers.LogType.DEBUG, $"Checking if episode exists as scraped or downloaded", null));
                        var existsAsScraperOrDownloaded = episodesInDatabase.SingleOrDefault(x => x.Filename == item.Filename && (x.Status == EpisodeStatus.SCRAPED || x.Status == EpisodeStatus.DOWNLOADED));

                        if (existsAsScraperOrDownloaded != null)
                        {
                            logs.Add(new Log(Core.Enums.LogHelpers.LogType.DEBUG, $"Episode already downloaded", null));
                            continue;
                        }

                        if (ep != null)
                        {
                            if (item.Status != ep.Status && ep.Status != EpisodeStatus.DOWNLOADED)
                            {
                                logs.Add(new Log(Core.Enums.LogHelpers.LogType.DEBUG, $"Episode type updated to download", null));
                                episodeRepo.Update(item);
                                showRepo.Update(show);
                            }
                        }
                        else
                        {
                            logs.Add(new Log(Core.Enums.LogHelpers.LogType.DEBUG, $"Episode added to download queue", null));
                            episodeRepo.Create(item);
                            showRepo.Update(show);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Add(new Log(Core.Enums.LogHelpers.LogType.INFO, e.Message, e.StackTrace));
            }

            logs.Add(new Log(Core.Enums.LogHelpers.LogType.INFO, "Finished Enqueuer logic", null));

            LogRepository.Create(logs);

            return true;
        }

        private List<Episode> GetEpisodesBySource(List<Episode> episodesByShow, Show show)
        {
            var newepisodes = new List<Episode>();

            switch (show.Source)
            {
                case Source.MYASIANTV:
                    newepisodes = MATScraper.GetEpisodes(episodesByShow, show);
                    break;
                case Source.ESTRENOSDORAMAS:
                    newepisodes = ESScraper.GetEpisodes(episodesByShow, show);
                    break;
                case Source.KSHOW:
                    newepisodes = KSScraper.GetEpisodes(episodesByShow, show);
                    break;
            }

            return newepisodes;
        }

    }
}
