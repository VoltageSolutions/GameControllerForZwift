using CommunityToolkit.Mvvm.ComponentModel;
using GameControllerForZwift.Core;
using GameControllerForZwift.Core.Configurations;
using GameControllerForZwift.Core.Controller;

namespace GameControllerForZwift.UI.WPF.ViewModels
{
    /// <summary>
    /// One entry in the configuration list: the built-in Default, a saved configuration for the selected controller model,
    /// or a saved configuration of another controller model (CFG-039).
    /// </summary>
    public partial class ConfigurationItem : ObservableObject
    {
        public ConfigurationItem(string name, IReadOnlyDictionary<ControllerInput, ZwiftFunction> mappings, SavedConfiguration? configuration,
            bool isFromOtherController = false)
        {
            Name = name;
            Mappings = mappings;
            Configuration = configuration;
            IsFromOtherController = isFromOtherController && configuration is not null;
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
        /// Gets a value indicating whether the configuration belongs to another controller model (CFG-039). Selecting it
        /// offers to save a copy for the selected controller (CFG-040).
        /// </summary>
        public bool IsFromOtherController { get; }

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
        /// Gets the name to show, with an unsaved-changes marker (CFG-021), for example "Climbing •". A configuration of another
        /// controller model names its controller, for example "Climbing (Xbox Series X Controller)" (CFG-039).
        /// </summary>
        public string DisplayName
        {
            get
            {
                var name = IsFromOtherController ? $"{Name} ({Configuration!.Controller.DisplayName})" : Name;
                return IsDirty ? name + " •" : name;
            }
        }

        public override string ToString() => DisplayName;
    }
}
