using System.Text.Json;

namespace GameControllerForZwift.Core.Mapping
{
    public class ControllerProfileManager
    {
        private readonly Dictionary<string, ControllerProfile> _profiles = new();

        public IReadOnlyDictionary<string, ControllerProfile> Profiles => _profiles;

        public ControllerProfile LoadProfile(string filePath)
        {
            var json = File.ReadAllText(filePath);

            // Deserialize the JSON into a dictionary
            var profileDict = JsonSerializer.Deserialize<Dictionary<string, ControllerProfile>>(json);

            if (profileDict == null || profileDict.Count == 0)
            {
                throw new InvalidOperationException("The profile data is invalid or empty.");
            }

            // Return the first profile entry in the dictionary
            return profileDict.Values.First();
        }


        public void AddProfile(string profileName, ControllerProfile profile)
        {
            if (_profiles.ContainsKey(profileName))
            {
                throw new ArgumentException($"A profile named '{profileName}' already exists.");
            }

            _profiles[profileName] = profile;
        }

        public ControllerProfile GetProfile(string profileName)
        {
            if (_profiles.TryGetValue(profileName, out var profile))
            {
                return profile;
            }
            throw new KeyNotFoundException($"Profile '{profileName}' not found.");
        }

        public void RemoveProfile(string profileName)
        {
            if (!_profiles.Remove(profileName))
            {
                throw new KeyNotFoundException($"Profile '{profileName}' not found.");
            }
        }

        // Serialize all profiles to JSON
        public string ToJson()
        {
            return JsonSerializer.Serialize(_profiles, new JsonSerializerOptions { WriteIndented = true });
        }

        // Deserialize JSON into profiles
        public static ControllerProfileManager FromJson(string json)
        {
            var profiles = JsonSerializer.Deserialize<Dictionary<string, ControllerProfile>>(json);
            var manager = new ControllerProfileManager();
            if (profiles != null)
            {
                foreach (var kvp in profiles)
                {
                    manager.AddProfile(kvp.Key, kvp.Value);
                }
            }
            return manager;
        }
    }

}
