using TdLib;
using tg_cli.Extensions;
using tg_cli.Handlers;
using tg_cli.ViewModels;

namespace tg_cli;

public class MainViewModel
{
    private readonly HandlersCollection<TdApi.Update> _updateHandlers = new();
    private readonly HandlersCollection<Command> _commandHandlers = new();

    private readonly IRenderer _renderer;
    private readonly IClient _client;
    private readonly TgCliSettings _settings;

    private string _commandInput = string.Empty;

    private readonly Model _model;

    private int[] TopChatIndexes { get; set; }

    private int TopChatIndex => TopChatIndexes is null ? 0 : TopChatIndexes[_model.SelectedFolderIndex];
    private int RelativeSelectedChatIndex => _model.SelectedFolder.SelectedChatIndex - TopChatIndex;
    private int BottomChatIndex => TopChatIndex + VisibleChatsCount - 1;
    private int VisibleChatsCount => _renderer.MaxVisibleChatsCount;
    private int VisibleMessagesCount => _renderer.MaxVisibleMessagesCount;

    public event Action<VisibleInterface> RenderRequested;

    public MainViewModel(IRenderer renderer, IClient client, TgCliSettings settings, Model model)
    {
        _renderer = renderer;
        _client = client;
        _settings = settings;
        _model = model;

        RegisterUpdateHandlers();
        RegisterCommandHandlers();
    }

    public async void OnClientUpdateReceived(object sender, TdApi.Update update)
    {
        Program.Logger.LogUpdate(update, _model.AllChatsFolder.ChatsDict);

        if (update is TdApi.Update.UpdateChatFolders updateChatFolders)
            TopChatIndexes = new int[updateChatFolders.ChatFolders.Length + 1];

        var requestRender = await _updateHandlers.HandleAsync(update);

        if (requestRender)
            RequestRender();
    }

    public async void OnListenerCommandReceived(Command command)
    {
        await _commandHandlers.HandleAsync(command);

        if (_model.SelectedFolder.SelectedChatIndex < TopChatIndex)
            TopChatIndexes[_model.SelectedFolderIndex] = _model.SelectedFolder.SelectedChatIndex;

        if (_model.SelectedFolder.SelectedChatIndex > BottomChatIndex)
            TopChatIndexes[_model.SelectedFolderIndex] =
                _model.SelectedFolder.SelectedChatIndex - (VisibleChatsCount - 1);

        RequestRender();
    }

    public void OnListenerInputReceived(string input)
    {
        _commandInput = input;
        RequestRender();
    }
    
    public bool IsChatVisible(Chat chat)
    {
        if (!_model.SelectedFolder.ChatsDict.ContainsKey(chat.Id))
            return false;
            
        var index = _model.SelectedFolder.SortedChats.IndexOf(chat);
        return index >= TopChatIndex && index <= BottomChatIndex;
    }

    private void RequestRender()
    {
        var chatsCount = Math.Min(_model.SelectedFolder.SortedChats.Count, VisibleChatsCount);
        IReadOnlyList<Chat> visibleChats = chatsCount > 0
            ? _model.SelectedFolder.SortedChats.GetRange(TopChatIndex, chatsCount)
            : Array.Empty<Chat>();

        var messagesCount = Math.Min(_model.SelectedFolder?.SelectedChat?.Messages?.Count ?? 0, VisibleMessagesCount);
        IReadOnlyList<Message> visibleMessages = messagesCount > 0
            ? _model.SelectedFolder.SelectedChat.Messages.GetRange(
                _model.SelectedFolder.SelectedChat.Messages.Count - messagesCount, messagesCount)
            : Array.Empty<Message>();

        var visibleInterface = new VisibleInterface(_model.Folders, visibleChats, visibleMessages,
            RelativeSelectedChatIndex, _commandInput, _model.SelectedFolderIndex, _model.Users);

        RenderRequested?.Invoke(visibleInterface);
    }

    private void RegisterUpdateHandlers()
    {
        _updateHandlers.Register(new UpdateNewChatHandler(this, _model));
        _updateHandlers.Register(new UpdateChatPositionHandler(this, _model));
        _updateHandlers.Register(new UpdateChatLastMessageHandler(this, _model));
        _updateHandlers.Register(new UpdateChatReadInboxHandler(this, _model));
        _updateHandlers.Register(new UpdateScopeNotificationSettingsHandler(this, _model));
        _updateHandlers.Register(new UpdateChatNotificationSettingsHandler(this, _model));
        _updateHandlers.Register(new UpdateChatFoldersHandler(this, _model));
        _updateHandlers.Register(new UpdateChatActionHandler(this, _model));
        _updateHandlers.Register(new UpdateUserStatusHandler(this, _model));
        _updateHandlers.Register(new UpdateUserHandler(this, _model));
    }

    private void RegisterCommandHandlers()
    {
        _commandHandlers.Register(new MoveDownCommandHandler(_model));
        _commandHandlers.Register(new MoveUpCommandHandler(_model));
        _commandHandlers.Register(new MoveToTopCommandHandler(_model));
        _commandHandlers.Register(new MoveToBottomCommandHandler(_model));
        _commandHandlers.Register(new NextFolderCommandHandler(_model));
        _commandHandlers.Register(new PreviousFolderCommandHandler(_model));
        _commandHandlers.Register(new SelectFolderCommandHandler(_model));
        _commandHandlers.Register(new LastFolderCommandHandler(_model));
        _commandHandlers.Register(new MoveSeparatorToLeftCommandHandler(_settings, _model));
        _commandHandlers.Register(new MoveSeparatorToRightCommandHandler(_settings, _model));
        _commandHandlers.Register(new LoadChatsCommandHandler(_client, _model));
        _commandHandlers.Register(new LoadMessagesCommandHandler(_client, _renderer, _model));
    }
}