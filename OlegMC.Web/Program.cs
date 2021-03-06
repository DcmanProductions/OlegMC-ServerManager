using com.drewchaseproject.net.asp.mc.OlegMC.Library.Data;
using com.drewchaseproject.net.asp.mc.OlegMC.Library.Data.Streams;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;

namespace com.drewchaseproject.net.asp.mc.OlegMC.Web
{
    /// <summary>
    /// Starting Class
    /// <br/>
    /// Initializes the IIS/Kestral Server
    /// </summary>
    public class Program
    {
        private static readonly ChaseLabs.CLLogger.Interfaces.ILog log = ChaseLabs.CLLogger.LogManger.Init().SetLogDirectory(Values.Singleton.LogFile);

        public static void Main(string[] args)
        {
            try
            {
                CreateHostBuilder(args).Build().Run();
            }
            catch
            {
                CreateAltHostBuilder(args).Build().Run();
            }

            In.ImportPreExistingServers();

            //AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            //{
            //    NetworkUtilities.Singleton.ClosePorts();
            //    System.Threading.Thread.Sleep(3 * 1000);
            //};
        }

        /// <summary>
        /// Alternative Host Builder, Usually means executed in IDE or via IIS
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IHostBuilder CreateAltHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseContentRoot(Directory.GetCurrentDirectory());
                webBuilder.UseIISIntegration();
                webBuilder.UseStartup<Startup>();
            });
        }

        /// <summary>
        /// Default Host Builder using Kestral
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>().UseKestrel(options =>
                {
                    int port = Configuration.Port;
                    string filename = "", password = "";
                    bool useHttps = false;
                    foreach (string argument in args)
                    {
                        string text = argument.ToLower();
                        if (text.StartsWith("help") || text.StartsWith("h"))
                        {
                            log.Info(new string[]{
                                "-----HELP-----",
                                "-h | --help: For this Menu",
                                "-p={port} | --port={port}: To set the Starting Port (Ex: --port=1234)",
                                "--usehttps: To Enable Https (REQUIRES: filename, password)",
                                "--filename={path/to/Certificate/File}: Cert File for HTTPS",
                                "--password={password}: Password for the HTTPS Certificate",
                                "-----END HELP-----"});
                            Console.ReadLine();
                            Environment.Exit(0);
                        }
                        else if (text.StartsWith("port=") || text.StartsWith("p="))
                        {
                            if (int.TryParse(text.Replace("port=", "").Replace("p=", ""), out port))
                            {
                                Configuration.Port = port;
                            }
                            continue;
                        }
                        else if (text.StartsWith("usehttps"))
                        {
                            useHttps = true;
                        }
                        else if (text.StartsWith("filename="))
                        {
                            filename = text.Replace("filename=", "");
                        }
                        else if (text.StartsWith("password="))
                        {
                            password = text.Replace("password=", "");
                        }
                    }

                    if (useHttps)
                    {
                        if (string.IsNullOrWhiteSpace(filename))
                        {
                            log.Error("Filename cannot be blank in --filename when using HTTPS", "type --help or -h for more information");
                            Environment.Exit(0);
                        }
                        if (string.IsNullOrWhiteSpace(password))
                        {
                            log.Error("Password cannot be blank in --password when using HTTPS", "type --help or -h for more information");
                            Environment.Exit(0);
                        }
                        options.ListenAnyIP(port, listenOptions =>
                    {
                        listenOptions.UseHttps(filename, password);
                    });
                    }
                    else
                    {
                        options.ListenAnyIP(port);
                    }
                    //NetworkUtilities.Singleton.PortForward(port);
                });
            });
        }
    }
}
