using System;
using System.Linq;
using System.Windows;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;

namespace AudioFileInspector;

/// <summary>
/// アプリケーションのエントリポイント。
/// </summary>
public partial class App : Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        var catalog = new AssemblyCatalog(Assembly.GetExecutingAssembly());
        var container = new CompositionContainer(catalog);
        var inspectors = container.GetExportedValues<IAudioFileInspector>().ToList();
        var args = e.Args;
        if (args.Length > 0)
        {
            if (args[0] == "-install")
            {
                try
                {
                    OptionsWindow.Associate(inspectors);
                    Console.WriteLine("Created {0} file associations", inspectors.Count);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unable to create file associations");
                    Console.WriteLine(ex);
                    Environment.ExitCode = -1;
                    Shutdown();
                    return;
                }
                Shutdown();
                return;
            }
            if (args[0] == "-uninstall")
            {
                try
                {
                    OptionsWindow.Disassociate(inspectors);
                    Console.WriteLine("Removed {0} file associations", inspectors.Count);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unable to remove file associations");
                    Console.WriteLine(ex);
                    Environment.ExitCode = -1;
                    Shutdown();
                    return;
                }
                Shutdown();
                return;
            }
        }
        var mainWindow = container.GetExportedValue<MainWindow>();
        mainWindow.CommandLineArguments = args;
        mainWindow.Show();
    }
}
