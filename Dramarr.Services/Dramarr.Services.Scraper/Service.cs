using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;

namespace Dramarr.Services.Scraper
{
    public class Service : ServiceBase
    {
        protected override void OnStart(string[] args)
        {
            base.OnStart(args);
        }

        protected override void OnStop()
        {
            base.OnStop();
        }

        protected override void OnPause()
        {
            base.OnPause();
        }
    }
}
