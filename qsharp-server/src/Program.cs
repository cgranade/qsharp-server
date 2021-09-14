// Start all services required to implement the Q# server.
using Microsoft.Quantum.IQSharp.Jupyter;
using QSharpStream;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

var command = new RootCommand
{
    new Option<UseMode>(
        "--use",
        description: "Communication method to use to talk to host programs.",
        getDefaultValue: () => UseMode.StdInOut
    ),
    new Option<int>(
        "--port",
        description: "TCP/IP port to be used when communicating via sockets. No effect if --use tcpip is not set.",
        getDefaultValue: () => 8004
    ),
    new Option<string?>(
        "--log-file",
        description: "Path to write log info to.",
        getDefaultValue: () => null
    ),
    new Option<bool>(
        "--log-json",
        description: "Write JSON to the log file instead of plain text. No effect if --log-file is not set."
    )
};

command.Handler = CommandHandler.Create<Options>(async options =>
{
    var serviceProvider = BuildServices(options);
    var (reader, writer) = await OpenCommunications(options);

    // Build an event loop to consume input and write back to output.
    var eventLoop = serviceProvider.GetRequiredService<EventLoop>();
    eventLoop.InputStream = reader;
    eventLoop.OutputStream = writer;

    // Actually start the event loop, keeping a token we can use to cancel it.
    var cancellationTokenSource = new CancellationTokenSource();
    var loopTask = eventLoop.Run(cancellationTokenSource.Token);

    // Wait for the client to end input so that we can shut down.
    await loopTask;
});

async Task<(TextReader inStream, TextWriter outStream)> OpenCommunications(Options options)
{
    switch (options.Use)
    {
        case UseMode.StdInOut:
            return (Console.In, Console.Out);

        case UseMode.TcpIp:
            // Safe to use stdin/out when using TCP/IP.
            Console.Write($"Connecting via TCP/IP port {options.Port}...");
            var client = new TcpClient();
            await client.ConnectAsync("localhost", options.Port);
            Console.WriteLine(" done.");
            var stream = client.GetStream();
            return (new StreamReader(stream), new StreamWriter(stream));

        default:
            throw new ArgumentException($"Mode {options.Use} not supported.");
    }
}

IServiceProvider BuildServices(Options options) =>
    new CommandStreamBuilder
    {
        ConfigureServices = services => services
            .AddIQSharp()
            .AddLogging(loggingBuilder =>
            {
                if (options.LogFile != null)
                {
                    loggingBuilder.AddFile(
                        options.LogFile,
                        minimumLevel: LogLevel.Debug,
                        isJson: options.LogJson
                    );
                }
            }),

        ConfigureServiceProvider = provider => provider
            // We just need some nonstatic type in this assembly.
            .AddCommandsByAttribute<CompileCommand>()
            .AddMagicCommandsFromAssembly<AbstractMagic>()
    }
    .Build();

await command.InvokeAsync(args);


enum UseMode
{
    StdInOut,
    TcpIp
};

record Options(UseMode Use, int Port, string LogFile, bool LogJson);
