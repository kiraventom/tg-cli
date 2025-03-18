namespace TgCli;

public class QuitCommand : InputCommand
{
    public QuitCommand() : base("q")
    {

    }

    public override Task Execute(string parameter = null)
    {
        return Task.CompletedTask;
    }
}

public class MoveUpCommand : InputCommand
{
    public MoveUpCommand() : base("k")
    {

    }

    public override Task Execute(string parameter = null)
    {
        return Task.CompletedTask;
    }
}

public class MoveDownCommand : InputCommand
{
    public MoveDownCommand() : base("j")
    {

    }

    public override Task Execute(string parameter = null)
    {
        return Task.CompletedTask;
    }
}

public class MoveToTopCommand : InputCommand
{
    public MoveToTopCommand() : base("gg")
    {

    }

    public override Task Execute(string parameter = null)
    {
        return Task.CompletedTask;
    }
}

public class MoveToBottomCommand : InputCommand
{
    public MoveToBottomCommand() : base("G")
    {

    }

    public override Task Execute(string parameter = null)
    {
        return Task.CompletedTask;
    }
}

public class NextFolderCommand : InputCommand
{
    public NextFolderCommand() : base("gt")
    {

    }

    public override Task Execute(string parameter = null)
    {
        return Task.CompletedTask;
    }
}

public class PreviousFolderCommand : InputCommand
{
    public PreviousFolderCommand() : base("gT")
    {

    }

    public override Task Execute(string parameter = null)
    {
        return Task.CompletedTask;
    }
}

public class SelectFolderCommand : ParametrizedInputCommand
{
    public SelectFolderCommand() : base("g_t", "0123456789")
    {

    }

    public override Task Execute(string parameter = null)
    {
        return Task.CompletedTask;
    }
}

public class LoadChatsCommand : InputCommand
{
    public LoadChatsCommand() : base("R")
    {

    }

    public override Task Execute(string parameter = null)
    {
        return Task.CompletedTask;
    }
}

public class LoadMessagesCommand : InputCommand
{
    public LoadMessagesCommand() : base("l")
    {

    }

    public override Task Execute(string parameter = null)
    {
        return Task.CompletedTask;
    }
}
