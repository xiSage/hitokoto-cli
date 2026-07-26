using System.Diagnostics.CodeAnalysis;
using hitokoto_cli.Commands.Config;
using hitokoto_cli.Settings;

namespace hitokoto_cli;

/// <summary>
/// Keeps settings-type property metadata alive under Native AOT trimming.
/// </summary>
/// <remarks>
/// Spectre.Console.Cli discovers command options/arguments by reflecting over
/// the settings type's public properties and their [CommandOption]/
/// [CommandArgument] attributes (CommandModelBuilder.GetPropertiesInOrder).
/// Under Native AOT, <c>preserve="All"</c> in the trimmer root descriptor
/// keeps the properties but not necessarily their custom-attribute metadata.
/// These <see cref="DynamicDependencyAttribute"/> annotations (applied to a
/// method that <c>Program</c> calls) tell the trimmer/AOT compiler to keep
/// the public properties of each settings type so Spectre's reflection-based
/// binding works at runtime.
/// </remarks>
internal static class AotPreservation
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(FetchSettings))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(ConfigSettings))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(ConfigGetSettings))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(ConfigSetSettings))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(ConfigUnsetSettings))]
    public static void EnsureSettingsTypesPreserved()
    {
        // No-op: the attributes alone drive trimmer/AOT behavior. Called from
        // Program to make the method (and thus its attributes) reachable.
    }
}
