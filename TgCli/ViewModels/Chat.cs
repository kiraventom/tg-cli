using TgCli.Utils;

namespace TgCli.ViewModels;

public class Chat : IRenderChat
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
    
    private readonly CovariantKeyDictionaryWrapper<IRenderFolder, Folder, long> _readonlyPositions;

    public long Id { get; }
    public Dictionary<Folder, long> Positions { get; } = new();
    
    IReadOnlyDictionary<IRenderFolder, long> IRenderChat.Positions => _readonlyPositions;
    IRenderMessage IRenderChat.LastMessage => LastMessage;
    
    public string Title { get; set; }
    public int UnreadCount { get; set; }
    public bool IsMuted { get; set; }
    public bool IsPrivate => Id > 0;
    public List<Message> Messages { get; } = new();
    public Message LastMessage { get; set; }
    public string ChatAction { get; set; }

    public Chat(long id, string title)
    {
        Id = id;
        Title = title;
        _readonlyPositions = new CovariantKeyDictionaryWrapper<IRenderFolder, Folder, long>(Positions);
    }
}