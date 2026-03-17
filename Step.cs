using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rotronic
{
    public class StepClass
    {
        public string Step { get; set; }
        public double SetPointRH { get; set; }
        public double SetPointTemp { get; set; }
        public string SoakTime { get; set; }
        public double Accuracy { get; set; }
        public bool Adjust { get; set; }
    }
}
