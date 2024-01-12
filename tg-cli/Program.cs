using System.Text;
using SettingsManagement;
using Spectre.Console;
using Serilog;
using Serilog.Core;

namespace tg_cli;

public static class Program
{
    internal static Logger Logger { get; private set; }
    
    private static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        var ansiConsole = AnsiConsole.Console;
        ansiConsole.Cursor.Hide();
        var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var tgCliFolder = Path.Combine(appDataFolder, "tg_cli");
        Directory.CreateDirectory(tgCliFolder);
        var settingsManager = new SettingsManager<TgCliSettings>(tgCliFolder);
        var settings = settingsManager.LoadSettings();

        var databaseDirPath = Path.Combine(tgCliFolder, "database");
        var filesDirPath = Path.Combine(tgCliFolder, "files");
        var logsDirPath = Path.Combine(tgCliFolder, "logs");
        var tdLibLogsPath = Path.Combine(logsDirPath, "tdlib");
        
        var appLogFilePath = Path.Combine(logsDirPath, "app.log");
        Logger = new LoggerConfiguration()
            .WriteTo.File(appLogFilePath)
            .CreateLogger();
    
        var client = new Client(settings);
        var authorizer = new Authorizer(ansiConsole);
        var inputListener = new InputListener(ansiConsole);
        var renderer = new Renderer(ansiConsole, settings);
        var model = new Model(renderer, settings);
        client.UpdateReceived += authorizer.OnClientUpdateReceived;
        client.UpdateReceived += model.OnClientUpdateReceived;
        inputListener.CommandReceived += model.OnListenerCommandReceived;
        inputListener.InputReceived += model.OnListenerInputReceived;
        model.RenderRequested += renderer.OnRenderRequested;
        
        await client.Start(databaseDirPath, filesDirPath, tdLibLogsPath);
        await inputListener.StartListen();
    }
}