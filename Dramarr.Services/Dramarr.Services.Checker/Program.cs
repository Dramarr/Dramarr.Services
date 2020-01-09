using System;
using System.ServiceProcess;

namespace Dramarr.Services.Checker
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceBase.Run(new Service());
        }
    }
}
