using Microsoft.Extensions.DependencyInjection;

namespace CommandStream;

public static class Extensions
{
    internal static OneOf<TObject?, JsonSerializationException> MaybeToObject<TObject>(this JToken token)
    {
        try
        {
            return token.ToObject<TObject>();
        }
        catch (JsonSerializationException ex)
        {
            return ex;
        }
    }

    internal static TAttribute? GetCustomAttribute<TAttribute>(this Type type)
    where TAttribute: class =>
        Attribute.GetCustomAttribute(type, typeof(TAttribute)) as TAttribute;

    internal static void AddContext(this JToken token, string? context)
    {
        if (context == null)
        {
            return;
        }
        (token as JObject)?.Last?.AddAfterSelf(
            new JProperty("context", context)
        );
    }

    public static IServiceProvider AddCommandsByAttribute<TAssembly>(this IServiceProvider serviceProvider)
    {
        var commandRouter = serviceProvider.GetRequiredService<ICommandRouter>();
        foreach (var type in typeof(TAssembly).Assembly.DefinedTypes)
        {
            if (
                type.GetCustomAttribute<CommandAttribute>() is CommandAttribute attribute
            )
            {
                var obj = ActivatorUtilities.CreateInstance(serviceProvider, type);
                if (obj is ICommand command)
                {
                    commandRouter.Add(
                        attribute.CommandName,
                        command
                    );
                }
                else
                {
                    throw new Exception($"Expected target of Command attribute to be an ICommand, but was {obj.GetType()}.");
                }
            }
        }
        return serviceProvider;
    }
}
