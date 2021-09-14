namespace CommandStream;

public record CommandToken(
    [property: JsonProperty("command")] string Name,
    [property: JsonProperty("payload")] JToken Payload,
    [property: JsonProperty("context", NullValueHandling = NullValueHandling.Ignore)]
    string? Context = null
);

/// <summary>
///     Represents a single command sent by the client to the command stream
///     server.
/// </summary>
public record CommandMessage(
    [property: JsonProperty("command")]
    string Name,

    [property: JsonProperty("payload")]
    JToken Payload,

    [property: JsonProperty("context", NullValueHandling = NullValueHandling.Ignore)]
    string? Context = null
);

public record Error(
    [property: JsonProperty("error-id")]
    string Id,

    [property: JsonProperty("message")]
    string Message
);

public record Error<TError>(
    string Id,
    string Message,

    [property: JsonProperty("details")]
    TError Details
) : Error(Id, Message);


public record Ok();

public record Ok<TResult>(
    [property: JsonProperty("data")]
    TResult Data
) : Ok;

public class Result<TOk, TError> : OneOfBase<Ok<TOk>, Error<TError>>
{
    internal Result(OneOf<Ok<TOk>, Error<TError>> _) : base(_) { }

    public static implicit operator Result<TOk, TError>(Ok<TOk>  _) => new Result<TOk, TError>(_);
    public static implicit operator Result<TOk, TError>(Error<TError>  _) => new Result<TOk, TError>(_);
}
