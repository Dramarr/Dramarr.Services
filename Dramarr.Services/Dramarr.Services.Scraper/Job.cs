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

namespace Dramarr.Services.Scraper
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

            Run();
        }

        public void Run() => TaskHelpers.Retry(Logic, Timeout);

        public bool Logic()
        {
            try
            {
                LogRepository.Create(new Log(Core.Enums.LogHelpers.LogType.INFO, "Starting Scraper logic", null));

                var showRepo = new ShowRepository(ConnectionString);

                LogRepository.Create(new Log(Core.Enums.LogHelpers.LogType.DEBUG, "Getting shows in database", null));
                var showsInDatabase = showRepo.Select();
                LogRepository.Create(new Log(Core.Enums.LogHelpers.LogType.DEBUG, $"Found {showsInDatabase.Count} shows", null));

                var allShows = new List<Show>();

                LogRepository.Create(new Log(Core.Enums.LogHelpers.LogType.INFO, $"Getting shows from sources", null));

                GetAllShows(Source.MYASIANTV)?.ForEach(x => allShows.Add(new Show(x)));
                GetAllShows(Source.ESTRENOSDORAMAS)?.ForEach(x => allShows.Add(new Show(x)));
                GetAllShows(Source.KSHOW)?.ForEach(x => allShows.Add(new Show(x)));

                LogRepository.Create(new Log(Core.Enums.LogHelpers.LogType.DEBUG, $"Found {allShows.Count} counts from sources", null));

                var finalList = allShows.Where(x => !showsInDatabase.Exists(y => x.Url == y.Url)).ToList();
                var distinctAux = finalList
                    .GroupBy(x => x.Url)
                    .Select(x => x.First()).ToList();

                LogRepository.Create(new Log(Core.Enums.LogHelpers.LogType.INFO, $"{distinctAux.Count} shows will be added", null));

                showRepo.BulkCreate(finalList);

                LogRepository.Create(new Log(Core.Enums.LogHelpers.LogType.INFO, $"Shows added successfully", null));
            }
            catch (Exception e)
            {
                LogRepository.Create(new Log(Core.Enums.LogHelpers.LogType.INFO, e.Message, e.StackTrace));
            }

            LogRepository.Create(new Log(Core.Enums.LogHelpers.LogType.INFO, "Finished Scraper logic", null));

            return true;
        }

        private List<string> GetAllShows(Source source)
        {
            return source switch
            {
                Source.MYASIANTV => MATScraper.GetLatestShows(),
                Source.ESTRENOSDORAMAS => ESScraper.GetLatestShows(),
                Source.KSHOW => KSScraper.GetLatestShows(),
                _ => null,
            };
        }

    }
}
