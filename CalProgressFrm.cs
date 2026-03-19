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
        private readonly Mirror _selectedMirror;
        private readonly List<StepClass> _steps;
        private readonly bool _manual;
        private readonly bool _advancedTemp;

        private readonly Timer _uiRefreshTimer = new Timer();

        private Timer _soakTimer;
        private TimeSpan _soakRemaining = TimeSpan.Zero;


        public CalProgressFrm(List<ListViewItem> SelectedProbes, ListViewItem SelectedMirror, ListViewItem SelectedChamber, List<StepClass> Steps, bool Manual, bool AdvancedTemp)
        {
            InitializeComponent();
            this.FormClosed += CalProgressFrm_FormClosed;

            _selectedProbes = SelectedProbes ?? new List<ListViewItem>();
            _selectedMirrorItem = SelectedMirror;
            _selectedChamberItem = SelectedChamber;
            _selectedChamber = SelectedChamber?.Tag as Chamber;
            _selectedMirror = SelectedMirror?.Tag as Mirror;

            _steps = Steps ?? new List<StepClass>();
            _manual = Manual;
            _advancedTemp = AdvancedTemp;

            PopulateProbeComboBox();

            comboBoxRotProbe.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxRotProbe.IntegralHeight = true;
            comboBoxRotProbe.DropDownWidth = Math.Max(comboBoxRotProbe.Width, 450);

            textBoxTemp.ReadOnly = true;
            textBoxTemp.TabStop = false;
            textBoxHum.ReadOnly = true;
            textBoxHum.TabStop = false;
            textBoxTempSP.ReadOnly = true;
            textBoxTempSP.TabStop = false;
            textBoxHumSP.ReadOnly = true;
            textBoxHumSP.TabStop = false;

            _uiRefreshTimer.Interval = 500;
            _uiRefreshTimer.Tick += (s, e) => RefreshLiveReadings();
            _uiRefreshTimer.Start();


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

        private sealed class ProbeComboItem
        {
            public ProbeComboItem(RotProbe probe, string display)
            {
                Probe = probe;
                Display = display;
            }

            public RotProbe Probe { get; }
            public string Display { get; }

            public override string ToString()
            {
                return Display;
            }
        }

        private void PopulateProbeComboBox()
        {
            comboBoxRotProbe.BeginUpdate();
            try
            {
                comboBoxRotProbe.Items.Clear();

                foreach (var item in _selectedProbes)
                {
                    var probe = item?.Tag as RotProbe;
                    if (probe == null) continue;

                    var name = string.IsNullOrWhiteSpace(probe.DeviceName) ? "(Unnamed)" : probe.DeviceName;
                    var serial = string.IsNullOrWhiteSpace(probe.SerialNumber) ? "(No SN)" : probe.SerialNumber;
                    comboBoxRotProbe.Items.Add(new ProbeComboItem(probe, name + "  |  " + serial));
                }

                if (comboBoxRotProbe.Items.Count > 0)
                    comboBoxRotProbe.SelectedIndex = 0;
            }
            finally
            {
                comboBoxRotProbe.EndUpdate();
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            RefreshStabilityIndicators();
            RefreshLiveReadings();
        }

        public void RefreshStabilityIndicators()
        {
            if (IsDisposed) return;

            if (InvokeRequired)
            {
                BeginInvoke((Action)RefreshStabilityIndicators);
                return;
            }

            bool chamberStable = _selectedChamber != null && _selectedChamber.TempStable && _selectedChamber.HumStable;
            bool mirrorStable = _selectedMirror != null && _selectedMirror.Stable;

            panelChamberStable.BackColor = chamberStable ? Color.LimeGreen : Color.Red;
            panelMirrorStable.BackColor = mirrorStable ? Color.LimeGreen : Color.Red;

            panelChamberStable.Invalidate();
            panelMirrorStable.Invalidate();
        }

        public void RefreshLiveReadings()
        {
            if (IsDisposed) return;

            if (InvokeRequired)
            {
                BeginInvoke((Action)RefreshLiveReadings);
                return;
            }

            if (_selectedMirror != null)
            {
                textBoxTemp.Text = _selectedMirror.MirrorTemp.ToString("F2") + " °C";
                textBoxHum.Text = _selectedMirror.Humdity.ToString("F2") + " %RH";
            }

            if (_selectedChamber != null)
            {
                textBoxTempSP.Text = _selectedChamber.TemperatureSP.ToString("F2") + " °C";
                textBoxHumSP.Text = _selectedChamber.HumiditySP.ToString("F2") + " %RH";
            }
        }

        private static void PaintIndicatorCircle(object sender, PaintEventArgs e)
        {
            var panel = sender as Panel;
            if (panel == null) return;

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var rect = panel.ClientRectangle;
            rect.Inflate(-1, -1);

            using (var brush = new SolidBrush(panel.BackColor))
            {
                e.Graphics.FillEllipse(brush, rect);
            }

            using (var pen = new Pen(Color.Black, 1f))
            {
                e.Graphics.DrawEllipse(pen, rect);
            }
        }

        private void panelChamberStable_Paint(object sender, PaintEventArgs e)
        {
            PaintIndicatorCircle(sender, e);
        }

        private void panelMirrorStable_Paint(object sender, PaintEventArgs e)
        {
            PaintIndicatorCircle(sender, e);
        }

        private void CalProgressFrm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _uiRefreshTimer.Stop();
            _uiRefreshTimer.Dispose();

            SkipSoakTimer();

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
                    RefreshStabilityIndicators();
                    Application.DoEvents(); // Keep UI responsive
                }

                RefreshStabilityIndicators();
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

            /*
     Public members reference (auto-inserted)
     
        pseudo-code:
        bool completed = false;
        status false if at start of calibration, true at end of calibration.
        public void DataCollector(step, chamber, mirror, probe, _manual, optional bool completed)
        {
            if (!completed)
            {
                //record all Static Data, all dynamic data that does not frequently change
                //helper data to add: time stamp for start of calibration
            }
             else if (completed)
             {
                // record all infrequently changing dynamic data - probe coefficients and corrections made will be captured here.
                //helper data to add: time stamp for end of calibration
             }
            else (if completed is not in method)
             {
                //record all dynamic data that should be recorded at each step, including chamber conditions, probe measurements, and mirror measurements.
                //add time stamp for each sample
             }
             if (!_manual)
             {
                //no data can be collected from the chamber. Assume chamber conditions are at the step set points. Record manual status.
             }
        Organize data and save to attached structured database. TBI
        }
            storage method tbd
            static data
              Probe:
                -trivial data:
                    ComPort, HumidityUnit(Assume %RH), HumidityAlarm, Humidity Trend, TemperatureAlarm, TemperatureTrend, TemperatureUnit (normalize all to °C,
                    make C stored value for every temperature, CalculatedParameter, CalculatedValue, CalculatedUnit, CalculatedAlarm, CalculatedTrend, AlarmByte, DeviceType,
                    ProbeAddress, CelsiusHelper, InUse, Selected
                -Important Static Data, but unlikely to ever change: ProbeType, DeviceModel, FirmwareVersion, SerialNumber, DeviceType, ProbeName
                -Important Dynamic Data, but does not frequently change. Record at start and end of calibration procedure, does not need to be recorded at each step: HumidityFactoryCorrection,
                   HumidityUserCorrection, HumidityTemperatureCorrection, HumidityDriftCorrection, PT100CoeffA, PT100CoeffB, PT100CoeffC, TempOffset, TempConversion.
                -Dynamic Data that should be recorded at each step: Humidity, HumidityCount, HumdityRaw, Temperature, TemperatureCount, Resistance
             Mirror:
                -trivial data: Most of mirror data is not relavent. We're primarily concerned with Humidity and External/Mirror temp.
                -Important Static Data: ID, IDN, SerialNumber
                -Important Dynamic Data, but does not frequently change: None
                -Dynamic Data that should be recorded at each step: DewPoint, FrostPoint, Humidity, ExternalTemp, MirrorTemp
            Chamber:
                -Trivial Data: IPAddress, TempControl, HumControl, TempStable, HumStable, DessicantLevel, DessicantSerial, WaterLevel, Calculation, Anything External Reference related, Warning, ProgramRunning, InUse, Selected
                -Important Static Data: Name, HC2Serial, ControllerSerial, Version
                -Important Dynamic Data with infrequent changes: None
                -Important Dynamic Data to record at each step: Temperature, TemperatureSP, Humidity, HumiditySP
    */
        public void SafeClose(Chamber chamber)
        {
            ChamberCommands.SetRHControl(chamber, false);
            ChamberCommands.SetTempControl(chamber, false);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "Start Calibration")
            {
                StartCalRoutine();
                StepClass test = new StepClass();
                test.SoakTime = "00:15";
                SoakTimer(test);
                button1.Text = "Skip Soak";
                return;
            }
            else if (button1.Text == "Skip Soak")
            {
                SkipSoakTimer();
                button1.Text = "Start Calibration";

                //skip soak logic
            }
        }

        public void SoakTimer(StepClass step)
        {
            if (step == null) return;

            var soakText = (step.SoakTime ?? string.Empty).Trim();
            if (!TryParseHourMinute(soakText, out var duration))
                duration = TimeSpan.Zero;

            _soakRemaining = duration;
            UpdateSoakTextBox();

            if (_soakRemaining == TimeSpan.Zero)
                return;

            if (_soakTimer == null)
            {
                _soakTimer = new Timer();
                _soakTimer.Interval = 1000;
                _soakTimer.Tick += SoakTimer_Tick;
            }

            _soakTimer.Start();
        }
        public void SkipSoakTimer()
        {
            if (InvokeRequired)
            {
                BeginInvoke((Action)SkipSoakTimer);
                return;
            }

            _soakRemaining = TimeSpan.Zero;
            UpdateSoakTextBox();

            if (_soakTimer != null)
            {
                _soakTimer.Stop();
                _soakTimer.Tick -= SoakTimer_Tick;
                _soakTimer.Dispose();
                _soakTimer = null;
            }
        }

        private void SoakTimer_Tick(object sender, EventArgs e)
        {
            if (_soakRemaining > TimeSpan.Zero)
                _soakRemaining = _soakRemaining.Subtract(TimeSpan.FromSeconds(1));

            if (_soakRemaining < TimeSpan.Zero)
                _soakRemaining = TimeSpan.Zero;

            UpdateSoakTextBox();

            if (_soakRemaining == TimeSpan.Zero)
                SkipSoakTimer();
        }

        private void UpdateSoakTextBox()
        {
            if (IsDisposed) return;

            if (InvokeRequired)
            {
                BeginInvoke((Action)UpdateSoakTextBox);
                return;
            }

            textBoxSoak.ReadOnly = true;
            textBoxSoak.TabStop = false;
            textBoxSoak.Text = string.Format("{0:D2}:{1:D2}:{2:D2}", (int)_soakRemaining.TotalHours, _soakRemaining.Minutes, _soakRemaining.Seconds);
        }

        private static bool TryParseHourMinute(string value, out TimeSpan duration)
        {
            duration = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(value)) return false;

            var parts = value.Split(':');
            if (parts.Length != 2) return false;

            if (!int.TryParse(parts[0], out var hours)) return false;
            if (!int.TryParse(parts[1], out var minutes)) return false;

            if (hours < 0 || minutes < 0 || minutes > 59) return false;
            duration = new TimeSpan(hours, minutes, 0);
            return true;
        }

        public void StartCalRoutine()
        {
            /*Foreach step in steps:
             * if step is Humidity and adjust is false, do humidity step
             * if step is humidity and adjuist is true , do humidity adjustment step
             * if step is temperature step and adjust is falso, do temperature step
             * if step is temperature step and adjust is true, do temperature adjustment step
             * if advanced temperature adjustment step, do advanced temperature adjustment step
             * 
             */
            return;
        }
    }
}
/*
 * 
     RotProbe (class: RotProbe)
      - string ComPort { get; set; }
      - string ProbeType { get; set; }
      - double Humidity { get; set; }
      - int HumidityCount { get; set; }
      - double HumdityRaw { get; set; }
      - double HumidityFactoryCorrection { get; set; }
      - double HumidityUserCorrection { get; set; }
      - double HumidityTemperatureCorrection { get; set; }
      - double HumidityDriftCorrection { get; set; }
      - string HumidityUnit { get; set; }
      - bool HumidityAlarm { get; set; }
      - char HumidityTrend { get; set; }
      - double Temperature { get; set; }
      - int TemperatureCount { get; set; }
      - double Resistance { get; set; }
      - double PT100CoeffA { get; set; }
      - double PT100CoeffB { get; set; }
      - double PT100CoeffC { get; set; }
      - double TempOffset { get; set; }
      - double TempConversion { get; set; }
      - string TemperatureUnit { get; set; }
      - bool TemperatureAlarm { get; set; }
      - char TemperatureTrend { get; set; }
      - string CalculatedParameter { get; set; }
      - double CalculatedValue { get; set; }
      - string CalculatedUnit { get; set; }
      - bool CalculatedAlarm { get; set; }
      - char CalculatedTrend { get; set; }
      - string DeviceModel { get; set; }
      - string FirmwareVersion { get; set; }
      - string SerialNumber { get; set; }
      - string DeviceName { get; set; }
      - string AlarmByte { get; set; }
      - char DeviceType { get; set; }
      - string ProbeAddress { get; set; }
      - bool CelsiusHelper { get; set; }
      - bool InUse { get; set; }
      - bool Selected { get; set; }


     Mirror (class: Mirror)
      - string ComPort { get; set; }
      - string SerialNumber { get; set; }
      - double DewPoint { get; set; }
      - double FrostPoint { get; set; }
      - double Humdity { get; set; }
      - double WMO { get; set; }
      - double VolumeRatio { get; set; }
      - double WeightRatio { get; set; }
      - double AbsoluteHumdity { get; set; }
      - double SpecificHumdity { get; set; }
      - double VaporPressure { get; set; }
      - double HeadPressure { get; set; }
      - double ExternalTemp { get; set; }
      - double MirrorTemp { get; set; }
      - double HeadTemp { get; set; }
      - double MirrorResistance { get; set; }
      - double ExternalResistance { get; set; }
      - string ID { get; set; }
      - string IDN { get; set; }
      - bool Stable { get; set; }
      - bool InUse { get; set; }
      - bool Selected { get; set; }

     Chamber (class: Chamber)
      - string IPAddress { get; set; }
      - double Temperature { get; set; }
      - double TemperatureReference { get; set; }
      - bool TempControl { get; set; }
      - double TemperatureSP { get; set; }
      - bool TempStable { get; set; }
      - double Humidity { get; set; }
      - double HumidityReference { get; set; }
      - bool HumControl { get; set; }
      - double HumiditySP { get; set; }
      - bool HumStable { get; set; }
      - double DessicentLevel { get; set; }
      - double WaterLevel { get; set; }
      - string HC2Serial { get; set; }
      - string DessicantSerial { get; set; }
      - string Version { get; set; }
      - string ControllerSerial { get; set; }
      - string Name { get; set; }
      - bool CorrApplied { get; set; }
      - bool Calculation { get; set; }
      - string ExtRefSerial { get; set; }
      - double ExtRefTemp { get; set; }
      - double ExtRefDP { get; set; }
      - double ExtRefDPCorr { get; set; }
      - double ExtRefFP { get; set; }
      - double ExtRefRH { get; set; }
      - bool ExtRefControl { get; set; }
      - bool ExtRefStable { get; set; }
      - string Warning { get; set; }
      - bool ProgramRunning { get; set; }
      - bool InUse { get; set; }
      - bool Selected { get; set; }
 */