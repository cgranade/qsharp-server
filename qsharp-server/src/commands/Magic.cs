using Microsoft.Jupyter.Core;
using Microsoft.Quantum.IQSharp.Common;
using Microsoft.Quantum.IQSharp.Jupyter;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace QSharpStream;

public record MagicInput(
    [property: JsonProperty("input")] string Input
);

public record MagicResult(
    // TODO: Make mime bundles.
    [property: JsonProperty("output")] object Output
);

public class MagicChannel : IChannel
{
    private readonly Action<ChannelOutput> Output;

    public MagicChannel(Action<ChannelOutput> output)
    {
        Output = output;
    }

    public void Display(object displayable)
    {
        Output(new DisplayOutput(displayable));
    }

    public void Stderr(string message)
    {
        Output(new ConsoleMessage(message, ConsoleStreamKind.StandardError));
    }

    public void Stdout(string message)
    {
        Output(new ConsoleMessage(message));
    }
}

// NB: We intentionally don't place an attribute here, as we'll discover
//     instances rather than types.
public class MagicCommand : BaseCommand<MagicInput, ChannelOutput, MagicResult, Exception?>
{
    private AbstractMagic Magic;

    public MagicCommand(AbstractMagic magic)
    {
        this.Magic = magic;
    }

    public override async Task<Result<MagicResult, Exception?>> Run(MagicInput input, Action<ChannelOutput> output, CancellationToken cancellationToken)
    {
        try
        {
            var result = await Magic.ExecuteCancellable(input.Input, new MagicChannel(output), cancellationToken);
            // TODO: Encode here!
            return new Ok<MagicResult>(new MagicResult(result));
        }
        catch (Exception ex)
        {
            return new Error<Exception?>(ErrorIds.UnknownException, ex.Message, ex);
        }
    }
}
