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

    private void RequestRender()
    {
        var count = Math.Min(_model.SelectedFolder.SortedChats.Count, VisibleChatsCount);
        IReadOnlyList<Chat> visibleChats = count > 0
            ? _model.SelectedFolder.SortedChats.GetRange(TopChatIndex, count)
            : Array.Empty<Chat>();

        var visibleInterface = new VisibleInterface(visibleChats, RelativeSelectedChatIndex,
            _commandInput, _model.Folders, _model.SelectedFolderIndex, _model.Users);

        RenderRequested?.Invoke(visibleInterface);
    }
    
    private void RegisterUpdateHandlers()
    {
        _updateHandlers.Register(new UpdateNewChatHandler(_model));
        _updateHandlers.Register(new UpdateChatPositionHandler(_model));
        _updateHandlers.Register(new UpdateChatLastMessageHandler(_model));
        _updateHandlers.Register(new UpdateChatReadInboxHandler(_model));
        _updateHandlers.Register(new UpdateScopeNotificationSettingsHandler(_model));
        _updateHandlers.Register(new UpdateChatNotificationSettingsHandler(_model));
        _updateHandlers.Register(new UpdateChatFoldersHandler(_model));
        _updateHandlers.Register(new UpdateChatActionHandler(_model));
        _updateHandlers.Register(new UpdateUserStatusHandler(_model));
        _updateHandlers.Register(new UpdateUserHandler(_model));
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
    }
}