namespace TgCli.ViewModels;

public class Message : IRenderMessage
{
    public long Id { get; }
    public string Text { get; }

    public Message(long id, string text)
    {
        Id = id;
        Text = text;
    }
}