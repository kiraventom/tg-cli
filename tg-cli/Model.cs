using System.Collections.ObjectModel;
using System.Collections.Specialized;
using TdLib;

namespace tg_cli;

public struct VisibleInterface
{
    public IReadOnlyList<Folder> Folders { get; }
    public int SelectedFolderIndex { get; }
    public IReadOnlyList<Chat> Chats { get; }
    public int SelectedChatIndex { get; }
    public string CommandInput { get; }

    public VisibleInterface(IReadOnlyList<Chat> chats, int selectedChatIndex, string commandInput,
        IReadOnlyList<Folder> folders, int selectedFolderIndex)
    {
        Chats = chats;
        SelectedChatIndex = selectedChatIndex;
        CommandInput = commandInput;
        Folders = folders;
        SelectedFolderIndex = selectedFolderIndex;
    }
}

public class Chat
{
    public class Comparer : IComparer<Chat>
    {
        private readonly Folder _folder;

        public Comparer(Folder folder)
        {
            _folder = folder;
        }

        public int Compare(Chat x, Chat y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(null, y)) return 1;
            if (ReferenceEquals(null, x)) return -1;
            x.Positions.TryGetValue(_folder, out var xPosition);
            y.Positions.TryGetValue(_folder, out var yPosition);
            return yPosition.CompareTo(xPosition);
        }
    }

    public long Id { get; }
    public Dictionary<Folder, long> Positions { get; } = new();
    public string Title { get; set; }
    public int UnreadCount { get; set; }
    public bool IsMuted { get; set; }
    public bool IsPrivate => Id > 0;
    public string LastMessagePreview { get; set; }

    public Chat(long id, string title)
    {
        Id = id;
        Title = title;
    }
}

public class Folder
{
    public int TopChatIndex { get; set; }
    public int SelectedChatIndex { get; set; }
    public int RelativeSelectedChatIndex => SelectedChatIndex - TopChatIndex;

    public ObservableCollection<Chat> Chats { get; } = new();
    public List<Chat> SortedChats { get; } = new();
    public Dictionary<long, Chat> ChatsDict { get; } = new();

    public long Id { get; }
    public string Title { get; }

    public Folder(long id, string title)
    {
        Id = id;
        Title = title;
        Chats.CollectionChanged += OnChatsCollectionChanged;
    }

    public void TriggerSort()
    {
        SortedChats.Sort(new Chat.Comparer(this));
    }

    private void OnChatsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                var newChat = e.NewItems!.OfType<Chat>().Single();
                SortedChats.Add(newChat);
                SortedChats.Sort(new Chat.Comparer(this));
                ChatsDict.Add(newChat.Id, newChat);
                break;

            case NotifyCollectionChangedAction.Remove:
                var oldChat = e.OldItems!.OfType<Chat>().Single();
                SortedChats.Remove(oldChat);
                SortedChats.Sort(new Chat.Comparer(this));
                ChatsDict.Remove(oldChat.Id);
                break;

            case NotifyCollectionChangedAction.Replace:
            case NotifyCollectionChangedAction.Move:
            case NotifyCollectionChangedAction.Reset:
            default:
                throw new NotSupportedException($"{e.Action} is not expected");
        }
    }
}

public class Model
{
    private readonly IRenderer _renderer;
    private readonly TgCliSettings _settings;
    private readonly List<Folder> _folders = new();

    private bool _muteChanneldByDefault;

    private int _selectedFolderIndex;

    private string _commandInput = string.Empty;

    private Folder AllChatsFolder => _folders[0];
    private Folder SelectedFolder => _folders[_selectedFolderIndex];

    private int BottomChatIndex => SelectedFolder.TopChatIndex + VisibleChatsCount - 1;
    private int VisibleChatsCount => _renderer.MaxVisibleChatsCount;

    public event Action<VisibleInterface> RenderRequested;

    public Model(IRenderer renderer, TgCliSettings settings)
    {
        _renderer = renderer;
        _settings = settings;
        _folders.Add(new Folder(-1, "All chats"));
    }

    public void OnClientUpdateReceived(object sender, TdApi.Update update)
    {
        Program.Logger.LogUpdate(update, AllChatsFolder.ChatsDict);

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

                if (!newChat.IsPrivate)
                    newChat.IsMuted = _muteChanneldByDefault;

                AllChatsFolder.Chats.Add(newChat);

                if (SelectedFolder.Chats.Count - 1 > VisibleChatsCount)
                    return;

                return;
            }

            case TdApi.Update.UpdateChatPosition updateChatPosition:
            {
                var newIndex = SetChatPosition(updateChatPosition.ChatId, updateChatPosition.Position);
                if (newIndex == -1 || newIndex > VisibleChatsCount - 1)
                    return;
                
                break;
            }

            case TdApi.Update.UpdateChatLastMessage updateChatLastMessage:
            {
                var content = updateChatLastMessage?.LastMessage?.Content?.GetContentString();
                if (!AllChatsFolder.ChatsDict.TryGetValue(updateChatLastMessage.ChatId, out var chat))
                    return; // TODO

                chat.LastMessagePreview = content;

                var requestRender = false;
                foreach (var position in updateChatLastMessage.Positions)
                {
                    var newIndex = SetChatPosition(updateChatLastMessage.ChatId, position);
                    if (newIndex == -1 || newIndex > VisibleChatsCount - 1)
                        continue;
                        
                    requestRender = true;
                };
                
                if (!requestRender)
                    return;

                break;
            }

