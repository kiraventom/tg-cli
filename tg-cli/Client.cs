using System.Runtime.InteropServices;
using System.Text;
using TdLib;

namespace tg_cli;

public class Client
{
    private readonly TgCliSettings _settings;
    private readonly CancellationTokenSource _waitingForReadyCts = new();

    public event EventHandler<TdApi.Update> UpdateReceived;

    public Client(TgCliSettings settings)
    {
        _settings = settings;
    }

    public async Task Start(string databaseDirPath, string filesDirPath, string tdLibLogsDirPath)
    {
        Directory.CreateDirectory(databaseDirPath);
        Directory.CreateDirectory(filesDirPath);
        Directory.CreateDirectory(tdLibLogsDirPath);

        var client = new TdClient();
        var pathToLogFile = Path.Combine(tdLibLogsDirPath, "tdlib.log");
        InitLogging(client, pathToLogFile);

        client.UpdateReceived += ClientOnUpdateReceived;

        await client.SetTdlibParametersAsync(false, databaseDirPath, filesDirPath, null, false, false, false,
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