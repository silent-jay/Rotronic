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
        private int fakeMirrorCounter = 100000;
        private const string FakeMirrorSerialPrefix = "27-";


        public ValidationFrm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Generate a unique serial for each new fake mirror so multiple can be added
            Program.AddFakeMirror(
                idn: idn,
                mirrorTemp: mirrorTemp,
                humidity: humidity,
                dewPoint: dewPoint,
                serialNumber: FakeMirrorSerialPrefix + fakeMirrorCounter.ToString(),
                stable: true);
            fakeMirrorCounter++;
        }

        private void buttonUpdate_Click(object sender, EventArgs e)
        {
            /*
             * This code needs to be refactored and updated.
             * new text boxes have been added to update values for fake chambers and fake probes as well
             * 
             */
            mirrorTemp = textBoxMirrorTemp.Text != "" ? Convert.ToDouble(textBoxMirrorTemp.Text) : 23.000;
            humidity = textBoxMirrorHumdity.Text != "" ? Convert.ToDouble(textBoxMirrorHumdity.Text) : 10.000;
            dewPoint = textBoxMirrorDewPoint.Text != "" ? Convert.ToDouble(textBoxMirrorDewPoint.Text) : 1.234;

            // Apply updated values to all fake mirrors
            Program.UpdateAllFakeMirrors(mirrorTemp: mirrorTemp, humidity: humidity, dewPoint: dewPoint, idn: idn, stable: true);

            // Fake probe updates
            double probeTemp = textBoxProbeTemp.Text != "" ? Convert.ToDouble(textBoxProbeTemp.Text) : 23.000;
            double probeHumidity = textBoxProbeHumidity.Text != "" ? Convert.ToDouble(textBoxProbeHumidity.Text) : 10.000;
            int probeTempCount = textBoxProbeTempCount.Text != "" ? Convert.ToInt32(textBoxProbeTempCount.Text) : 12345;
            double probeResistance = textBoxProbeRes.Text != "" ? Convert.ToDouble(textBoxProbeRes.Text) : 109.52;
            Program.UpdateAllFakeProbes(temperature: probeTemp, humidity: probeHumidity, temperatureCount: probeTempCount, resistance: probeResistance);

            // Fake chamber updates
            double chamberTemp = textBoxChTemp.Text != "" ? Convert.ToDouble(textBoxChTemp.Text) : 23.000;
            double chamberTempSp = textBoxChTempSP.Text != "" ? Convert.ToDouble(textBoxChTempSP.Text) : 23.000;
            double chamberHumidity = textBoxChHum.Text != "" ? Convert.ToDouble(textBoxChHum.Text) : 10.000;
            double chamberHumiditySp = textBoxChHumSP.Text != "" ? Convert.ToDouble(textBoxChHumSP.Text) : 10.000;
            Program.UpdateAllFakeChambers(temperature: chamberTemp, humidity: chamberHumidity, temperatureSp: chamberTempSp, humiditySp: chamberHumiditySp);
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
        int fakeChamberCounter = 1;
        private void buttonAddProbe_Click(object sender, EventArgs e)
        {
            Program.AddFakeProbe(
                ComPort: "COM" + (fakeComPort).ToString(),
                DeviceName: "FakeProbe " + fakeComPort.ToString(),
                SerialNumber: fakeComPort.ToString()
            );
            fakeComPort++;
        }

        private void buttonChamber_Click(object sender, EventArgs e)
        {
            if (fakeChamberCounter == 256)
                return;
            Program.AddFakeChamber(
                Name: "FakeChamber" + fakeChamberCounter.ToString(),
                IPAddress: "192.168.1." + fakeChamberCounter.ToString(),
                Temperature: 23.00,
                TempControl: true,
                TemperatureSP: 23.00,
                TempStable: true,
                Humidity: 10.00,
                HumiditySP: 10.00,
                HumStable: true,
                Version: "1.0",
                ControllerSerial: fakeChamberCounter.ToString(),
                HC2Serial: fakeChamberCounter.ToString(),
                DessicantSerial: fakeChamberCounter.ToString(),
                Warning: "Still not a real chamber, but at least it has a unique IP and serials! :)",
                ProgramRunning: false
                );
            fakeChamberCounter++;
        }
    }
}