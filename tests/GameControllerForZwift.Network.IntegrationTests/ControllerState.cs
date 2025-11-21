using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameControllerForZwift.Network.IntegrationTests
{
    /// <summary>
    /// Represents the state of a Zwift Play controller
    /// </summary>
    public class ControllerState
    {
        // Button states
        public bool ButtonA { get; set; }
        public bool ButtonB { get; set; }
        public bool ButtonY { get; set; }
        public bool ButtonZ { get; set; }
        public bool ArrowUp { get; set; }
        public bool ArrowDown { get; set; }
        public bool ArrowLeft { get; set; }
        public bool ArrowRight { get; set; }
        public bool ButtonLS { get; set; }
        public bool ButtonRS { get; set; }

        // Analog inputs (0-255)
        public byte AnalogLeft { get; set; }
        public byte AnalogRight { get; set; }

        // Current gear (1-24)
        public int CurrentGear { get; set; } = 12; // Default to middle gear

        /// <summary>
        /// Resets the controller state to defaults
        /// </summary>
        public void Reset()
        {
            ButtonA = false;
            ButtonB = false;
            ButtonY = false;
            ButtonZ = false;
            ArrowUp = false;
            ArrowDown = false;
            ArrowLeft = false;
            ArrowRight = false;
            ButtonLS = false;
            ButtonRS = false;
            AnalogLeft = 0;
            AnalogRight = 0;
            CurrentGear = 12;
        }

        /// <summary>
        /// Gets the byte representation of the current gear
        /// </summary>
        public byte[] GetGearBytes()
        {
            switch (CurrentGear)
            {
                case 1: return new byte[] { 0xCC, 0x3A };
                case 2: return new byte[] { 0xFC, 0x43 };
                case 3: return new byte[] { 0xAC, 0x4D };
                case 4: return new byte[] { 0xDC, 0x56 };
                case 5: return new byte[] { 0x8C, 0x60 };
                case 6: return new byte[] { 0xE8, 0x6B };
                case 7: return new byte[] { 0xC4, 0x77 };
                case 8: return new byte[] { 0xA0, 0x83, 0x01 };
                case 9: return new byte[] { 0xA8, 0x91, 0x01 };
                case 10: return new byte[] { 0xB0, 0x9F, 0x01 };
                case 11: return new byte[] { 0xB8, 0xAD, 0x01 };
                case 12: return new byte[] { 0xC0, 0xBB, 0x01 };
                case 13: return new byte[] { 0xF3, 0xCB, 0x01 };
                case 14: return new byte[] { 0xA8, 0xDC, 0x01 };
                case 15: return new byte[] { 0xDC, 0xEC, 0x01 };
                case 16: return new byte[] { 0x90, 0xFD, 0x01 };
                case 17: return new byte[] { 0xD4, 0x90, 0x02 };
                case 18: return new byte[] { 0x98, 0xA4, 0x02 };
                case 19: return new byte[] { 0xDC, 0xB7, 0x02 };
                case 20: return new byte[] { 0x9F, 0xCB, 0x02 };
                case 21: return new byte[] { 0xD8, 0xE2, 0x02 };
                case 22: return new byte[] { 0x90, 0xFA, 0x02 };
                case 23: return new byte[] { 0xC8, 0x91, 0x03 };
                case 24: return new byte[] { 0xF3, 0xAC, 0x03 };
                default: return new byte[] { 0xC0, 0xBB, 0x01 }; // Default to gear 12
            }
        }
    }
}
