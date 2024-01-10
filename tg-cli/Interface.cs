using Spectre.Console;
using TdLib;

namespace tg_cli;

public struct VisibleInterface
{
    public IReadOnlyList<string> Chats { get; }
    public int SelectedIndex { get; }

    public VisibleInterface(IReadOnlyList<string> chats, int selectedIndex)
    {
        Chats = chats;
        SelectedIndex = selectedIndex;
    }
}

public class Interface
{
    private readonly IAnsiConsole _console;
    private readonly List<string> _chatTitles = new();
    private int _topChatIndex;
    private int _selectedChatIndex;

    private int ChatsToShowCount => _console.Profile.Height - 3 - 1 - 1;
    private int BottomChatIndex => _topChatIndex + ChatsToShowCount - 1;
    private int RelativeSelectedChatIndex => _selectedChatIndex - _topChatIndex;

    public event Action<VisibleInterface> RenderRequested;

    public Interface(IAnsiConsole console)
    {
        _console = console;
    }

    public async void OnClientUpdateReceived(object sender, TdApi.Update update)
    {
        var client = (TdClient) sender;
        switch (update)
        {
            case TdApi.Update.UpdateAuthorizationState updateAuthState:
                await Authorizer.OnAuthorizationStateUpdateReceived(_console, client, updateAuthState);
                break;

            case TdApi.Update.UpdateNewChat updateNewChat:
                var chatTitle = Utils.RemoveNonUtf16Characters(updateNewChat.Chat.Title);
                _chatTitles.Add(chatTitle);

                if (_chatTitles.Count - 1 <= ChatsToShowCount)
                    RequestRender();

                break;
        }
    }

    public void OnListenerCommandReceived(Command command)
    {
        switch (command)
        {
            case Command.MoveDown:
                SelectIndex(_selectedChatIndex + 1);
                break;

            case Command.MoveUp:
                SelectIndex(_selectedChatIndex - 1);
                break;
                
            case Command.MoveToTop:
                SelectIndex(0);
                break;
                
            case Command.MoveToBottom:
                SelectIndex(_chatTitles.Count - 1);
                break;
        }
    }

    private void SelectIndex(int index)
    {
        if (index < 0)
            index = 0;

        if (index > _chatTitles.Count - 1)
            index = _chatTitles.Count - 1;

        if (index == _selectedChatIndex)
            return;
            
        _selectedChatIndex = index;

        if (_selectedChatIndex < _topChatIndex)
            _topChatIndex = _selectedChatIndex;
            
        if (_selectedChatIndex > BottomChatIndex)
            _topChatIndex = _selectedChatIndex - (ChatsToShowCount - 1);

        RequestRender();
    }

    private void RequestRender()
    {
        var count = Math.Min(_chatTitles.Count, ChatsToShowCount);
        var visibleChats = _chatTitles.GetRange(_topChatIndex, count);
        var visibleInterface = new VisibleInterface(visibleChats, RelativeSelectedChatIndex);
        RenderRequested?.Invoke(visibleInterface);
    }
}