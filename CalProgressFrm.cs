using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Rotronic
{


    public partial class CalProgressFrm : Form
    {
        private readonly List<RotProbe> _selectedProbes;
        private readonly Mirror _selectedMirror;
        private readonly Chamber _selectedChamber;
        private readonly List<StepClass> _steps;
        private readonly bool _manual;
        private readonly bool _advancedTemp;

        private readonly Timer _uiRefreshTimer = new Timer();

        private Timer _soakTimer;
        private TimeSpan _soakRemaining = TimeSpan.Zero;
        private volatile bool _soakSkipRequested = false;


        public CalProgressFrm(List<RotProbe> selectedProbes, Mirror selectedMirror, Chamber selectedChamber, List<StepClass> steps, bool manual, bool advancedTemp)
        {
            InitializeComponent();
            this.FormClosed += CalProgressFrm_FormClosed;

            _selectedProbes = selectedProbes ?? new List<RotProbe>();
            _selectedMirror = selectedMirror;
            _selectedChamber = selectedChamber;

            _steps = steps ?? new List<StepClass>();
            _manual = manual;
            _advancedTemp = advancedTemp;

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

                foreach (var probe in _selectedProbes)
                {
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

            // NOTE: Units displayed as °C and %RH to normalize UI.
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
        public void DataRecorder(RotProbe probe, Mirror mirror, Chamber chamber, StepClass step, string calibrationID)
        {
            /* PSEUDOCODE / PLAN (detailed)
             * Write  a new record to 
             * 
             */
        }

        public string DataRecorderSnapShotStart(RotProbe probe, Mirror mirror, Chamber chamber, StepClass step)
        {
            /*
             * PSEUDOCODE / PLAN (detailed)
             * - Create a unique calibration ID per probe invocation.
             * - Format timestamp as "yyyyMMddHHmmss" using 24-hour time and invariant culture.
             * - Append a 2-digit index (##) representing the probe's position in the connected probe list.
             * - If probe exists in `_selectedProbes`, use its index; otherwise fall back to 0.
             * - Store as `calibrationID` string and return it.
             * This will be used to link  all records and snapshots for this calibration procedure in the database, allowing for historical tracking and analysis of calibration data over time.
             * Data to write to database here:
             *      -CalibrationID
             *      -StartedUTC (current UTC timestamp in ISO8601 format)
             *      -OperatorName (default to "Operator" for now
             *      -Notes (default to "None" for now)
             *      ProbeSerialNumber
             *      MirrorSerialNumber
             *      ChamberControllerSerialNumber
             *      ProbeSnapshotStartJson
             *      ChamberSnapshotStartJson
             *      MirrorSnapshotStartJson
             *      FOREIGN KEY(ProbeSerialNumber) REFERENCES Probe(SerialNumber),
             *      FOREIGN KEY(MirrorSerialNumber) REFERENCES Mirror(SerialNumber),
             *      FOREIGN KEY(ChamberControllerSerialNumber) REFERENCES Chamber(ControllerSerialNumber)
             */
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);

            var index = 0;
            if (probe != null && _selectedProbes != null && _selectedProbes.Count > 0)
            {
                var foundIndex = _selectedProbes.IndexOf(probe);
                if (foundIndex >= 0) index = foundIndex;
            }

            string calibrationID = timestamp + index.ToString("D2", System.Globalization.CultureInfo.InvariantCulture);

            /* Record JSON snapshot of calibration constants, probe name mirror, chamber calibration dates as outline in database design docs
             * 
             */
            return calibrationID;
        }
        public void DataRecorderSnapShotEnd(RotProbe probe, Mirror mirror, Chamber chamber, string CalibrationID)
        {
            /* Record JSON snapshot of calibration constants, probe name mirror, chamber calibration dates as outline in database design docs at calibration end
             * 
             */
        }


        public void DataRecorderNewOrUpdate(RotProbe probe, Chamber chamber, Mirror mirror)
        {
            // Upsert (insert/update) master records for Probe/Mirror/Chamber.
            // Does NOT create a Calibration row or snapshots (handled elsewhere).

            if (probe == null && chamber == null && mirror == null)
                return;

            try
            {
                var dbPath = Data.GetDatabasePath();
                var connStr = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Data Source={0};Version=3;Foreign Keys=True;", dbPath);

                using (var conn = new SQLiteConnection(connStr))
                {
                    conn.Open();

                    using (var tx = conn.BeginTransaction())
                    {
                        // Use a very explicit placeholder for "unknown/not yet calibrated".
                        // Stored in ISO8601 UTC format for consistency with other timestamps.
                        var placeholderUtc = new DateTime(1900,1,1,0,0,0, DateTimeKind.Utc);
                        var placeholderIso = placeholderUtc.ToString("o", System.Globalization.CultureInfo.InvariantCulture);

                        // --- Probe ---
                        if (probe != null && !string.IsNullOrWhiteSpace(probe.SerialNumber))
                        {
                            bool exists;
                            using (var existsCmd = conn.CreateCommand())
                            {
                                existsCmd.CommandText = "SELECT1 FROM Probe WHERE SerialNumber = @sn LIMIT1;";
                                existsCmd.Parameters.AddWithValue("@sn", probe.SerialNumber);
                                exists = existsCmd.ExecuteScalar() != null;
                            }

                            if (exists)
                            {
                                using (var cmd = conn.CreateCommand())
                                {
                                    cmd.CommandText = @"
UPDATE Probe SET
    ProbeType = @ProbeType,
    DeviceModel = @DeviceModel,
    FirmwareVersion = @FirmwareVersion,
    DeviceName = @DeviceName,
    DeviceType = @DeviceType,
    HumidityFactoryCorrection = @HumidityFactoryCorrection,
    HumidityUserCorrection = @HumidityUserCorrection,
    HumidityTemperatureCorrection = @HumidityTemperatureCorrection,
    HumidityDriftCorrection = @HumidityDriftCorrection,
    PT100CoeffA = @PT100CoeffA,
    PT100CoeffB = @PT100CoeffB,
    PT100CoeffC = @PT100CoeffC,
    TempOffset = @TempOffset,
    TempConversion = @TempConversion,
    LastCalibrationUtc = COALESCE(LastCalibrationUtc, @LastCalibrationUtc),
    NextDueUtc = COALESCE(NextDueUtc, @NextDueUtc)
WHERE SerialNumber = @SerialNumber;";
                                    cmd.Parameters.AddWithValue("@ProbeType", (object)probe.ProbeType ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@DeviceModel", (object)probe.DeviceModel ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@FirmwareVersion", (object)probe.FirmwareVersion ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@DeviceName", (object)probe.DeviceName ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@DeviceType", probe.DeviceType == '\0' ? (object)DBNull.Value : probe.DeviceType.ToString());
                                    cmd.Parameters.AddWithValue("@HumidityFactoryCorrection", probe.HumidityFactoryCorrection);
                                    cmd.Parameters.AddWithValue("@HumidityUserCorrection", probe.HumidityUserCorrection);
                                    cmd.Parameters.AddWithValue("@HumidityTemperatureCorrection", probe.HumidityTemperatureCorrection);
                                    cmd.Parameters.AddWithValue("@HumidityDriftCorrection", probe.HumidityDriftCorrection);
                                    cmd.Parameters.AddWithValue("@PT100CoeffA", probe.PT100CoeffA);
                                    cmd.Parameters.AddWithValue("@PT100CoeffB", probe.PT100CoeffB);
                                    cmd.Parameters.AddWithValue("@PT100CoeffC", probe.PT100CoeffC);
                                    cmd.Parameters.AddWithValue("@TempOffset", probe.TempOffset);
                                    cmd.Parameters.AddWithValue("@TempConversion", probe.TempConversion);
                                    cmd.Parameters.AddWithValue("@LastCalibrationUtc", placeholderIso);
                                    cmd.Parameters.AddWithValue("@NextDueUtc", placeholderIso);
                                    cmd.Parameters.AddWithValue("@SerialNumber", probe.SerialNumber);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                using (var cmd = conn.CreateCommand())
                                {
                                    cmd.CommandText = @"
INSERT INTO Probe (
    SerialNumber,
    ProbeType,
    DeviceModel,
    FirmwareVersion,
    DeviceName,
    DeviceType,
    HumidityFactoryCorrection,
    HumidityUserCorrection,
    HumidityTemperatureCorrection,
    HumidityDriftCorrection,
    PT100CoeffA,
    PT100CoeffB,
    PT100CoeffC,
    TempOffset,
    TempConversion,
    LastCalibrationUtc,
    NextDueUtc
) VALUES (
    @SerialNumber,
    @ProbeType,
    @DeviceModel,
    @FirmwareVersion,
    @DeviceName,
    @DeviceType,
    @HumidityFactoryCorrection,
    @HumidityUserCorrection,
    @HumidityTemperatureCorrection,
    @HumidityDriftCorrection,
    @PT100CoeffA,
    @PT100CoeffB,
    @PT100CoeffC,
    @TempOffset,
    @TempConversion,
    @LastCalibrationUtc,
    @NextDueUtc
);";
                                    cmd.Parameters.AddWithValue("@SerialNumber", probe.SerialNumber);
                                    cmd.Parameters.AddWithValue("@ProbeType", (object)probe.ProbeType ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@DeviceModel", (object)probe.DeviceModel ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@FirmwareVersion", (object)probe.FirmwareVersion ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@DeviceName", (object)probe.DeviceName ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@DeviceType", probe.DeviceType == '\0' ? (object)DBNull.Value : probe.DeviceType.ToString());
                                    cmd.Parameters.AddWithValue("@HumidityFactoryCorrection", probe.HumidityFactoryCorrection);
                                    cmd.Parameters.AddWithValue("@HumidityUserCorrection", probe.HumidityUserCorrection);
                                    cmd.Parameters.AddWithValue("@HumidityTemperatureCorrection", probe.HumidityTemperatureCorrection);
                                    cmd.Parameters.AddWithValue("@HumidityDriftCorrection", probe.HumidityDriftCorrection);
                                    cmd.Parameters.AddWithValue("@PT100CoeffA", probe.PT100CoeffA);
                                    cmd.Parameters.AddWithValue("@PT100CoeffB", probe.PT100CoeffB);
                                    cmd.Parameters.AddWithValue("@PT100CoeffC", probe.PT100CoeffC);
                                    cmd.Parameters.AddWithValue("@TempOffset", probe.TempOffset);
                                    cmd.Parameters.AddWithValue("@TempConversion", probe.TempConversion);
                                    cmd.Parameters.AddWithValue("@LastCalibrationUtc", placeholderIso);
                                    cmd.Parameters.AddWithValue("@NextDueUtc", placeholderIso);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }

                        // --- Mirror ---
                        if (mirror != null && !string.IsNullOrWhiteSpace(mirror.SerialNumber))
                        {
                            bool exists;
                            using (var existsCmd = conn.CreateCommand())
                            {
                                existsCmd.CommandText = "SELECT1 FROM Mirror WHERE SerialNumber = @sn LIMIT1;";
                                existsCmd.Parameters.AddWithValue("@sn", mirror.SerialNumber);
                                exists = existsCmd.ExecuteScalar() != null;
                            }

                            if (exists)
                            {
                                using (var cmd = conn.CreateCommand())
                                {
                                    cmd.CommandText = @"
UPDATE Mirror SET
    ID = @ID,
    IDN = @IDN,
    LastCalibrationUtc = COALESCE(LastCalibrationUtc, @LastCalibrationUtc),
    NextDueUtc = COALESCE(NextDueUtc, @NextDueUtc)
WHERE SerialNumber = @SerialNumber;";
                                    cmd.Parameters.AddWithValue("@ID", (object)mirror.ID ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@IDN", (object)mirror.IDN ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@LastCalibrationUtc", placeholderIso);
                                    cmd.Parameters.AddWithValue("@NextDueUtc", placeholderIso);
                                    cmd.Parameters.AddWithValue("@SerialNumber", mirror.SerialNumber);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                using (var cmd = conn.CreateCommand())
                                {
                                    cmd.CommandText = @"
INSERT INTO Mirror (
    SerialNumber,
    ID,
    IDN,
    LastCalibrationUtc,
    NextDueUtc
) VALUES (
    @SerialNumber,
    @ID,
    @IDN,
    @LastCalibrationUtc,
    @NextDueUtc
);";
                                    cmd.Parameters.AddWithValue("@SerialNumber", mirror.SerialNumber);
                                    cmd.Parameters.AddWithValue("@ID", (object)mirror.ID ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@IDN", (object)mirror.IDN ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@LastCalibrationUtc", placeholderIso);
                                    cmd.Parameters.AddWithValue("@NextDueUtc", placeholderIso);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }

                        // --- Chamber ---
                        if (chamber != null)
                        {
                            // Primary key is ControllerSerialNumber in DB. If it isn't available, do not write.
                            var controllerSerial = (chamber.ControllerSerial ?? string.Empty).Trim();
                            if (!string.IsNullOrWhiteSpace(controllerSerial))
                            {
                                bool exists;
                                using (var existsCmd = conn.CreateCommand())
                                {
                                    existsCmd.CommandText = "SELECT1 FROM Chamber WHERE ControllerSerialNumber = @csn LIMIT1;";
                                    existsCmd.Parameters.AddWithValue("@csn", controllerSerial);
                                    exists = existsCmd.ExecuteScalar() != null;
                                }

                                if (exists)
                                {
                                    using (var cmd = conn.CreateCommand())
                                    {
                                        cmd.CommandText = @"
UPDATE Chamber SET
    Name = @Name,
    LastIpAddress = @LastIpAddress,
    HC2SerialNumber = @HC2SerialNumber,
    DessicantSerialNumber = @DessicantSerialNumber,
    ControlProbeCalibrationUtc = COALESCE(ControlProbeCalibrationUtc, @ControlProbeCalibrationUtc),
    ControlProbeNextDueUtc = COALESCE(ControlProbeNextDueUtc, @ControlProbeNextDueUtc)
WHERE ControllerSerialNumber = @ControllerSerialNumber;";
                                        cmd.Parameters.AddWithValue("@Name", (object)chamber.Name ?? DBNull.Value);
                                        cmd.Parameters.AddWithValue("@LastIpAddress", (object)chamber.IPAddress ?? DBNull.Value);
                                        cmd.Parameters.AddWithValue("@HC2SerialNumber", (object)chamber.HC2Serial ?? DBNull.Value);
                                        cmd.Parameters.AddWithValue("@DessicantSerialNumber", (object)chamber.DessicantSerial ?? DBNull.Value);
                                        cmd.Parameters.AddWithValue("@ControlProbeCalibrationUtc", placeholderIso);
                                        cmd.Parameters.AddWithValue("@ControlProbeNextDueUtc", placeholderIso);
                                        cmd.Parameters.AddWithValue("@ControllerSerialNumber", controllerSerial);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                                else
                                {
                                    using (var cmd = conn.CreateCommand())
                                    {
                                        cmd.CommandText = @"
INSERT INTO Chamber (
    ControllerSerialNumber,
    Name,
    LastIpAddress,
    HC2SerialNumber,
    DessicantSerialNumber,
    ControlProbeCalibrationUtc,
    ControlProbeNextDueUtc
) VALUES (
    @ControllerSerialNumber,
    @Name,
    @LastIpAddress,
    @HC2SerialNumber,
    @DessicantSerialNumber,
    @ControlProbeCalibrationUtc,
    @ControlProbeNextDueUtc
);";

                                        cmd.Parameters.AddWithValue("@ControllerSerialNumber", controllerSerial);
                                        cmd.Parameters.AddWithValue("@Name", (object)chamber.Name ?? DBNull.Value);
                                        cmd.Parameters.AddWithValue("@LastIpAddress", (object)chamber.IPAddress ?? DBNull.Value);
                                        cmd.Parameters.AddWithValue("@HC2SerialNumber", (object)chamber.HC2Serial ?? DBNull.Value);
                                        cmd.Parameters.AddWithValue("@DessicantSerialNumber", (object)chamber.DessicantSerial ?? DBNull.Value);
                                        cmd.Parameters.AddWithValue("@ControlProbeCalibrationUtc", placeholderIso);
                                        cmd.Parameters.AddWithValue("@ControlProbeNextDueUtc", placeholderIso);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                        }

                        tx.Commit();
                    } // tx
                } // conn
            }
            catch (Exception ex)
            {
                try
                {
                    MessageBox.Show("Failed to record initial master data: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch { }
            }
        }
        public void HumidityStep(StepClass step, bool Manual)
        {
            return;
        }
        /*

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

                StartCalRoutine(_selectedProbes, _selectedMirror, _selectedChamber, _steps);
                return;
            }
            else if (button1.Text == "Skip Soak")
            {
                SkipSoakTimer();
            }
        }

        public void SoakTimer(StepClass step)
        {
            button1.Text = "Skip Soak";
            if (step == null) return;

            _soakSkipRequested = false;

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
                _soakTimer.Interval =1000;
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

            _soakSkipRequested = true;
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

        public void StartCalRoutine(List<RotProbe> rotProbes, Mirror mirror, Chamber chamber, List<StepClass> steps)
        {
            // Record initial master data with a single pass over the probe list.
            // Desired order:
            //1) foreach probe -> record initial probe data
            //2) record initial mirror data once
            //3) record initial chamber data once

            if (rotProbes != null)
            {
                foreach (var probe in rotProbes)
                {
                    // Only pass the probe to avoid mirror/chamber being upserted repeatedly.
                    DataRecorderNewOrUpdate(probe, null, null);
                }
            }

            // Then mirror master record
            DataRecorderNewOrUpdate(null, null, mirror);

            // Finally chamber master record
            DataRecorderNewOrUpdate(null, chamber, null);

            //Collect initial snapshot before beginning caliration. Generate new calibration ID for each probe
            var CalibrationID = "";
            foreach (var probe in rotProbes)
            {
                CalibrationID = DataRecorderSnapShotStart(probe, mirror, chamber, null);
            }

            foreach (var step in steps)
            {
                ChamberController(chamber, _manual, step);
                SoakTimer(step);
                WaitForSoakToComplete();
                foreach (var probe in rotProbes)
                {
                    DataRecorder(probe, mirror, chamber, step, CalibrationID);
                }
            }

            foreach (var probe in rotProbes)
            {
                DataRecorderSnapShotEnd(probe, mirror, chamber, CalibrationID);
            }
        }

        private void WaitForSoakToComplete()
        {
            // Wait until the soak timer reaches0 or the user requests a skip.
            // Keep UI responsive.
            while (!IsDisposed && !_soakSkipRequested && _soakRemaining > TimeSpan.Zero)
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(50);
            }
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