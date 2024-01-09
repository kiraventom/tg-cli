using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using SettingsManagement;
using Spectre.Console;
using TdLib;

namespace tg_cli;

#pragma warning disable CS0169

public partial class TgCliSettings : ISettings
{
}

#pragma warning restore CS0169

public class Renderer
{
    private readonly List<string> _chatTitles = new();

    private readonly Style _selectedChatStyle = new(Color.Black, Color.White);
    private int _selectedChatIndex;

    private readonly IAnsiConsole _console;

    private int ChatsToShowCount => _console.Profile.Height - 3 - 1 - 1;

    public Renderer(IAnsiConsole console, Client client)
    {
        _console = console;
        client.UpdateReceived += OnClientUpdateReceived;
    }

    public async Task StartInputCycle()
    {
        while (true)
        {
            var cki = await _console.Input.ReadKeyAsync(true, CancellationToken.None);
            switch (cki!.Value.Key)
            {
                case ConsoleKey.Q:
                    return;

                case ConsoleKey.J:
                    if (_selectedChatIndex == _chatTitles.Count - 1)
                        break;

                    ++_selectedChatIndex;
                    RenderChats();
                    break;

                case ConsoleKey.K:
                    if (_selectedChatIndex == 0)
                        break;

                    --_selectedChatIndex;
                    RenderChats();
                    break;
            }
        }
    }

    private void RenderChats()
    {
        _console.Clear();

        const double chatListWidthMod = 0.2;
        var chatWidthMod = 1 - chatListWidthMod;
        var consoleWidthWithoutBorders = _console.Profile.Width - 1 - 1 - 1;
        var chatListWidth = (int)Math.Round(consoleWidthWithoutBorders * chatListWidthMod);
        var chatWidth = (int)Math.Round(consoleWidthWithoutBorders * chatWidthMod);
        // Make up for rounding
        chatListWidth += consoleWidthWithoutBorders - (chatListWidth + chatWidth);
        
        const char ellipsis = '\u2026';

        var chatsToShow = _chatTitles.Take(ChatsToShowCount);
        var chats = chatsToShow.Select((title, i) =>
        {
            if (title.Length > chatListWidth)
                title = title[..(chatListWidth - 1)] + ellipsis;

            return new Markup(title.EscapeMarkup(), i == _selectedChatIndex ? _selectedChatStyle : null);
        });

        var chatList = new Rows(chats);
        var chatPanel = new Panel("Messages here")
        {
            Expand = true
        };

        var table = new Table() {Border = TableBorder.Rounded};
        table.AddColumn("Chats list");
        table.Columns[0].Width = chatListWidth;
        table.Columns[0].Alignment = Justify.Left;

        var selectedChatTitle = _chatTitles[_selectedChatIndex];
        table.AddColumn(selectedChatTitle.EscapeMarkup());
        
        table.Columns[1].Width = chatWidth;
        table.AddRow(chatList, chatPanel);

        _console.Write(table);
    }

    private async void OnClientUpdateReceived(object sender, TdApi.Update update)
    {
        var client = (TdClient) sender;
        switch (update)
        {
            case TdApi.Update.UpdateNewChat updateNewChat:
                var chatTitle = updateNewChat.Chat.Title;
                // HACK to fix Spectre bug that causes strings with surrogate pairs and zero-widths break aligning
                var zeroWidths = new[] {"\u200c", "\u200d", "\u200b", "\u2060"};
                chatTitle = string.Join(string.Empty, chatTitle.Where(c => !char.IsSurrogate(c)));
                chatTitle = zeroWidths.Aggregate(chatTitle,
                    (current, zeroWidth) => current.Replace(zeroWidth, string.Empty));

                _chatTitles.Add(chatTitle);
                
                if (_chatTitles.Count - 1 <= ChatsToShowCount)
                    RenderChats();
                    
                break;

            case TdApi.Update.UpdateAuthorizationState updateAuthState:
                switch (updateAuthState.AuthorizationState)
                {
                    case TdApi.AuthorizationState.AuthorizationStateWaitPhoneNumber:
                        var phoneNumber = _console.Ask<string>("Enter phone number in international format: ",
                            n => n.StartsWith('+') && n[1..].All(char.IsDigit));
                        await client.SetAuthenticationPhoneNumberAsync(phoneNumber);
                        break;

                    case TdApi.AuthorizationState.AuthorizationStateWaitCode:
                        var code = _console.Ask<string>("Enter code: ",
                            c => c.All(char.IsDigit));
                        await client.CheckAuthenticationCodeAsync(code);
                        break;

                    case TdApi.AuthorizationState.AuthorizationStateWaitPassword:
                        var password = _console.Ask<string>("Enter password: ", p => !string.IsNullOrEmpty(p));
                        await client.CheckAuthenticationPasswordAsync(password);
                        break;
                }

                break;
        }
    }
}

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

    private async void ClientOnUpdateReceived(object sender, TdApi.Update update)
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