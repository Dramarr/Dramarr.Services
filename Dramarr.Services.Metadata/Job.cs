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

namespace Dramarr.Services.Metadata
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
            var logs = new List<Log>();

            try
            {
                logs.Add(new Log(Core.Enums.LogHelpers.LogType.INFO, "Starting Metadata logic", null));

                var showRepo = new ShowRepository(ConnectionString);
                var metadataRepo = new MetadataRepository(ConnectionString);

                logs.Add(new Log(Core.Enums.LogHelpers.LogType.DEBUG, "Getting shows in database", null));
                var showsInDatabase = showRepo.Select();
                logs.Add(new Log(Core.Enums.LogHelpers.LogType.DEBUG, $"Found {showsInDatabase.Count} shows", null));

                logs.Add(new Log(Core.Enums.LogHelpers.LogType.DEBUG, "Getting metadatas in database", null));
                var metadataInDatabase = metadataRepo.Select();
                logs.Add(new Log(Core.Enums.LogHelpers.LogType.DEBUG, $"Found {metadataInDatabase.Count} shows", null));
            
                logs.Add(new Log(Core.Enums.LogHelpers.LogType.INFO, $"Getting shows that does not have metadata", null));

                //2433

                var showsWithoutMetadata = (from shows in showsInDatabase
                                            join metadata in metadataInDatabase on shows.Id equals metadata.ShowId into md
                                            from metadata in md.DefaultIfEmpty()
                                            select new { shows }).ToList();

                logs.Add(new Log(Core.Enums.LogHelpers.LogType.INFO, $"Found {showsWithoutMetadata.Count} shows without metadata", null));

                foreach (var show in showsWithoutMetadata)
                {
                    logs.Add(new Log(Core.Enums.LogHelpers.LogType.INFO, $"Looking for {show.shows.Title} metadata", null));
                    var metadata = GetMetadata(show.shows.Source, show.shows.Url);
                    var newMetadata = new Data.Model.Metadata(show.shows.Id, metadata.ImageUrl, metadata.Plot, metadata.Cast, metadata.Language);
                    metadataRepo.Create(newMetadata);
                    logs.Add(new Log(Core.Enums.LogHelpers.LogType.INFO, $"Metadata added successfully", null));
                }
            }
            catch (Exception e)
            {
                logs.Add(new Log(Core.Enums.LogHelpers.LogType.INFO, e.Message, e.StackTrace));
            }

            logs.Add(new Log(Core.Enums.LogHelpers.LogType.INFO, "Finished Metadata logic", null));

            LogRepository.Create(logs);

            return true;
        }

        private Data.Model.Metadata GetMetadata(Source source, string urlTitle)
        {
            return source switch
            {
                Source.MYASIANTV => MATScraper.GetMetadata(urlTitle),
                Source.ESTRENOSDORAMAS => ESScraper.GetMetadata(urlTitle),
                Source.KSHOW => KSScraper.GetMetadata(urlTitle),
                _ => null,
            };
        }

    }
}
