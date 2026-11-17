using GameControllerForZwift.Core.Controller;

namespace GameControllerForZwift.Core.Configurations
{
    /// <summary>
    /// A saved configuration: a named set of mappings for one controller model.
    /// </summary>
    /// <param name="Id">Identifies the configuration across renames and refreshes (CFG-008).</param>
    /// <param name="Name">The name the user gave it.</param>
    /// <param name="Controller">The controller model it belongs to (CFG-011).</param>
    /// <param name="Mappings">The function for every <see cref="ControllerInput"/>; missing inputs are <see cref="ZwiftFunction.None"/> (CFG-010).</param>
    /// <param name="Modified">When it was last saved.</param>
    /// <param name="FilePath">The file it was loaded from or saved to, or null if it has not been saved yet. Not stored in the file (CFG-003).</param>
    public sealed record SavedConfiguration(
        Guid Id,
        string Name,
        ControllerModel Controller,
        IReadOnlyDictionary<ControllerInput, ZwiftFunction> Mappings,
        DateTimeOffset Modified,
        string? FilePath = null);
}
