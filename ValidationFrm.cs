using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Rotronic
{
    public partial class ValidationFrm : Form
    {
        string idn = "473";
        double mirrorTemp = 30.000;
        double humidity = 15.000;
        double dewPoint = 1.234;
        private int fakeMirrorCounter = 1;
        private string lastFakeSerial = "27-000000";


        public ValidationFrm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Generate a unique serial for each new fake mirror so multiple can be added
            var serial = $"27-{fakeMirrorCounter:D6}";
            lastFakeSerial = serial;

            Program.AddFakeMirror(
                idn: idn,
                mirrorTemp: mirrorTemp,
                humidity: humidity,
                dewPoint: dewPoint,
                serialNumber: serial,
                stable: true);
            fakeMirrorCounter++;
        }

        private void buttonUpdate_Click(object sender, EventArgs e)
        {
            mirrorTemp = textBoxMirrorTemp.Text != "" ? Convert.ToDouble(textBoxMirrorTemp.Text) : 23.000;
            humidity = textBoxMirrorHumdity.Text != "" ? Convert.ToDouble(textBoxMirrorHumdity.Text) : 10.000;
            dewPoint = textBoxMirrorDewPoint.Text != "" ? Convert.ToDouble(textBoxMirrorDewPoint.Text) : 1.234;

            // Apply updated values to all fake mirrors
            Program.UpdateAllFakeMirrors(mirrorTemp: mirrorTemp, humidity: humidity, dewPoint: dewPoint, idn: idn, stable: true);
        }
        //int ComPortHelper = 1000;
        //string ComPort = "COM";
        //    string ProbeType
        //    int HumidityCount
        //    double HumdityRaw
        //    double HumidityUserCorrection
        //    double HumidityTemperatureCorrection
        //    double HumidityDriftCorrection
        //    string HumidityUnit
        //    bool HumidityAlarm
        //    char HumidityTrend
        //    double Temperature
        //    int TemperatureCount
        //    double Resistance
        //    double PT100CoeffA
        //    double PT100CoeffB
        //    double PT100CoeffC
        //    double TempOffset
        //    double TempConversion
        //    string TemperatureUnit
        //    bool TemperatureAlarm
        //    char TemperatureTrend
        //    string CalculatedParameter
        //    double CalculatedValue
        //    string CalculatedUnit
        //    bool CalculatedAlarm
        //    char CalculatedTrend
        //    string DeviceModel
        //    string FirmwareVersion
        //    string SerialNumber
        //    string DeviceName
        //    string AlarmByte
        //    char DeviceType
        //    string ProbeAddress
        //    bool CelsiusHelper
        int fakeComPort = 1000;
        private void buttonAddProbe_Click(object sender, EventArgs e)
        {
            Program.AddFakeProbe(
                ComPort: "COM" + (fakeComPort).ToString(),
                DeviceName: "FakeProbe " + fakeComPort.ToString()
            );
            fakeComPort++;
        }
    }
}