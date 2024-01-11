using System.Collections.ObjectModel;
using System.Collections.Specialized;
using TdLib;

namespace tg_cli;

public struct VisibleInterface
{
    public IReadOnlyList<Chat> Chats { get; }
    public int SelectedIndex { get; }
    public string CommandInput { get; }

    public VisibleInterface(IReadOnlyList<Chat> chats, int selectedIndex, string commandInput)
    {
        Chats = chats;
        SelectedIndex = selectedIndex;
        CommandInput = commandInput;
    }
}

public class Chat
{
    public class Comparer : IComparer<Chat>
    {
        public static Comparer Instance { get; } = new();

        private Comparer()
        {
        }

        public int Compare(Chat x, Chat y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(null, y)) return 1;
            if (ReferenceEquals(null, x)) return -1;
            return y.Position.CompareTo(x.Position);
        }
    }

    public long Id { get; }
    public long Position { get; set; }
    public string Title { get; set; }
    public int UnreadCount { get; set; }
    public bool IsMuted { get; set; }

    public Chat(long id, string title)
    {
        Id = id;
        Title = title;
    }
}

public class Model
{
    private readonly IRenderer _renderer;
    
    private readonly Dictionary<string, bool> _scopesMuted = new();

    private readonly ObservableCollection<Chat> _chats = new();
    private readonly List<Chat> _sortedChats = new();
    private readonly Dictionary<long, Chat> _chatsDict = new();

    private int _topChatIndex;
    private int _selectedChatIndex;

    private string _commandInput = string.Empty;

    private int VisibleChatsCount => _renderer.VisibleChatsCount;
    private int BottomChatIndex => _topChatIndex + VisibleChatsCount - 1;
    private int RelativeSelectedChatIndex => _selectedChatIndex - _topChatIndex;

    public event Action<VisibleInterface> RenderRequested;

    public Model(IRenderer renderer)
    {
        _renderer = renderer;
        _chats.CollectionChanged += OnChatsCollectionChanged;
    }

    public async void OnClientUpdateReceived(object sender, TdApi.Update update)
    {
        update.Log(_chatsDict);
        switch (update)
        {
            case TdApi.Update.UpdateNewChat updateNewChat:
            {
                var chat = updateNewChat.Chat;
                var chatTitle = Utils.RemoveNonUtf16Characters(chat.Title);
                var newChat = new Chat(chat.Id, chatTitle)
                {
                    UnreadCount = chat.UnreadCount,
                };

                _chats.Add(newChat);
                if (_chats.Count - 1 > VisibleChatsCount)
                    return;
                break;
            }

            case TdApi.Update.UpdateChatPosition updateChatPosition:
            {
                if (updateChatPosition.Position.List.DataType != "chatListMain")
                    return;

                if (!_chatsDict.TryGetValue(updateChatPosition.ChatId, out var chat))
                    return; // TODO

                chat.Position = updateChatPosition.Position.Order;
                break;
            }

            case TdApi.Update.UpdateChatReadInbox updateChatReadInbox:
            {
                if (!_chatsDict.TryGetValue(updateChatReadInbox.ChatId, out var chat))
                    return; // TODO

                chat.UnreadCount = updateChatReadInbox.UnreadCount;
                break;
            }

            case TdApi.Update.UpdateScopeNotificationSettings updateScopeNotificationSettings:
            {
                _scopesMuted[updateScopeNotificationSettings.Scope.DataType] = updateScopeNotificationSettings.NotificationSettings.MuteFor != 0;
                return;
            }

            default:
                return;
        }

        RequestRender();
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
                SelectIndex(_chats.Count - 1);
                break;
        }
    }

    public void OnListenerInputReceived(string input)
    {
        _commandInput = input;
        RequestRender();
    }

    private void SelectIndex(int index)
    {
        if (index < 0)
            index = 0;

        if (index > _chats.Count - 1)
            index = _chats.Count - 1;

        if (index == _selectedChatIndex)
            return;

        _selectedChatIndex = index;

        if (_selectedChatIndex < _topChatIndex)
            _topChatIndex = _selectedChatIndex;

        if (_selectedChatIndex > BottomChatIndex)
            _topChatIndex = _selectedChatIndex - (VisibleChatsCount - 1);

        RequestRender();
    }

    private void RequestRender()
    {
        var count = Math.Min(_sortedChats.Count, VisibleChatsCount);
        var visibleChats = _sortedChats.GetRange(_topChatIndex, count);
        var visibleInterface = new VisibleInterface(visibleChats, RelativeSelectedChatIndex, _commandInput);
        RenderRequested?.Invoke(visibleInterface);
    }

    private void OnChatsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                var newChat = e.NewItems!.OfType<Chat>().Single();
                _sortedChats.Add(newChat);
                _sortedChats.Sort(Chat.Comparer.Instance);
                _chatsDict.Add(newChat.Id, newChat);
                break;

            case NotifyCollectionChangedAction.Remove:
                var oldChat = e.OldItems!.OfType<Chat>().Single();
                _sortedChats.Remove(oldChat);
                _sortedChats.Sort(Chat.Comparer.Instance);
                _chatsDict.Remove(oldChat.Id);
                break;

            case NotifyCollectionChangedAction.Replace:
            case NotifyCollectionChangedAction.Move:
            case NotifyCollectionChangedAction.Reset:
            default:
                throw new NotSupportedException($"{e.Action} is not expected");
        }
    }
}