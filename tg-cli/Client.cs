using System.Runtime.InteropServices;
using System.Text;
using SettingsManagement;
using TdLib;

namespace tg_cli;

#pragma warning disable CS0169

public partial class TgCliSettings : ISettings
{
}

#pragma warning restore CS0169

public class Client
{
    private readonly string _tgCliFolder;
    private readonly TgCliSettings _settings;
    private readonly CancellationTokenSource _waitingForReadyCts = new();

    public event EventHandler<TdApi.Update> UpdateReceived;

    public Client(string tgCliFolder, TgCliSettings settings)
    {
        _tgCliFolder = tgCliFolder;
        _settings = settings;
    }

    public async Task Start()
    {
        var databaseDirectory = Path.Combine(_tgCliFolder, "database");
        var filesDirectory = Path.Combine(_tgCliFolder, "files");
        var logsDirectory = Path.Combine(_tgCliFolder, "logs");
        Directory.CreateDirectory(databaseDirectory);
        Directory.CreateDirectory(filesDirectory);
        Directory.CreateDirectory(logsDirectory);

        var client = new TdClient();
        var pathToLogFile = Path.Combine(logsDirectory, "tdlib.log");
        InitLogging(client, pathToLogFile);

        client.UpdateReceived += ClientOnUpdateReceived;

        await client.SetTdlibParametersAsync(false, databaseDirectory, filesDirectory, null, false, false, false,
            false, 20623965, "6c3f5f166e8fd2b613e88395e32b42dd", "ru-RU", "Windows", "10", "1.0");

        try
        {
            await Task.Delay(-1, _waitingForReadyCts.Token);
        }
        catch (TaskCanceledException)
        {
        }

        await client.LoadChatsAsync(null, 5);
    }

    private void ClientOnUpdateReceived(object sender, TdApi.Update update)
    {
        var client = (TdClient) sender;
        switch (update)
        {
            case TdApi.Update.UpdateAuthorizationState updateAuthState:
                switch (updateAuthState.AuthorizationState)
                {
                    case TdApi.AuthorizationState.AuthorizationStateReady:
                        _waitingForReadyCts.Cancel();
                        break;

                    case TdApi.AuthorizationState.AuthorizationStateClosed:
                        client.Dispose();
                        break;
                }

                break;
        }

        UpdateReceived?.Invoke(client, update);
    }

    private static void InitLogging(TdClient client, string pathToLogFile)
    {
        client.Bindings.SetLogVerbosityLevel(5);
        client.Bindings.SetLogFileMaxSize(8_000_000);

        var ptr = StringToIntPtr(pathToLogFile);
        client.Bindings.SetLogFilePath(ptr);
        Marshal.FreeHGlobal(ptr);
    }

    private static IntPtr StringToIntPtr(string str)
    {
        var numArray = new byte[Encoding.UTF8.GetByteCount(str) + 1];
        Encoding.UTF8.GetBytes(str, 0, str.Length, numArray, 0);
        var ptr = Marshal.AllocHGlobal(numArray.Length);
        Marshal.Copy(numArray, 0, ptr, numArray.Length);
        return ptr;
    }
}