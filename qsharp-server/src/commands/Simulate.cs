using System.Diagnostics.CodeAnalysis;
using Microsoft.Quantum.Experimental;
using Microsoft.Quantum.IQSharp;
using Microsoft.Quantum.Simulation.Common;
using Microsoft.Quantum.Simulation.Simulators;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using OneOf;

namespace QSharpStream;

public record SimulateInput(
    [property: JsonProperty("operation")] string OperationName,
    [property: JsonProperty("input")] JToken Input,
    [property: JsonProperty("simulator", NullValueHandling = NullValueHandling.Ignore)] string? Simulator = null
);

public record SimulateResult(
    [property: JsonProperty("output")] JToken Output
);

public class ChannelOutputConverter : JsonConverter<ChannelOutput>
{
    public override ChannelOutput? ReadJson(JsonReader reader, Type objectType,ChannelOutput? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override void WriteJson(JsonWriter writer,ChannelOutput? value, JsonSerializer serializer)
    {
        value?.Switch(
            consoleMessage => JToken.FromObject(consoleMessage).WriteTo(writer),
            diagnosticOutput => JToken.FromObject(diagnosticOutput).WriteTo(writer)
        );
    }
}

[Command("simulate")]
public class SimulateCommand : BaseCommand<SimulateInput, ChannelOutput, SimulateResult, Exception?>
{
    private readonly ISnippets Snippets;

    public SimulateCommand(ISnippets snippets)
    {
        Snippets = snippets;
    }

    private bool TryCreateSimulator(string simulatorName, [NotNullWhen(true)] out SimulatorBase? simulator, Action<ChannelOutput> output)
    {
        simulator = null;
        // Try a few well-known names.
        if (simulatorName == "QuantumSimulator")
        {
            simulator = new QuantumSimulator();
            // TODO: hook up diagnostic capturing for dump* calls
        }
        else if (simulatorName == "OpenSystemsSimulator")
        {
            simulator = new OpenSystemsSimulator(capacity: 2);
            // TODO: hook up noise model, allow setting nqubits
        }
        // TODO: try falling back to loading type by name

        if (simulator == null)
        {
            return false;
        }
        else
        {
            simulator.DisableLogToConsole();
            simulator.OnLog += msg => output(new ConsoleMessage(msg));
            simulator.OnDisplayableDiagnostic += diagnostic => output(new DisplayOutput(diagnostic));
            return true;
        }
    }

    private const string NoSuchSimulator = "no-such-simulator";
    private const string DefaultSimulator = "QuantumSimulator";

    public override async Task<Result<SimulateResult, Exception?>> Run(SimulateInput input, Action<ChannelOutput> output, CancellationToken cancellationToken)
    {
        try
        {
            var operation = Snippets.Operations.Where(
                op => op.FullName == input?.OperationName
            ).SingleOrDefault();

            if (operation == null)
            {
                return new Error<Exception?>(
                    Id: ErrorIds.NoSuchOperation,
                    Message: $"No such operation {input?.OperationName}.",
                    Details: null
                );
            }

            var runMethod = operation.RoslynType.GetMethod("Run");
            if (runMethod == null)
            {
                throw new Exception("Run method not found on generated C# type.");
            }

            var simulatorName = input?.Simulator ?? DefaultSimulator;
            if (!TryCreateSimulator(simulatorName, out var simulator, output))
            {
                return new Error<Exception?>(
                    Id: NoSuchSimulator,
                    Message: $"No simulator with name {simulatorName}.",
                    Details: null
                );
            }

            // Cheat and assume no inputs.
            // TODO: look at Input property.
            var result = runMethod.Invoke(null, new object[] { simulator });
            var response = await (dynamic)result!;
            return new Ok<SimulateResult>(new SimulateResult(
                Output: JToken.FromObject(response)
            ));
        }
        catch (Exception ex)
        {
            return new Error<Exception?>(
                Id: ErrorIds.UnknownException,
                Message: ex.Message,
                Details: ex
            );
        }
    }
}
