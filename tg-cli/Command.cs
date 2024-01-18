namespace tg_cli;

public abstract class Command
{
    public string Parameter { get; }

    protected Command()
    {
    }

    protected Command(string parameter)
    {
        Parameter = parameter;
    }
}

public class QuitCommand : Command
{
}

public class MoveUpCommand : Command
{
}

public class MoveDownCommand : Command
{
}

public class MoveToTopCommand : Command
{
}

public class MoveToBottomCommand : Command
{
}

public class NextFolderCommand : Command
{
}

public class PreviousFolderCommand : Command
{
}

public class SelectFolderCommand : Command
{
    public int FolderIndex { get; }

    public SelectFolderCommand(string parameter) : base(parameter)
    {
        FolderIndex = int.Parse(parameter);
    }
}

public class LastFolderCommand : Command
{
}

public class MoveSeparatorToLeftCommand : Command
{
}

public class MoveSeparatorToRightCommand : Command
{
}

public class LoadChatsCommand : Command
{
}

public class LoadMessagesCommand : Command
{
}
