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
        var inputListener = new InputListener(ansiConsole);
        var renderer = new Renderer(ansiConsole);
        var @interface = new Interface(ansiConsole);
        client.UpdateReceived += @interface.OnClientUpdateReceived;
        inputListener.CommandReceived += @interface.OnListenerCommandReceived;
        @interface.RenderRequested += renderer.OnRenderRequested;
        
        await client.Start();
        await inputListener.StartListen();
    }
}