using System.Text;

namespace GameControllerForZwift.Network
{
    public class ControllerNotification
    {
        private byte[] message;

        public ControllerNotification(byte[] message)
        {
            this.message = message;
        }

        public override string ToString()
        {
            return Encoding.UTF8.GetString(message);
        }

        public string Diff(ControllerNotification other)
        {
            // Implement the diff logic here
            return string.Empty;
        }
    }
}
