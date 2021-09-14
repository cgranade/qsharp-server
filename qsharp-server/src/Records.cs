using Newtonsoft.Json.Converters;
using OneOf;

namespace QSharpStream;

[JsonConverter(typeof(StringEnumConverter))]
public enum ConsoleStreamKind
{
    StandardOut,
    StandardError
}

public record ConsoleMessage(
    [property: JsonProperty("console_message")] string Text,
    [property: JsonProperty("stream")] ConsoleStreamKind StreamKind = ConsoleStreamKind.StandardOut
);

public record DisplayOutput(
    [property: JsonProperty("display_output")] object Diagnostic
);

// TODO: Generalize this more later and hook up to display encoding.
[JsonConverter(typeof(ChannelOutputConverter))]
public class ChannelOutput : OneOfBase<ConsoleMessage, DisplayOutput>
{
    protected ChannelOutput(OneOf<ConsoleMessage, DisplayOutput> input) : base(input) { }

    public static implicit operator ChannelOutput(ConsoleMessage _) => new ChannelOutput(_);
    public static implicit operator ChannelOutput(DisplayOutput _) => new ChannelOutput(_);
}
