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
    public partial class ChamberControllerFrm : Form
    {
        private readonly Chamber _chamber;

        public ChamberControllerFrm(Chamber chamber)
        {
            InitializeComponent();

            _chamber = chamber ?? throw new ArgumentNullException(nameof(chamber));

            // Populate UI from chamber state
            checkBoxHumControl.Checked = _chamber.HumControl;
            checkBoxTempControl.Checked = _chamber.TempControl;
            textBoxHum.Text = _chamber.HumiditySP.ToString();
            textBoxTemp.Text = _chamber.TemperatureSP.ToString();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            double chamberTempSP = double.TryParse(textBoxTemp.Text, out var tempSP) ? tempSP : 23;
            double chamberHumSP = double.TryParse(textBoxHum.Text, out var humSP) ? humSP : 35;
            ChamberCommands.SetRHControl(_chamber, checkBoxHumControl.Checked);
            ChamberCommands.SetTempControl(_chamber, checkBoxTempControl.Checked);
            ChamberCommands.SetRHSP(_chamber, chamberHumSP);
            ChamberCommands.SetTempSP(_chamber, chamberTempSP);
            this.Close();
        }
        
        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
