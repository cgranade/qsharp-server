using System.Diagnostics;

namespace CommandStream;

public interface ICommand
{
    Task<JToken> Run(JToken input, Action<JToken> output, CancellationToken cancellationToken);
}

public abstract class BaseCommand<TInput, TOutput, TOk, TError> : ICommand
{
    public abstract Task<Result<TOk, TError>> Run(TInput input, Action<TOutput> output, CancellationToken cancellationToken);

    // Handles turning strongly typed commands into commands that shuffle
    // JTokens around.
    async Task<JToken> ICommand.Run(JToken inputToken, Action<JToken> output, CancellationToken cancellationToken) =>
        await inputToken.MaybeToObject<TInput>().Match(
            async input =>
            {
                void WriteOutput(TOutput outValue)
                {
                    output(outValue == null ? JValue.CreateNull() : JToken.FromObject(outValue));
                }

                Debug.Assert(input != null);
                try
                {
                    return (await Run(input, WriteOutput, cancellationToken)).Match(
                        ok => JToken.FromObject(ok),
                        error => JToken.FromObject(error)
                    );
                }
                catch (Exception ex)
                {
                    return JToken.FromObject(new Error<Exception>(
                        Id: ErrorIds.UnknownException,
                        Message: ex.Message,
                        Details: ex
                    ));
                }
            },
            ex => Task.FromResult(JToken.FromObject(new Error<JsonSerializationException>(
                Id: ErrorIds.SerializionError,
                Message: ex.Message,
                Details: ex
            )))
        );
}