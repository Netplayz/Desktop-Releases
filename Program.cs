using Avalonia;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.ReactiveUI;
using Avalonia.Svg.Skia;
using DiscordRPC;
using ERM_Desktop.Services;
using Sentry;

namespace ERM_Desktop;

public static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        using(Mutex mutex = new Mutex(false, "ERM Desktop"))
        {
            if(!mutex.WaitOne(0, false))
            {
                Environment.Exit(1);
                return;
            }

            try
            {
                if(!Directory.Exists(Storage.FolderPath))
                {
                    Directory.CreateDirectory(Storage.FolderPath);
                }
                
                if(File.Exists(Path.Combine(Storage.FolderPath, "erm.auth")))
                {
                    string key = File.ReadAllText(Path.Combine(Storage.FolderPath, "erm.auth"));
                    
                    if(!string.IsNullOrWhiteSpace(key))
                    {
                        Storage.Identifier = key;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
            SentrySdk.Init(o =>
            {
                o.Dsn = "https://42b71650a2c1ae3efdf50b0883cf359f@o4510448999792640.ingest.us.sentry.io/4510449003003904";
                o.Debug = false;
                o.EnableTracing = true;
                o.IsGlobalModeEnabled = true;
            });

            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
        
            if(args.Length > 0)
            {
                if(args[0].ToUpper() == "EXPERIMENTAL")
                {
                    Storage.ExperimentalMode = true;
                }
            }

           
            Task.Run(() =>
            {
                try
                {
                    Storage.DiscordRpcClient.Initialize();
            
                    Storage.DiscordRpcClient.SetPresence(new RichPresence()
                    {
                        Details = "Logging in...",
                        Timestamps = new Timestamps(Storage.InitialLoadTime),
                        Assets = new Assets()
                        {
                            LargeImageKey = "erm",
                            LargeImageText = "ERM Desktop"
                        },
                        Buttons = new Button[]
                        {
                            new()
                            {
                                Label = "ERM CE Website",
                                Url = "ermce.hueymcspewy.online/"
                            }
                    }   
                    });
                
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);

            Task.Run(() =>
            {
                Storage.DiscordRpcClient.Dispose();
            });
        }
    }
    
    private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Exception ex = e.ExceptionObject as Exception ?? new Exception("Unknown exception");

        SentrySdk.CaptureException(ex);
        
        throw ex;
    }
    
    public static AppBuilder BuildAvaloniaApp()
    {
        GC.KeepAlive(typeof(SvgImageExtension).Assembly);
        GC.KeepAlive(typeof(Avalonia.Svg.Skia.Svg).Assembly);
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();
    }

}
