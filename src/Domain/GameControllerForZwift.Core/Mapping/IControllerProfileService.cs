namespace GameControllerForZwift.Core.Mapping
{
    public interface IControllerProfileService
    {
        ControllerProfiles Profiles { get; }

        ControllerProfile LoadDefaultProfile(string filePath);

        void AddProfile(string profileName, ControllerProfile profile);

        ControllerProfile GetProfile(string profileName);

        void RemoveProfile(string profileName);

        string ToJson();
    }
}
