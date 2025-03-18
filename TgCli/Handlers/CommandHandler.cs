using TgCli.Extensions;
using TgCli.ViewModels;

namespace TgCli.Handlers;

// TODO: Move all code to Commands and get rid of CommandHandlers
public abstract class CommandHandler<T> : IHandler<Command>
{
    protected Model Model { get; }

    protected CommandHandler(Model model)
    {
        Model = model;
    }

    protected abstract Task<bool> HandleAsync(T command);

    public async Task<bool> HandleAsync(Command obj)
    {
        if (obj is not T t)
            throw new NotSupportedException();

        return await HandleAsync(t);
    }

    public bool CanHandle(Command obj) => obj is T;
}

public class MoveDownCommandHandler : CommandHandler<MoveDownCommand>
{
    public MoveDownCommandHandler(Model model) : base(model)
    {
    }

    protected override Task<bool> HandleAsync(MoveDownCommand command)
    {
        Model.SelectChatAt(Model.SelectedFolder.SelectedChatIndex + 1);
        return Task.FromResult(true);
    }
}

public class MoveUpCommandHandler : CommandHandler<MoveUpCommand>
{
    public MoveUpCommandHandler(Model model) : base(model)
    {
    }

    protected override Task<bool> HandleAsync(MoveUpCommand command)
    {
        Model.SelectChatAt(Model.SelectedFolder.SelectedChatIndex - 1);
        return Task.FromResult(true);
    }
}

public class MoveToTopCommandHandler : CommandHandler<MoveToTopCommand>
{
    public MoveToTopCommandHandler(Model model) : base(model)
    {
    }

    protected override Task<bool> HandleAsync(MoveToTopCommand command)
    {
        Model.SelectChatAt(0);
        return Task.FromResult(true);
    }
}

public class MoveToBottomCommandHandler : CommandHandler<MoveToBottomCommand>
{
    public MoveToBottomCommandHandler(Model model) : base(model)
    {
    }

    protected override Task<bool> HandleAsync(MoveToBottomCommand command)
    {
        Model.SelectChatAt(Model.SelectedFolder.Chats.Count - 1);
        return Task.FromResult(true);
    }
}

public class NextFolderCommandHandler : CommandHandler<NextFolderCommand>
{
    public NextFolderCommandHandler(Model model) : base(model)
    {
    }

    protected override Task<bool> HandleAsync(NextFolderCommand command)
    {
        Model.SelectFolderAt(Model.SelectedFolderIndex + 1);
        return Task.FromResult(true);
    }
}

public class PreviousFolderCommandHandler : CommandHandler<PreviousFolderCommand>
{
    public PreviousFolderCommandHandler(Model model) : base(model)
    {
    }

    protected override Task<bool> HandleAsync(PreviousFolderCommand command)
    {
        Model.SelectFolderAt(Model.SelectedFolderIndex - 1);
        return Task.FromResult(true);
    }
}

public class SelectFolderCommandHandler : CommandHandler<SelectFolderCommand>
{
    public SelectFolderCommandHandler(Model model) : base(model)
    {
    }

    protected override Task<bool> HandleAsync(SelectFolderCommand command)
    {
        var index = command.FolderIndex;
        if (index > Model.Folders.Count - 1)
            index = Model.Folders.Count - 1;

        Model.SelectFolderAt(index);
        return Task.FromResult(true);
    }
}

public class LastFolderCommandHandler : CommandHandler<LastFolderCommand>
{
    public LastFolderCommandHandler(Model model) : base(model)
    {
    }

    protected override Task<bool> HandleAsync(LastFolderCommand command)
    {
        Model.SelectFolderAt(Model.Folders.Count - 1);
        return Task.FromResult(true);
    }
}

public class MoveSeparatorToLeftCommandHandler : CommandHandler<MoveSeparatorToLeftCommand>
{
    private TgCliSettings Settings { get; }

    public MoveSeparatorToLeftCommandHandler(TgCliSettings settings, Model model) : base(model)
    {
        Settings = settings;
    }

    protected override Task<bool> HandleAsync(MoveSeparatorToLeftCommand command)
    {
        Settings.SeparatorOffset -= 1;
        return Task.FromResult(true);
    }
}

public class MoveSeparatorToRightCommandHandler : CommandHandler<MoveSeparatorToRightCommand>
{
    private TgCliSettings Settings { get; }

    public MoveSeparatorToRightCommandHandler(TgCliSettings settings, Model model) : base(model)
    {
        Settings = settings;
    }

    protected override Task<bool> HandleAsync(MoveSeparatorToRightCommand command)
    {
        Settings.SeparatorOffset += 1;
        return Task.FromResult(true);
    }
}

public class LoadChatsCommandHandler : CommandHandler<LoadChatsCommand>
{
    private IClient Client { get; }

    public LoadChatsCommandHandler(IClient client, Model model) : base(model)
    {
        Client = client;
    }

    protected override async Task<bool> HandleAsync(LoadChatsCommand command)
    {
        await Client.LoadChatsAsync(Model.SelectedFolder.Id);
        return false;
    }
}

public class LoadMessagesCommandHandler : CommandHandler<LoadMessagesCommand>
{
    private IRenderer Renderer { get; }
    private IClient Client { get; }

    public LoadMessagesCommandHandler(IClient client, IRenderer renderer, Model model) : base(model)
    {
        Renderer = renderer;
        Client = client;
    }

    protected override async Task<bool> HandleAsync(LoadMessagesCommand command)
    {
        var chatId = Model.SelectedFolder.SelectedChat.Id;
        long lastMessageId = 0;
        if (Model.SelectedFolder.SelectedChat.Messages.Any())
        {
            lastMessageId = Model.SelectedFolder.SelectedChat.Messages.First().Id;
        }
        else if (Model.SelectedFolder.SelectedChat.LastMessage is not null)
        {
            lastMessageId = Model.SelectedFolder.SelectedChat.LastMessage.Id;
            Model.SelectedFolder.SelectedChat.Messages.Insert(0, Model.SelectedFolder.SelectedChat.LastMessage);
        }

        var messagesCount = Renderer.MaxVisibleMessagesCount;

        while (Model.SelectedFolder.SelectedChat.Messages.Count < messagesCount)
        {
            var messages = await Client.LoadMessagesAsync(chatId, lastMessageId, 0, messagesCount);
            if (messages?.Messages_ is null)
                return false;

            foreach (var message in messages.Messages_)
            {
                var messageViewModel = new Message(message.Id, message.Content.GetContentString());
                Model.SelectedFolder.SelectedChat.Messages.Insert(0, messageViewModel);
            }
        }

        return true;
    }
}