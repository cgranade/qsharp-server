namespace CommandStream;

public record CompileInput(
    [property: JsonProperty("source")] string Source
);

public record CompileResult(
    [property: JsonProperty("warnings")] IList<string> Warnings,
    [property: JsonProperty("compiled_callables")] IList<string> CompiledCallables
);

[Command("exit")]
public class ExitCommand : BaseCommand<object, object, object, Exception?>
{
    private EventLoop EventLoop;

    public ExitCommand(EventLoop eventLoop)
    {
        EventLoop = eventLoop;
    }

    public override Task<Result<object, Exception?>> Run(object input, Action<object> output, CancellationToken cancellationToken)
    {
        try
        {
            EventLoop.Cancel();
            return Task.FromResult(
                (Result<object, Exception?>)new Ok<object>(new object())
            );
        }
        catch (InvalidOperationException ex)
        {
            return Task.FromResult(
                (Result<object, Exception?>)new Error<Exception?>(
                    ErrorIds.UnknownException,
                    ex.Message,
                    ex
                )
            );
        }
    }
}
