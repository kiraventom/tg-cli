using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace TgCli.ViewModels;

public class Folder : IRenderFolder
{
    public int SelectedChatIndex { get; set; }

    public Chat SelectedChat => SelectedChatIndex < SortedChats.Count ? SortedChats[SelectedChatIndex] : null;

    public ObservableCollection<Chat> Chats { get; } = new();
    public List<Chat> SortedChats { get; } = new();
    public Dictionary<long, Chat> ChatsDict { get; } = new();

    public int Id { get; }
    public string Title { get; }

    public int UnreadChatsCount => Chats.Count(c => c.UnreadCount > 0);
    public bool HasUnmutedChat => Chats.Where(c => c.UnreadCount > 0).Any(c => !c.IsMuted);
    
    IRenderChat IRenderFolder.SelectedChat => SelectedChat;

    public Folder(int id, string title)
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