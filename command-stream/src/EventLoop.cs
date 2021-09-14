namespace CommandStream;
using Microsoft.Extensions.Logging;


public class EventLoop
{
    public EventLoop(ICommandRouter commandRouter, ILogger<EventLoop> logger)
    {
        this.CommandRouter = commandRouter;
        this.Logger = logger;
    }

    private ILogger Logger;
    private readonly ICommandRouter CommandRouter;
    private readonly AsyncQueue<JToken> WriteQueue = new();
    private Task? MainThread = null;

    private CancellationTokenSource CancellationTokenSource = new();

    public TextReader InputStream { get; set; } = Console.In;
    public TextWriter OutputStream { get; set; } = Console.Out;

    public void Cancel()
    {
        if (MainThread == null)
        {
            throw new InvalidOperationException("Event loop can only be cancelled once it is running.");
        }
        CancellationTokenSource.Cancel();
    }

    public Task Run(CancellationToken cancellationToken = default)
    {
        if (MainThread != null)
        {
            throw new InvalidOperationException("Event loop can only be run once.");
        }

        var writer = new JsonTextWriter(OutputStream);

        // Allow cancelling from outside this method by chaining a new
        // source that we control to the incoming token.
        cancellationToken.Register(CancellationTokenSource.Cancel);
        var rootCancellationToken = CancellationTokenSource.Token;

        // Chain the two threads together so that if our cancellation token is ever
        // cancelled, so is the one we use for the writer thread.
        var source = new CancellationTokenSource();
        rootCancellationToken.Register(source.Cancel);

        var writerThread = Task.Run(async () =>
        {
            await foreach (var tokenToWrite in WriteQueue.WithCancellation(source.Token))
            {
                var jToken = JToken.FromObject(tokenToWrite);
                Logger.LogDebug("[Q# → Host] {Json}", jToken.ToString());
                await jToken.WriteToAsync(writer, rootCancellationToken);
                await OutputStream.WriteLineAsync();
                await OutputStream.FlushAsync();
            }
        }, source.Token);

        MainThread = Task.Run((Func<Task?>)(async () =>
        {
            Logger.LogDebug("Starting event loop.");
            void WriteToQueue(JToken token, string? context)
            {
                token.AddContext(context);
                WriteQueue.Enqueue(token);
            }

            while (!rootCancellationToken.IsCancellationRequested)
            {
                // Have we reached EOF?
                // Try to read a JSONL object.
                var json = await InputStream.ReadLineAsync();

                // If we got null, then we hit end of file.
                if (json == null)
                {
                    Logger.LogDebug("Hit end of stream, ending event loop.");
                    return;
                }

                // Ignore empty lines.
                if (string.IsNullOrWhiteSpace(json))
                {
                    Logger.LogDebug("Text line was empty, ignoring.");
                    continue;
                }

                var nextToken = JsonConvert.DeserializeObject<CommandToken>(json);
                if (nextToken == null)
                {
                    Logger.LogDebug("Text line `{Json}` deserialized to null, ignoring..", json);
                    continue;
                }

                Logger.LogDebug("[Q# ← Host] {Json}", json);

                var response = await CommandRouter.Run(
                    nextToken.Name,
                    nextToken.Payload,
                    output => WriteToQueue(output, nextToken.Context),
                    rootCancellationToken
                );
                WriteToQueue(response, nextToken.Context);

                await System.Console.Out.FlushAsync();
            }
        }), rootCancellationToken);

        return Task.WhenAny(
            MainThread, 
            MainThread.ContinueWith(async task =>
            {
                await task;
                source.Cancel();
            }).Unwrap()
        ).Unwrap();
    }
}
