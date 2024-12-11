namespace GameControllerForZwift.Core.Mapping
{
    public class ControllerProfile
    {
        private readonly Dictionary<ControllerInput, InputMapping> _mappings = new();

        public IReadOnlyDictionary<ControllerInput, InputMapping> Mappings => _mappings;

        public ControllerProfile()
        {
            foreach (ControllerInput input in Enum.GetValues(typeof(ControllerInput)))
            {
                _mappings[input] = new InputMapping(input, ZwiftFunction.None);
            }
        }

        public void UpdateMapping(ControllerInput input, ZwiftFunction function, ZwiftPlayerView? playerView = null, ZwiftRiderAction? riderAction = null)
        {
            if (_mappings.ContainsKey(input))
            {
                _mappings[input] = new InputMapping(input, function, playerView, riderAction);
            }
            else
            {
                throw new ArgumentException("Invalid ControllerInput");
            }
        }
    }

}