            case TdApi.Update.UpdateChatReadInbox updateChatReadInbox:
            {
                if (!AllChatsFolder.ChatsDict.TryGetValue(updateChatReadInbox.ChatId, out var chat))
                    return; // TODO

                chat.UnreadCount = updateChatReadInbox.UnreadCount;
                break;
            }

            case TdApi.Update.UpdateScopeNotificationSettings updateScopeNotificationSettings:
            {
                _muteChanneldByDefault = updateScopeNotificationSettings.NotificationSettings.MuteFor != 0;
                return;
            }

            case TdApi.Update.UpdateChatNotificationSettings updateChatNotificationSettings:
            {
                if (!AllChatsFolder.ChatsDict.TryGetValue(updateChatNotificationSettings.ChatId, out var chat))
                    return; // TODO

                if (!updateChatNotificationSettings.NotificationSettings.UseDefaultMuteFor)
                    chat.IsMuted = updateChatNotificationSettings.NotificationSettings.MuteFor != 0;
                break;
            }

            case TdApi.Update.UpdateChatFolders updateChatFolders:
            {
                foreach (var chatFolderInfo in updateChatFolders.ChatFolders)
                {
                    var folder = new Folder(chatFolderInfo.Id, chatFolderInfo.Title);
                    _folders.Add(folder);
                }

                break;
            }

            default:
                return;
        }

        RequestRender();
    }
    
    public void OnListenerCommandReceived(Command command)
    {
        switch (command.Type)
        {
            case CommandType.MoveDown:
                SelectChatAt(SelectedFolder.SelectedChatIndex + 1);
                break;

            case CommandType.MoveUp:
                SelectChatAt(SelectedFolder.SelectedChatIndex - 1);
                break;

            case CommandType.MoveToTop:
                SelectChatAt(0);
                break;

            case CommandType.MoveToBottom:
                SelectChatAt(SelectedFolder.Chats.Count - 1);
                break;

            case CommandType.NextFolder:
                SelectFolderAt(_selectedFolderIndex + 1);
                break;

            case CommandType.PreviousFolder:
                SelectFolderAt(_selectedFolderIndex - 1);
                break;

            case CommandType.SelectFolder:
                if (!int.TryParse(command.Parameter, out var index))
                    return;

                if (index > _folders.Count - 1)
                    index = _folders.Count - 1;

                SelectFolderAt(index);
                break;

            case CommandType.LastFolder:
                SelectFolderAt(_folders.Count - 1);
                break;

            case CommandType.MoveSeparatorToLeft:
                _settings.SeparatorOffset -= 1;
                RequestRender();
                break;

            case CommandType.MoveSeparatorToRight:
                _settings.SeparatorOffset += 1;
                RequestRender();
                break;
        }
    }

    public void OnListenerInputReceived(string input)
    {
        _commandInput = input;
        RequestRender();
    }

    private void SelectChatAt(int index)
    {
        if (index < 0)
            index = 0;

        if (index > SelectedFolder.Chats.Count - 1)
            index = SelectedFolder.Chats.Count - 1;

        if (index == SelectedFolder.SelectedChatIndex)
            return;

        SelectedFolder.SelectedChatIndex = index;

        if (SelectedFolder.SelectedChatIndex < SelectedFolder.TopChatIndex)
            SelectedFolder.TopChatIndex = SelectedFolder.SelectedChatIndex;

        if (SelectedFolder.SelectedChatIndex > BottomChatIndex)
            SelectedFolder.TopChatIndex = SelectedFolder.SelectedChatIndex - (VisibleChatsCount - 1);

        RequestRender();
    }

    private void SelectFolderAt(int index)
    {
        if (index < 0)
            index = _folders.Count - 1;

        if (index > _folders.Count - 1)
            index = 0;

        if (index == _selectedFolderIndex)
            return;

        _selectedFolderIndex = index;

        RequestRender();
    }

    private void RequestRender()
    {
        var count = Math.Min(SelectedFolder.SortedChats.Count, VisibleChatsCount);
        IReadOnlyList<Chat> visibleChats = count > 0
            ? SelectedFolder.SortedChats.GetRange(SelectedFolder.TopChatIndex, count)
            : Array.Empty<Chat>();
            
        var visibleInterface = new VisibleInterface(visibleChats, SelectedFolder.RelativeSelectedChatIndex,
            _commandInput, _folders, _selectedFolderIndex);

        RenderRequested?.Invoke(visibleInterface);
    }

    private int SetChatPosition(long chatId, TdApi.ChatPosition position)
    {
        var folder = position.List switch
        {
            TdApi.ChatList.ChatListFolder chatListFolder =>
                _folders.First(f => f.Id == chatListFolder.ChatFolderId),
            TdApi.ChatList.ChatListMain => AllChatsFolder,
            TdApi.ChatList.ChatListArchive => null, // TODO
            _ => throw new NotSupportedException()
        };

        if (folder is null)
            return -1;

        if (!folder.ChatsDict.TryGetValue(chatId, out var chat))
        {
            chat = AllChatsFolder.ChatsDict[chatId];
            folder.Chats.Add(chat);
        }

        chat.Positions[folder] = position.Order;
        folder.TriggerSort();

        if (folder != SelectedFolder)
            return -1;

        var sortedIndex = folder.SortedChats.IndexOf(chat);
        return sortedIndex;
    }
}