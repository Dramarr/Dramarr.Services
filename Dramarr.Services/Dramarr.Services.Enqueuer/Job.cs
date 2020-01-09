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

namespace Dramarr.Services.Enqueuer
{
    public class Job
    {
        public string ConnectionString { get; set; }
        public TimeSpan Timeout { get; set; }
        public string Path { get; set; }

        private Scrapers.MyAsianTv.Manager MATScraper;
        private Scrapers.EstrenosDoramas.Manager ESScraper;
        private Scrapers.Kshow.Manager KSScraper;

        public Job(string connectionString, TimeSpan timeout, string path)
        {
            ConnectionString = connectionString;
            Timeout = timeout;
            Path = path;

            // todo
            MATScraper = new Scrapers.MyAsianTv.Manager();
            ESScraper = new Scrapers.EstrenosDoramas.Manager();
            KSScraper = new Scrapers.Kshow.Manager();
        }

        public void Run() => TaskHelpers.Retry(Logic, Timeout);

        public bool Logic()
        {
            var showRepo = new ShowRepository(ConnectionString);
            var episodeRepo = new EpisodeRepository(ConnectionString);
            var showsInDatabase = showRepo.Select().Where(x => x.Download ?? true).ToList();

            foreach (var show in showsInDatabase)
            {
                var episodesInDatabase = episodeRepo.Select().Where(x => x.ShowId == show.Id).ToList();
                var episodesByShow = episodesInDatabase.Where(x => x.Status == EpisodeStatus.UNKNOWN || x.Status == EpisodeStatus.FAILED && x.Status != EpisodeStatus.DOWNLOADED).ToList();
                var newepisodes = GetEpisodesBySource(episodesByShow, show);

                foreach (var item in newepisodes)
                {
                    var ep = episodesByShow.SingleOrDefault(x => x.Filename == item.Filename);
                    var existsAsScraperOrDownloaded = episodesInDatabase.SingleOrDefault(x => x.Filename == item.Filename && (x.Status == EpisodeStatus.SCRAPED || x.Status == EpisodeStatus.DOWNLOADED));

                    if (existsAsScraperOrDownloaded != null)
                    {
                        continue;
                    }

                    if (ep != null)
                    {
                        if (item.Status != ep.Status && ep.Status != EpisodeStatus.DOWNLOADED)
                        {
                            episodeRepo.Update(item);
                            showRepo.Update(show);
                        }
                    }
                    else
                    {
                        episodeRepo.Create(item);
                        showRepo.Update(show);
                    }
                }
            }

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
