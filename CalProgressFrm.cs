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
    public partial class CalProgressFrm : Form
    {
        public CalProgressFrm(List<ListViewItem> SelectedProbes, ListViewItem SelectedMirror, ListViewItem SelectedChamber, List<StepClass> Steps, bool Manual, bool AdvancedTemp)
        {
            InitializeComponent();

            this.FormClosed += CalProgressFrm_FormClosed;

            /*
             * General plan for calibration procedures and UI/UX features to implement:
             * 
             * manual control: user will be prompted to manually set chamber conditions
             * automatic control: software will send set points to chamber, monitor conditions until stability is reported, then begin soak time countdown
             * 
             * Adjust - simply sends adjust command to probe
             * Temperature
             *      if adjust is selected, sets adjust temperature to reference value.
             *      if adjust is not selected, should only record data for reference and dut
             *          only one adjustment point allowed with this method, step editor should only allow one of these steps to be selected as the adjustment for temperature
             * Humidity
             *      If adjust is selected, saves the reference value to the probe. Will not write until an adjust step is reached.
             *      If not selected, just measure and record data.
             * Advanced Temperature Adjustment:
             *      Requires a minimum of 4 setpoints, close to min and max of chamber capability. Use the 4 points plus probes resistance to create new coefficients
             * 
             * 
             */
            
        }

        private void CalProgressFrm_FormClosed(object sender, FormClosedEventArgs e)
        {
            SafeClose();
        }
        public void ChamberController(Chamber chamber, bool Manual, StepClass stepClass)
        {
            if (Manual)
            {
                MessageBox.Show("Please set the chamber to the following conditions: " + stepClass.SetPointTemp + "°C and " + stepClass.SetPointRH + "%RH. Click OK when conditions are stable.");
            }
            else
            {
                ChamberCommands.SetTempSP(chamber, stepClass.SetPointTemp);
                ChamberCommands.SetHumSP(chamber, stepClass.SetPointRH);
                ChamberCommands.SetTempControl(chamber, true);
                ChamberCommands.SetRHControl(chamber, true);
                // Monitor stability and begin soak time countdown when stable
                while (!chamber.TempStable || !chamber.HumStable)
                {
                    //TODO: Add timeout in case chamber cannot reach conditions - 0°C and 5%RH are difficult conditions, need a "close enough" option
                    Application.DoEvents(); // Keep UI responsive
                }
            }
        }
        public void SafeClose()
        {
            ChamberCommands.SetRHControl(SelectedChamber, false);
            ChamberCommands.SetTempControl(SelectedChamber, false);
        }
    }
}
