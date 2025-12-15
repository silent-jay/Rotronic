using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rotronic
{
    public class StepClass
    {
        public string Steps { get; set; }
        public double HumiditySetPoint { get; set; }
        public double TemperatureSetPoint { get; set; }
        public string SoakTime { get; set; }
        public bool EvalTemp {  get; set; }
        public double MinTemperature { get; set; }
        public double MaxTemperature { get; set; }
        public bool EvalHumidity { get; set; }
        public double MinHumidity { get; set; }
        public double MaxHumidity { get; set ; }
    }
}
