namespace tg_cli.ViewModels;

public class Message
{
    public long Id { get; }
    public string Text { get; }

    public Message(long id, string text)
    {
        Id = id;
        Text = text;
    }
}