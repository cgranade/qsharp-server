using Microsoft.Quantum.IQSharp.Common;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace QSharpStream;

public record CompileInput(
    [property: JsonProperty("source")] string Source
);

public record CompileResult(
    [property: JsonProperty("warnings")] IList<string> Warnings,
    [property: JsonProperty("compiled_callables")] IList<string> CompiledCallables
);

[Command("compile")]
public class CompileCommand : BaseCommand<CompileInput, object, CompileResult, IList<Diagnostic>>
{
    private readonly IWorkspace Workspace;
    private readonly ISnippets Snippets;

    public CompileCommand(IWorkspace workspace, ISnippets snippets)
    {
        Workspace = workspace;
        Snippets = snippets;
    }

    public override async Task<Result<CompileResult, IList<Diagnostic>>> Run(CompileInput input, Action<object> output, CancellationToken cancellationToken)
    {
        await Workspace.Initialization;
        var code = input.Source;
        try
        {
            var snippet = Snippets.Compile(code);
            return new Ok<CompileResult>(new CompileResult(
                Warnings: snippet.warnings,
                CompiledCallables: snippet
                    .Elements
                    .Select(element => element
                        .ToFullName()
                        .WithoutNamespace(Microsoft.Quantum.IQSharp.Snippets.SNIPPETS_NAMESPACE)
                    )
                    .ToList()
            ));
        }
        catch (CompilationErrorsException ex)
        {
            return new Error<IList<Diagnostic>>(
                Id: ErrorIds.CompilationError,
                Message: ex.Message,
                Details: ex.Diagnostics.ToList()
            );
        }
    }
}
