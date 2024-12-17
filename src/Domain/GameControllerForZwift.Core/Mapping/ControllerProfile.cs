namespace GameControllerForZwift.Core.Mapping
{
    public class ControllerProfile
    {
        public string Name { get; set; }
        public List<InputMapping> Mappings { get; set; }

        public ControllerProfile()
        {
            Name = string.Empty;
            Mappings = new List<InputMapping>();

            // Initialize Mappings with default values for each ControllerInput
            foreach (ControllerInput input in Enum.GetValues(typeof(ControllerInput)))
            {
                Mappings.Add(new InputMapping(input, ZwiftFunction.None, ZwiftPlayerView.Default, null));
            }
        }

        public void UpdateMapping(ControllerInput input, ZwiftFunction function, ZwiftPlayerView? playerView = null, ZwiftRiderAction? riderAction = null)
        {
            var mapping = Mappings.FirstOrDefault(m => m.Input == input);
            if (mapping != null)
            {
                mapping.Function = function;
                mapping.PlayerView = playerView ?? ZwiftPlayerView.Default;
                mapping.RiderAction = riderAction ?? ZwiftRiderAction.RideOn;
            }
            else
            {
                throw new ArgumentException("Invalid ControllerInput");
            }
        }
    }
}
