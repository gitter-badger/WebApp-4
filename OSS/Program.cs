﻿using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Aiursoft.Pylon;
using Aiursoft.OSS.Data;

namespace Aiursoft.OSS
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args)
                .MigrateDbContext<OSSDbContext>()
                .Run();
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            var host = WebHost.CreateDefaultBuilder(args)
                 .UseStartup<Startup>()
                 .Build();

            return host;
        }
    }
}
