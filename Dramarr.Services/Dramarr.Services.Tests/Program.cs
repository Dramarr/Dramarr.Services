using System;

namespace Dramarr.Services.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            var cs = "Data Source=.; Initial Catalog=Dramarr; User id=sa; Password=sa;";
            var path = @"D:\Downloads\Dramarr";

            var scrapper = new Scraper.Job(cs, TimeSpan.FromMinutes(5), path);
            scrapper.Run();
        }
    }
}
