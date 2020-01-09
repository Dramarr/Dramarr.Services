using Dramarr.Core.Download;
using Dramarr.Core.Retry;
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

        public Job(string connectionString, TimeSpan timeout, string path)
        {
            ConnectionString = connectionString;
            Timeout = timeout;
            Path = path;
        }

        public void Run() => TaskHelpers.Retry(Logic, Timeout);

        public bool Logic()
        {
            var showRepo = new ShowRepository(ConnectionString);
            var episodeRepo = new EpisodeRepository(ConnectionString);

            var showsInDatabase = showRepo.Select().Where(x => x.Download == true).ToList();
            var episodesInDatabase = episodeRepo.Select();

            foreach (var show in showsInDatabase)
            {
                var episodesByShow = episodesInDatabase.Where(x => x.ShowId == show.Id && x.Status == EpisodeStatus.SCRAPED).OrderBy(y => y.Filename).ToList();

                foreach (var episode in episodesByShow)
                {
                    var path = System.IO.Path.Combine(Path, show.Title);
                    episode.Status = DownloadHelpers.DownloadFile(episode.Url, path, episode.Filename) ? EpisodeStatus.DOWNLOADED : EpisodeStatus.FAILED;
                    episodeRepo.Update(episode);
                }
            }

            return true;
        }

    }
}
