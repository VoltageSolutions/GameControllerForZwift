using GameControllerForZwift.Core.Controller;
using GameControllerForZwift.Core.Mapping;

namespace GameControllerForZwift.Core.Configurations
{
    /// <summary>
    /// Helpers for the input-to-function maps that configurations hold.
    /// </summary>
    public static class ConfigurationMappings
    {
        /// <summary>
        /// Returns a map with an entry for every <see cref="ControllerInput"/>. Inputs that are not given map to
        /// <see cref="ZwiftFunction.None"/> (CFG-010). If an input is given more than once, the first one wins.
        /// </summary>
        public static IReadOnlyDictionary<ControllerInput, ZwiftFunction> Normalize(IEnumerable<KeyValuePair<ControllerInput, ZwiftFunction>> mappings)
        {
            ArgumentNullException.ThrowIfNull(mappings);
            var result = Enum.GetValues<ControllerInput>().ToDictionary(input => input, _ => ZwiftFunction.None);
            var seen = new HashSet<ControllerInput>();
            foreach (var (input, function) in mappings)
            {
                if (Enum.IsDefined(input) && seen.Add(input))
                    result[input] = Enum.IsDefined(function) ? function : ZwiftFunction.None;
            }
            return result;
        }

        /// <summary>
        /// Returns a complete map from a profile's mappings.
        /// </summary>
        public static IReadOnlyDictionary<ControllerInput, ZwiftFunction> FromProfile(ControllerProfile? profile)
        {
            var mappings = profile?.Mappings ?? [];
            return Normalize(mappings.Select(m => new KeyValuePair<ControllerInput, ZwiftFunction>(m.Input, m.Function)));
        }

        /// <summary>
        /// Returns true if both maps give the same function for every input.
        /// </summary>
        public static bool AreEqual(IReadOnlyDictionary<ControllerInput, ZwiftFunction> first, IReadOnlyDictionary<ControllerInput, ZwiftFunction> second)
        {
            ArgumentNullException.ThrowIfNull(first);
            ArgumentNullException.ThrowIfNull(second);
            foreach (var input in Enum.GetValues<ControllerInput>())
            {
                var a = first.TryGetValue(input, out var fa) ? fa : ZwiftFunction.None;
                var b = second.TryGetValue(input, out var fb) ? fb : ZwiftFunction.None;
                if (a != b)
                    return false;
            }
            return true;
        }
    }
}
