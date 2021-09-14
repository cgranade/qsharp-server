using Microsoft.Extensions.DependencyInjection;

namespace CommandStream;

public record CommandStreamBuilder
{
    public Func<IServiceCollection, IServiceCollection>? ConfigureServices { get; init; } = null;
    public Func<IServiceProvider, IServiceProvider>? ConfigureServiceProvider { get; init; } = null;

    public IServiceProvider Build()
    {
        IServiceCollection serviceCollection = new ServiceCollection()
                .AddSingleton<ICommandRouter, CommandRouter>()
                // FIXME: Separate into IEventLoop.
                .AddSingleton<EventLoop>();
        serviceCollection = ConfigureServices?.Invoke(serviceCollection) ?? serviceCollection;

        IServiceProvider provider = serviceCollection.BuildServiceProvider();
        provider = provider.AddCommandsByAttribute<CommandStreamBuilder>();
        provider = ConfigureServiceProvider?.Invoke(provider) ?? provider;

        return provider;
    }
}
