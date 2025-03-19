namespace TgCli.Commands;

public abstract class ParametrizedInputCommand : InputCommand
{
    private string CommandHead { get; }
    private string CommandTail { get; }
    private HashSet<char> AllowedParameters { get; }
    private char ParameterPlaceholder { get; }

    protected ParametrizedInputCommand(Model model, string commandTemplate, string allowedParameters, char parameterPlaceholder = '_') : base(model, commandTemplate)
    {
        var templateSplit = commandTemplate.Split(parameterPlaceholder, StringSplitOptions.TrimEntries);
        if (templateSplit.Length != 2)
            throw new NotSupportedException($"Template '{commandTemplate}' contains less or more than one '{parameterPlaceholder}'");

        if (allowedParameters.Length == 0)
            throw new NotSupportedException("Command has no allowed parameters");

        CommandHead = templateSplit[0];
        CommandTail = templateSplit[1];

        AllowedParameters = allowedParameters.ToCharArray().ToHashSet();
        ParameterPlaceholder = parameterPlaceholder;
    }

    public sealed override bool StartsWith(string input)
    {
        if (input.Length <= CommandHead.Length)
            return CommandHead.StartsWith(input);

        var inputHead = input[..CommandHead.Length];
        if (input.Length - CommandHead.Length == 1)
            return CommandHead == inputHead && AllowedParameters.Contains(input[^1]);

        var inputParameter = input[CommandHead.Length + 1];
        if (input.Length <= CommandHead.Length + 1 + CommandTail.Length)
            return
                CommandHead == inputHead
                && AllowedParameters.Contains(inputParameter)
                && CommandTail.StartsWith(input[(CommandHead.Length + 1 + 1)..]);

        return false;
    }

    public sealed override bool Match(string input, out string parameter)
    {
        parameter = null;
        if (input.Length != CommandHead.Length + 1 + CommandTail.Length)
            return false;

        var inputHead = input[..CommandHead.Length];
        var inputParameter = input[CommandHead.Length + 1];
        var inputTail = input[(CommandHead.Length + 1 + 1)..];

        var didMatch =
            inputHead == CommandHead
            && AllowedParameters.Contains(inputParameter)
            && inputTail == CommandTail;

        if (didMatch)
            parameter = inputParameter.ToString();

        return didMatch;
    }
}

