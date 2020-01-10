using System;

namespace Dramarr.Services.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            //EnqueuerTest();
            //ScraperTest();
            //DownloaderTest();
            CheckerTest();
        }

        public static void ScraperTest()
        {
            var cs = "Data Source=.; Initial Catalog=Dramarr; User id=sa; Password=sa;";

            var scrapper = new Scraper.Job(cs, TimeSpan.FromMinutes(5));
            scrapper.Run();
        }

        public static void EnqueuerTest()
        {
            var cs = "Data Source=.; Initial Catalog=Dramarr; User id=sa; Password=sa;";

            var enqueuer = new Enqueuer.Job(cs, TimeSpan.FromMinutes(10));
            enqueuer.Run();
        }


        public static void DownloaderTest()
        {
            var cs = "Data Source=.; Initial Catalog=Dramarr; User id=sa; Password=sa;";
            var path = @"D:\Downloads\Dramarr";

            var enqueuer = new Downloader.Job(cs, TimeSpan.FromMinutes(10), path);
            enqueuer.Run();
        }

        public static void CheckerTest()
        {
            var cs = "Data Source=.; Initial Catalog=Dramarr; User id=sa; Password=sa;";

            var enqueuer = new Checker.Job(cs, TimeSpan.FromMinutes(10));
            enqueuer.Run();
        }
    }
}
