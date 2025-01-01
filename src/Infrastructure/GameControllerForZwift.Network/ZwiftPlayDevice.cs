using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameControllerForZwift.Network
{
    public class ZwiftPlayDevice
    {
        private int batteryLevel;
        private ControllerNotification lastButtonState;

        public void ProcessEncryptedData(byte[] bytes)
        {
            try
            {
                Debug.WriteLine("Decrypted: " + BitConverter.ToString(bytes).Replace("-", ""));

                byte[] counter = bytes[..sizeof(int)];
                byte[] payload = bytes[sizeof(int)..];

                byte[] data = Decrypt(counter, payload); // Implement the Decrypt method
                Debug.WriteLine(BitConverter.ToString(data).Replace("-", " ") + " " + BitConverter.ToString(counter).Replace("-", " ") + " " + BitConverter.ToString(payload).Replace("-", " "));
                char type = (char)data[0];
                byte[] message = data[1..];

                switch (type)
                {
                    case ZapConstants.CONTROLLER_NOTIFICATION_MESSAGE_TYPE:
                        ProcessButtonNotification(new ControllerNotification(message));
                        break;
                    case ZapConstants.EMPTY_MESSAGE_TYPE:
                        Debug.WriteLine("Empty Message");
                        break;
                    case ZapConstants.BATTERY_LEVEL_TYPE:
                        // BatteryStatus notification = new BatteryStatus(message);
                        // if (batteryLevel != notification.GetLevel())
                        // {
                        //     batteryLevel = notification.GetLevel();
                        //     Debug.WriteLine("Battery level update: " + batteryLevel);
                        // }
                        break;
                    case (char)0x37:
                        if (bytes[2] == 0x00)
                            OnPlus();
                        else if (bytes[4] == 0x00)
                            OnMinus();
                        break;
                    default:
                        Debug.WriteLine("Unprocessed - Type: " + (int)type + " Data: " + BitConverter.ToString(data).Replace("-", ""));
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Decrypt failed: " + ex.Message);
            }
        }

        private void ProcessButtonNotification(ControllerNotification notification)
        {
            if (lastButtonState == null)
            {
                Debug.WriteLine(notification.ToString());
            }
            else
            {
                string diff = notification.Diff(lastButtonState);
                if (!string.IsNullOrEmpty(diff))
                    Debug.WriteLine(diff);
            }
            lastButtonState = notification;
        }

        private void OnPlus()
        {
            // Emit plus event
        }

        private void OnMinus()
        {
            // Emit minus event
        }

        private byte[] Decrypt(byte[] counter, byte[] payload)
        {
            // Implement the decryption logic here
            return new byte[0];
        }
    }
}
