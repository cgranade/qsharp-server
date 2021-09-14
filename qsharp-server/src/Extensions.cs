using Microsoft.Extensions.Logging;
using Microsoft.Quantum.IQSharp.Jupyter;

namespace QSharpStream;

internal static class Extensions
{
    public static IServiceProvider AddMagicCommandsFromAssembly<TAssembly>(this IServiceProvider serviceProvider)
    {
        var router = serviceProvider.GetRequiredService<ICommandRouter>();
        foreach (var type in typeof(TAssembly).Assembly.DefinedTypes)
        {
            if (type.IsAssignableFrom(typeof(AbstractMagic)) && !type.IsAbstract && !type.IsInterface)
            {
                var magicCommand = (AbstractMagic)ActivatorUtilities.CreateInstance(serviceProvider, type);
                System.Console.Error.WriteLine("Creating magic command {Name} from {Type}.", magicCommand.Name, type);
                router.Add(magicCommand.Name, new MagicCommand(magicCommand));
            }
        }
        return serviceProvider;
    }
}
