namespace tg_cli.ViewModels;

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
    public string ChatAction { get; set; }

    public Chat(long id, string title)
    {
        Id = id;
        Title = title;
    }
}