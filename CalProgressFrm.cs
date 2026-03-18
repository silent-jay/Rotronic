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
        private readonly List<ListViewItem> _selectedProbes;
        private readonly ListViewItem _selectedMirrorItem;
        private readonly ListViewItem _selectedChamberItem;
        private readonly Chamber _selectedChamber;
        private readonly List<StepClass> _steps;
        private readonly bool _manual;
        private readonly bool _advancedTemp;


        public CalProgressFrm(List<ListViewItem> SelectedProbes, ListViewItem SelectedMirror, ListViewItem SelectedChamber, List<StepClass> Steps, bool Manual, bool AdvancedTemp)
        {
            InitializeComponent();
            this.FormClosed += CalProgressFrm_FormClosed;

            _selectedProbes = SelectedProbes ?? new List<ListViewItem>();
            _selectedMirrorItem = SelectedMirror;
            _selectedChamberItem = SelectedChamber;
            _selectedChamber = SelectedChamber?.Tag as Chamber;

            _steps = Steps ?? new List<StepClass>();
            _manual = Manual;
            _advancedTemp = AdvancedTemp;


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
            if (_selectedChamber != null)
                SafeClose(_selectedChamber);
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
                ChamberCommands.SetRHSP(chamber, stepClass.SetPointRH);
                ChamberCommands.SetTempControl(chamber, true);
                ChamberCommands.SetRHControl(chamber, true);
                // Monitor stability and begin soak time countdown when stable
                while (!chamber.TempStable || !chamber.HumStable)
                {
                    //TODO: Add timeout in case chamber cannot reach conditions - 0°C and 5%RH are difficult conditions, need a "close enough" option
                    //TODO: UI element to show stability status and current chamber conditions
                    Application.DoEvents(); // Keep UI responsive
                }
            }
        }
        /*
        public void HumidityStep(args)
        {
            instructions for a humidity step go here.
        }

         public void TemperatureStep(args)
        {
            instructions for a temperature step go here.
        }

        public void AdvancedTemperatureStep(args)
        {
            advanced temperature adjustment should go here
        }

        public void HumidityAdjustmentStep(args)
        {
            instructions for a humidity adjustment step go here.
        }
        public void TemperatureAdjustmentStep(args)
        {
            instructions for a temperature adjustment step go here.
        }
        public void AdjustStep(args)
        {
        send adjust commmand to probe
        }

        Data to record during calibration
            Probe Temperature
            Probe Humidity
            Probe Resistance
            Probe Temperature Count
            Probe Humidity Count
            Chamber TempSP - recorded from chamber - static
            Chamber RHSP - recorded from chamber
            Chamber Temp - recorded from mirror - static
            Chamber RH - recorded from mirror
            Probe Name - static
            Probe SN - static
            Chamber Name - static
            Mirror SN -static
            TODO: list of Mirrors and Chambers and corresponding calibration information for traceability records and to ensure everything is in calibration at start of calibration procedure.
            
            Take 5 data points at each step at 15 second intervals.
            verify values are within acceptable range of chamber conditions, if not, flag step for review and possible repeat of step after troubleshooting chamber conditions.
            verify values don't indicate malfunction of any equipment.
            Save data at each step to database or excel file for record keeping. TODO: storage method.
            
        */
        public void SafeClose(Chamber chamber)
        {
            ChamberCommands.SetRHControl(chamber, false);
            ChamberCommands.SetTempControl(chamber, false);
        }
    }
}
