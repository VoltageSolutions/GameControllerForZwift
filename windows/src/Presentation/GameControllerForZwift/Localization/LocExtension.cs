using System.Windows.Data;
using System.Windows.Markup;

namespace GameControllerForZwift.UI.WPF.Localization
{
    /// <summary>
    /// <c>{loc:Loc Key}</c> in XAML: a one-way binding to the string <see cref="Key"/> in Resources/Strings.resx.
    /// </summary>
    [MarkupExtensionReturnType(typeof(object))]
    public sealed class LocExtension : MarkupExtension
    {
        public LocExtension()
        {
        }

        public LocExtension(string key)
        {
            Key = key;
        }

        /// <summary>The key of the string in Resources/Strings.resx.</summary>
        [ConstructorArgument("key")]
        public string Key { get; set; } = string.Empty;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var binding = new Binding("[" + Key + "]")
            {
                Source = Localizer.Instance,
                Mode = BindingMode.OneWay,
            };
            return binding.ProvideValue(serviceProvider);
        }
    }
}
