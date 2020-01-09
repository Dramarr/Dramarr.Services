using Dramarr.Core.Retry;
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
        private Scrapers.Kshow.Manager KSScraper;

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
        }

        public void Run() => TaskHelpers.Retry(Logic, Timeout);

        public bool Logic()
        {
            var showRepo = new ShowRepository(ConnectionString);
            var episodeRepo = new EpisodeRepository(ConnectionString);

            var showsInDatabase = showRepo.Select().Where(x => x.Enabled == true).ToList();
            var episodesInDatabase = episodeRepo.Select();

            foreach (var show in showsInDatabase)
            {
                // Get total episodes and status
                var status = GetStatus(show.Source, show.Url);

                // Check if current amount of episodes == total episodes and status is downloaded
                var episodesByShow = episodesInDatabase.Where(x => x.ShowId == show.Id && x.Status == EpisodeStatus.DOWNLOADED).ToList();

                // if yes then disable drama -> Enabled = false
                if (episodesByShow.Count == status.Item1 && status.Item2)
                {
                    show.Enabled = false;
                    showRepo.Update(show);
                }
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
