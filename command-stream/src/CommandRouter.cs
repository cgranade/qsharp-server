namespace CommandStream;

public static class ErrorIds
{
    public const string SerializionError = "json-serialization-error";
    public const string UnknownException = "unknown-exception";
    public const string NoSuchCommand = "no-such-command";
    public const string NoSuchOperation = "no-such-operation";
    // FIXME: move out of this project.
    public const string CompilationError = "compilation-error";
}

public interface ICommandRouter
{
    Task<JToken> Run(string commandName, JToken input, Action<JToken> output, CancellationToken cancellationToken);
    void Add(string commandName, ICommand command);
}

public class CommandRouter : ICommandRouter
{
    private Dictionary<string, ICommand> Commands = new();

    public async Task<JToken> Run(string commandName, JToken input, Action<JToken> output, CancellationToken cancellationToken)
    {
        // NB: commandName should be non-nullable, but JSON serialization may
        //     violate that.
        if (commandName != null && Commands.TryGetValue(commandName, out var command))
        {
            return await command.Run(input, output, cancellationToken);
        }
        else
        {
            return JToken.FromObject(new Error(
                Id: ErrorIds.NoSuchCommand,
                Message: $"No command named {commandName}."
            ));
        }
    }

    public void Add(string commandName, ICommand command)
    {
        Commands.Add(commandName, command);
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class CommandAttribute : Attribute
{

    public string CommandName { get; init; }
    public CommandAttribute(string commandName)
    {
        this.CommandName = commandName;
    }

}
