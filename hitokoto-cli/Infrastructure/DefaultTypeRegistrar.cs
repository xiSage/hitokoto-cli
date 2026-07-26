using System.Diagnostics.CodeAnalysis;
using Spectre.Console.Cli;

namespace hitokoto_cli.Infrastructure;

/// <summary>
/// Minimal <see cref="ITypeRegistrar"/>/<see cref="ITypeResolver"/> for Spectre.Console.Cli.
///
/// Spectre 0.50 resolves both its internal services AND command types through the
/// registrar. Internal services are registered via <see cref="Register(Type, Type)"/>
/// (instantiated with <see cref="Activator"/>, parameterless). Our own services and
/// command types are registered via <see cref="RegisterInstance(Type, object)"/> /
/// <see cref="RegisterLazy(Type, Func{object})"/>, which "pin" the entry so a later
/// <see cref="Register(Type, Type)"/> call cannot overwrite it.
///
/// <see cref="Resolve(Type?)"/> also synthesizes <see cref="IEnumerable{T}"/> from a
/// single registration of <c>T</c> (requested for <c>IHelpProvider</c>).
/// </summary>
internal sealed class DefaultTypeRegistrar : ITypeRegistrar, ITypeResolver
{
    private readonly Dictionary<Type, Func<object>> _factories = [];
    private readonly HashSet<Type> _pinned = [];

    // Register(Type, Type) is called only by Spectre.Console.Cli for its own
    // internal service types (e.g. IConfigurator). Those types live in
    // Spectre.Console(.Cli), which TrimmerRoots.xml preserves wholesale, so
    // their parameterless constructors survive trimming and Activator can
    // instantiate them at runtime.
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067",
        Justification = "Concrete types are Spectre internals preserved wholesale via TrimmerRoots.xml.")]
    public void Register(Type service, Type concrete)
    {
        // Don't overwrite an explicitly-pinned registration.
        if (_pinned.Contains(service))
        {
            return;
        }
        _factories[service] = () => Activator.CreateInstance(concrete)!;
    }

    public void RegisterInstance(Type service, object instance)
    {
        _factories[service] = () => instance;
        _pinned.Add(service);
    }

    public void RegisterLazy(Type service, Func<object> factory)
    {
        _factories[service] = factory;
        _pinned.Add(service);
    }

    public ITypeResolver Build() => this;

    // Resolve synthesizes IEnumerable<T> (e.g. IEnumerable<IHelpProvider>) from
    // a single registration of T. Array.CreateInstance needs dynamic code in
    // general, but the element types requested here are Spectre service
    // interfaces preserved wholesale via TrimmerRoots.xml, so their array
    // shapes are available to the AOT compiler at runtime.
    [UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "Element types are Spectre service interfaces preserved wholesale via TrimmerRoots.xml; array shapes are available at runtime.")]
    public object? Resolve(Type? type)
    {
        if (type is null)
        {
            return null;
        }

        if (_factories.TryGetValue(type, out var factory))
        {
            return factory();
        }

        // Synthesize IEnumerable<T> from a single registration of T.
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            var elemType = type.GetGenericArguments()[0];
            if (_factories.TryGetValue(elemType, out var elemFactory))
            {
                var array = Array.CreateInstance(elemType, 1);
                array.SetValue(elemFactory(), 0);
                return array;
            }
            return Array.CreateInstance(elemType, 0);
        }

        return null;
    }
}
