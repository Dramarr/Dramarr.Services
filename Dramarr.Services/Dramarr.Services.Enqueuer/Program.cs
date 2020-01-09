using System;
using System.ServiceProcess;

namespace Dramarr.Services.Enqueuer
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceBase.Run(new Service());
        }
    }
}
