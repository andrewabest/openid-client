using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace OpenIDClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "OpenID Client";

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                .CreateLogger();

            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(builder =>
                {
                    builder.UseUrls("https://*:8090");
                    builder.UseStartup<Startup>();
                    builder.UseSerilog();
                })
                .Build()
                .Run();
        }
    }
}
