namespace GameControllerForZwift.Core.Mapping
{
    public interface IControllerProfileService
    {
        ControllerProfiles Profiles { get; }

        public ControllerProfile GetDefaultProfile();

        void AddProfile(ControllerProfile profile);

        ControllerProfile GetProfile(string profileName);

        void RemoveProfile(string profileName);
    }
}
