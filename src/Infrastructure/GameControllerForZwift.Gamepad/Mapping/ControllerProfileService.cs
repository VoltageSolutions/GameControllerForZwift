using GameControllerForZwift.Core.Mapping;
using GameControllerForZwift.Gamepad.FileSystem;
using System.Text.Json;

namespace GameControllerForZwift.Gamepad.Mapping
{
    public class ControllerProfileService : IControllerProfileService
    {
        #region Fields
        private ControllerProfiles _profiles = new();
        private const string DefaultProfileName = "Default Profile";
        // todo - accept file service instead of doing string work itself
        //load the default on-constructor? or check for user app profile?
        // todo - means to save/load custom profiles
        
        public ControllerProfiles Profiles => _profiles;
        #endregion

        #region Constructor

        public ControllerProfileService(string jsonContent)
        {
            _profiles = LoadControllerProfiles(jsonContent);
        }

        public ControllerProfileService(IFileService fileService, string filePath) : this(fileService.ReadFileContent(filePath)) { }
        #endregion

        #region Methods

        public ControllerProfiles LoadControllerProfiles(string jsonContent)
        {
            if (string.IsNullOrEmpty(jsonContent))
                return new ControllerProfiles();
            else 
                return JsonSerializer.Deserialize<ControllerProfiles>(jsonContent);
        }
        public ControllerProfile GetDefaultProfile()
        {
            return GetProfile(DefaultProfileName);
        }

        public void AddProfile(ControllerProfile profile)
        {
            if (_profiles.Profiles.Any(p => p.Name == profile.Name))
            {
                throw new ArgumentException($"A profile named '{profile.Name}' already exists.");
            }

            _profiles.Profiles.Add(profile);
        }

        public ControllerProfile GetProfile(string profileName)
        {
            return _profiles.Profiles.Where(p => p.Name == profileName).FirstOrDefault();
        }

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
        #endregion
    }

}
