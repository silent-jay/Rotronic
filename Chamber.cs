using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rotronic
{
    internal class Chamber
    {
        public string IPAddress { get; set; }
        public double Temperature { get; set; }
        public double TemperatureReference { get; set; }
        public bool TempControl { get; set; }
        public double TemperatureSP { get; set; }
        public bool TempStable { get; set; }
        public double Humidity { get; set; }
        public double HumidityReference { get; set; }
        public bool HumControl { get; set; }
        public double HumiditySP { get; set; }
        public bool HumStable { get; set; }
        public double DessicentLevel { get; set; }
        public double WaterLevel { get; set; }
        public string HC2Serial { get; set; }
        public string DessicantSerial { get; set; }
        public string Version { get; set; }
        public string ControllerSerial { get; set; }
        public string Name { get; set; }
        public bool CorrApplied { get; set; }
        public bool Calculation {  get; set; }
        public string ExtRefSerial { get; set; }
        public double ExtRefTemp { get; set; }
        public double ExtRefDP { get; set; }
        public double ExtRefDPCorr { get; set; }
        public double ExtRefFP { get; set; }
        public double ExtRefRH { get; set; }
        public bool ExtRefControl { get; set; }
        public bool ExtRefStable { get; set; }
        public string Warning { get; set; }
        public bool ProgramRunning { get; set; }
        public bool InUse { get; set; }

        public bool Selected { get; set; }
    }
}
