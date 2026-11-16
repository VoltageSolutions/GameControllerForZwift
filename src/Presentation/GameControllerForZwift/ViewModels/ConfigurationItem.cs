using CommunityToolkit.Mvvm.ComponentModel;
using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Configurations;
using GameControllerForZwift.Core.Controller;

namespace GameControllerForZwift.UI.WPF.ViewModels
{
    /// <summary>
    /// One entry in the configuration list: the built-in Default or a saved configuration.
    /// </summary>
    public partial class ConfigurationItem : ObservableObject
    {
        public ConfigurationItem(string name, IReadOnlyDictionary<ControllerInput, ZwiftFunction> mappings, SavedConfiguration? configuration)
        {
            Name = name;
            Mappings = mappings;
            Configuration = configuration;
        }

        /// <summary>
        /// Gets the configuration id, or <see cref="Guid.Empty"/> for the Default.
        /// </summary>
        public Guid Id => Configuration?.Id ?? Guid.Empty;

        public string Name { get; }

        /// <summary>
        /// Gets a value indicating whether this is the read-only built-in Default (CFG-020).
        /// </summary>
        public bool IsDefault => Configuration is null;

        /// <summary>
        /// Gets the mappings as saved, used to detect unsaved changes (CFG-021).
        /// </summary>
        public IReadOnlyDictionary<ControllerInput, ZwiftFunction> Mappings { get; }

        /// <summary>
        /// Gets the saved configuration, or null for the Default.
        /// </summary>
        public SavedConfiguration? Configuration { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the current mappings differ from this configuration as saved.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayName))]
        private bool _isDirty;

        /// <summary>
        /// Gets the name to show, with an unsaved-changes marker (CFG-021), for example "Climbing •".
        /// </summary>
        public string DisplayName => IsDirty ? Name + " •" : Name;

        public override string ToString() => DisplayName;
    }
}
