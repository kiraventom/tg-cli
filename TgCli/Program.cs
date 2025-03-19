using System.Text;
using SettingsManagement;
using Spectre.Console;
using Serilog;

namespace TgCli;

public static class Program
{
    private static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        var ansiConsole = AnsiConsole.Console;
        ansiConsole.Cursor.Hide();
        var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var tgCliFolder = Path.Combine(appDataFolder, "TgCli");
        Directory.CreateDirectory(tgCliFolder);
        var settingsManager = new SettingsManager<TgCliSettings>(tgCliFolder);
        var settings = settingsManager.LoadSettings();

        var databaseDirPath = Path.Combine(tgCliFolder, "database");
        var filesDirPath = Path.Combine(tgCliFolder, "files");
        var logsDirPath = Path.Combine(tgCliFolder, "logs");
        var tdLibLogsPath = Path.Combine(logsDirPath, "tdlib");

        var appLogFilePath = Path.Combine(logsDirPath, "app.log");
        var logger = new LoggerConfiguration()
            .WriteTo.File(appLogFilePath)
            .CreateLogger();

        logger.Information("=== TgCli launched ===");

        var model = new Model(logger);
        var client = new Client(logger);
        var authorizer = new Authorizer(ansiConsole, logger);

        var commands = new InputCommand[]
        {
            new QuitCommand(model),
            new MoveDownCommand(model),
            new MoveUpCommand(model),
            new MoveToTopCommand(model),
            new MoveToBottomCommand(model),
            new NextFolderCommand(model),
            new PreviousFolderCommand(model),
            new SelectFolderCommand(model),
            new LoadChatsCommand(model),
            new LoadMessagesCommand(model),
        };

        var inputListener = new InputListener(ansiConsole, logger);
        var renderer = new Renderer(ansiConsole, logger, settings);
        var viewModel = new MainViewModel(logger, renderer, client, settings, model);

        client.UpdateReceived += authorizer.OnClientUpdateReceived;
        client.UpdateReceived += viewModel.OnClientUpdateReceived;

        inputListener.CommandReceived += viewModel.OnListenerCommandReceived;
        inputListener.InputUpdated += viewModel.OnListenerInputUpdated;

        viewModel.RenderRequested += renderer.OnRenderRequested;

        await client.Start(databaseDirPath, filesDirPath, tdLibLogsPath);
        await inputListener.StartListen();

        ansiConsole.Cursor.Show();
        ansiConsole.Clear();

        logger.Information("=== TgCli exited ===");
    }
}