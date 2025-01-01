using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameControllerForZwift.Network
{
    public class Bike
    {
        private double lastRawRequestedResistanceValue;
        private double requestResistance;
        private double lastRawRequestedInclinationValue;
        private double requestInclination;
        private double m_difficult;
        private bool autoResistanceEnable;
        private double RequestedResistance;
        private double RequestedCadence;
        private double Cadence;
        private int RequestedPelotonResistance;
        private int RequestedPower;

        public event EventHandler<double> ResistanceChanged;
        public event EventHandler<(double, double)> InclinationChanged;

        public Bike()
        {
            // Initialize the elapsed metric type
            // elapsed.setType(metric::METRIC_ELAPSED); // Implement this if needed
        }

        public VirtualBike VirtualBike()
        {
            return this as VirtualBike;
        }

        public void ChangeResistance(double resistance)
        {
            var configuration = new ConfigurationBuilder()
                //.AddJsonFile("appsettings.json")
                //.Build();
                ;

            //double zwiftErgResistanceUp = configuration.GetValue<double>("ZwiftErgResistanceUp");
            //double zwiftErgResistanceDown = configuration.GetValue<double>("ZwiftErgResistanceDown");

            double zwiftErgResistanceUp = 0;
            double zwiftErgResistanceDown = 0;

            Debug.WriteLine($"Bike::ChangeResistance {autoResistanceEnable} {resistance}");

            lastRawRequestedResistanceValue = resistance;
            if (autoResistanceEnable)
            {
                double v = (resistance * m_difficult) + Gears();
                if (v > zwiftErgResistanceUp)
                {
                    Debug.WriteLine("zwift_erg_resistance_up filter enabled!");
                    v = zwiftErgResistanceUp;
                }
                else if (v < zwiftErgResistanceDown)
                {
                    Debug.WriteLine("zwift_erg_resistance_down filter enabled!");
                    v = zwiftErgResistanceDown;
                }
                requestResistance = v;
                ResistanceChanged?.Invoke(this, requestResistance);
            }
            RequestedResistance = resistance * m_difficult + Gears();
        }

        public void ChangeInclination(double grade, double percentage)
        {
            Debug.WriteLine($"Bike::ChangeInclination {autoResistanceEnable} {grade} {percentage}");
            lastRawRequestedInclinationValue = grade;
            if (autoResistanceEnable)
            {
                requestInclination = grade;
            }
            InclinationChanged?.Invoke(this, (grade, percentage));
        }

        public uint PowerFromResistanceRequest(double requestResistance)
        {
            // This bike has resistance level to N.m so the formula is Power (kW) = Torque (N.m) x Speed (RPM) / 9.5488
            double cadence = RequestedCadence > 0 ? RequestedCadence : Cadence;
            return (uint)((requestResistance * cadence) / 9.5488);
        }

        public void ChangeRequestedPelotonResistance(int resistance)
        {
            RequestedPelotonResistance = resistance;
        }

        public void ChangeCadence(int cadence)
        {
            RequestedCadence = cadence;
        }

        public void ChangePower(int power)
        {
            RequestedPower = power; // In order to paint in any case the request power on the charts
        }

        private double Gears()
        {
            // Implement the logic to get the current gear value
            return 0;
        }
    }
}
