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

        public Job(string connectionString, TimeSpan timeout, string path)
        {
            ConnectionString = connectionString;
            Timeout = timeout;

            // todo
            MATScraper = new Scrapers.MyAsianTv.Manager();
            ESScraper = new Scrapers.EstrenosDoramas.Manager();
            KSScraper = new Scrapers.Kshow.Manager();
        }

        public void Run() => TaskHelpers.Retry(Logic, Timeout);

        public bool Logic()
        {
            var showRepo = new ShowRepository(ConnectionString);
            var showsInDatabase = showRepo.Select();
            var allShows = new List<Show>();

            GetAllShows(Source.MYASIANTV)?.ForEach(x => allShows.Add(new Show(x)));
            GetAllShows(Source.ESTRENOSDORAMAS)?.ForEach(x => allShows.Add(new Show(x)));
            GetAllShows(Source.KSHOW)?.ForEach(x => allShows.Add(new Show(x)));

            var finalList = allShows.Where(x => showsInDatabase.Exists(y => x.Url == y.Url)).ToList();
            showRepo.BulkCreate(finalList);

            return true;
        }

        private List<string> GetAllShows(Source source)
        {
            return source switch
            {
                Source.MYASIANTV => MATScraper.GetAllShows(),
                Source.ESTRENOSDORAMAS => ESScraper.GetAllShows(),
                Source.KSHOW => KSScraper.GetAllShows(),
                _ => null,
            };
        }

    }
}
