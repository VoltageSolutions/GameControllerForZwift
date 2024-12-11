using System.Text.Json;

namespace GameControllerForZwift.Core.Mapping
{
    public class ControllerProfileService : IControllerProfileService
    {
        private ControllerProfiles _profiles = new();
        private const string DefaultProfileName = "Default Profile";
        // todo - accept file service instead of doing string work itself
        //load the default on-constructor? or check for user app profile?
        // todo - means to save/load custom profiles
        public ControllerProfiles Profiles => _profiles;

        public ControllerProfile LoadDefaultProfile(string filePath)
        {
            return LoadProfile(filePath, DefaultProfileName);
        }

        public ControllerProfile LoadProfile(string filePath, string profileName)
        {
            var json = File.ReadAllText(filePath);

            var profiles = JsonSerializer.Deserialize<ControllerProfiles>(json);

            return profiles.Profiles.Where(p => p.Name == profileName).First();
        }


        public void AddProfile(string profileName, ControllerProfile profile)
        {
            if (_profiles.Profiles.Any(p => p.Name == profileName))
            {
                throw new ArgumentException($"A profile named '{profileName}' already exists.");
            }

            _profiles.Profiles.Add(profile);
        }

        public ControllerProfile GetProfile(string profileName)
        {
            return _profiles.Profiles.Where(p => p.Name == profileName).FirstOrDefault();        }

        public void RemoveProfile(string profileName)
        {
            var profileToRemove = _profiles.Profiles.Where(p => p.Name == profileName).FirstOrDefault();

            _profiles.Profiles.Remove(profileToRemove);
        }

        // Serialize all profiles to JSON
        public string ToJson()
        {
            return JsonSerializer.Serialize(_profiles, new JsonSerializerOptions { WriteIndented = true });
        }
    }

}
