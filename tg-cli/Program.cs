using System.Text;
using SettingsManagement;
using Spectre.Console;

namespace tg_cli;

public static class Program
{
    private static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        var ansiConsole = AnsiConsole.Console;
        var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var tgCliFolder = Path.Combine(appDataFolder, "tg_cli");
        Directory.CreateDirectory(tgCliFolder);
        var settingsManager = new SettingsManager<TgCliSettings>(tgCliFolder);
        var settings = settingsManager.LoadSettings();

        var client = new Client(tgCliFolder, settings);
        var renderer = new Renderer(ansiConsole, client);
        
        await client.Start();
        await renderer.StartInputCycle();
    }
}