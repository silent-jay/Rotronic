using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Rotronic
{


    public partial class CalProgressFrm : Form
    {
        private const int SamplesPerStep = 5;
        private static readonly TimeSpan SampleInterval = TimeSpan.FromSeconds(1);
        private readonly List<RotProbe> _selectedProbes;
        private readonly Mirror _selectedMirror;
        private readonly Chamber _selectedChamber;
        private readonly List<StepClass> _steps;
        private readonly bool _manual;

        private readonly Timer _uiRefreshTimer = new Timer();

        private readonly Timer _gridRefreshTimer = new Timer();
        private volatile bool _calibrationRunning = false;

        private volatile bool _closeRequested = false;
        private readonly object _calibrationIdsLock = new object();
        private readonly HashSet<string> _activeCalibrationIds = new HashSet<string>(StringComparer.Ordinal);

        private readonly Timer _samplingCountdownTimer = new Timer();
        private volatile bool _samplingInProgress = false;
        private DateTime _samplingEndUtc;
        private string _buttonTextBeforeSampling;

        private Timer _soakTimer;
        private TimeSpan _soakRemaining = TimeSpan.Zero;
        private volatile bool _soakSkipRequested = false;

        private readonly Dictionary<string, Dictionary<int, int>> _stepIdByCalibrationAndStepNumber
            = new Dictionary<string, Dictionary<int, int>>(StringComparer.Ordinal);

        private readonly Dictionary<string, Dictionary<int, StepTiming>> _stepTimingsByCalibration
            = new Dictionary<string, Dictionary<int, StepTiming>>(StringComparer.Ordinal);
        private readonly bool warning = false;

        private sealed class StepTiming
        {
            public DateTime? RampStartUtc { get; set; }
            public DateTime? SoakStartUtc { get; set; }
            public DateTime? SoakEndUtc { get; set; }
        }

        private sealed class SampleRecord
        {
            public int StepNumber { get; set; }
            public DateTime SampleUtc { get; set; }

            public string ProbeSerialNumber { get; set; }

            public double? ProbeHumidity { get; set; }
            public int? ProbeHumidityCount { get; set; }
            public double? ProbeHumidityRaw { get; set; }
            public double? ProbeTemperatureC { get; set; }
            public int? ProbeTemperatureCount { get; set; }
            public double? ProbeResistance { get; set; }

            public double? MirrorDewPointC { get; set; }
            public double? MirrorFrostPointC { get; set; }
            public double? MirrorHumidity { get; set; }
            public double? ExternalTemperatureC { get; set; }
            public double? MirrorTemperatureC { get; set; }

            public double? ChamberTemperatureC { get; set; }
            public double? ChamberTemperatureSetpointC { get; set; }
            public double? ChamberHumidity { get; set; }
            public double? ChamberHumiditySetpoint { get; set; }
        }

        public CalProgressFrm()
            : this(new List<RotProbe>(), null, null, new List<StepClass>(), false)
        {
        }


        public CalProgressFrm(List<RotProbe> selectedProbes, Mirror selectedMirror, Chamber selectedChamber, List<StepClass> steps, bool manual)
        {
            InitializeComponent();

            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
                return;

            this.FormClosed += CalProgressFrm_FormClosed;
            this.FormClosing += CalProgressFrm_FormClosing;

            _selectedProbes = selectedProbes ?? new List<RotProbe>();
            _selectedMirror = selectedMirror;
            _selectedChamber = selectedChamber;

            _steps = steps ?? new List<StepClass>();
            _manual = manual;

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

            comboBoxRotProbe.SelectedIndexChanged += (s, e) => RefreshCalProgressGrid();

            // Grid is refreshed on explicit DB flush events to avoid scroll resets.

            _samplingCountdownTimer.Interval = 250;
            _samplingCountdownTimer.Tick += (s, e) => UpdateSamplingUi();

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

        private void RefreshCalProgressGrid()
        {
            if (IsDisposed) return;

            if (InvokeRequired)
            {
                BeginInvoke((Action)RefreshCalProgressGrid);
                return;
            }

            var selected = comboBoxRotProbe.SelectedItem as ProbeComboItem;
            var probe = selected != null ? selected.Probe : null;
            var sn = probe != null ? probe.SerialNumber : null;

            if (string.IsNullOrWhiteSpace(sn))
            {
                dataGridViewCalProgress.DataSource = null;
                return;
            }

            try
            {
                var dbPath = Data.GetDatabasePath();
                var connStr = string.Format(CultureInfo.InvariantCulture, "Data Source={0};Version=3;Foreign Keys=True;", dbPath);

                using (var conn = new SQLiteConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
WITH CurrentCalibration AS (
    SELECT CalibrationId
    FROM Calibration
    WHERE ProbeSerialNumber = @sn
    ORDER BY StartedUtc DESC
    LIMIT 1
)
SELECT
    sa.SampleUtc AS [Sample Time],
    s.StepNumber,
    s.HumiditySetpoint AS [Humidity Setpoint],
    sa.ProbeHumidity AS [Probe Humidity],
    sa.MirrorHumidity AS [Mirror Humidity],
    sa.ChamberHumidity AS [Chamber Humidity],
    s.TemperatureSetpointC AS [Temperature Set Point],
    sa.ProbeTemperatureC AS [Probe Temperature],
    sa.MirrorTemperatureC AS [Mirror Temperature],
    sa.ChamberTemperatureC AS [Chamber Temperature]
FROM CurrentCalibration cc
JOIN Step s ON s.CalibrationId = cc.CalibrationId
JOIN Sample sa ON sa.StepId = s.StepId
ORDER BY sa.SampleUtc ASC;";
                        cmd.Parameters.AddWithValue("@sn", sn);

                        using (var adapter = new SQLiteDataAdapter(cmd))
                        {
                            var dt = new DataTable();
                            adapter.Fill(dt);
                            dataGridViewCalProgress.DataSource = dt;
                        }
                    }
                }
            }
            catch
            {
                // Ignore transient read errors while DB is being written.
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

            _gridRefreshTimer.Stop();
            _gridRefreshTimer.Dispose();

            _samplingCountdownTimer.Stop();
            _samplingCountdownTimer.Dispose();

            SkipSoakTimer();

            if (_selectedChamber != null)
                SafeClose(_selectedChamber);
        }

        private void CalProgressFrm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_closeRequested)
                return;

            if (!_calibrationRunning)
                return;

            var result = MessageBox.Show(
                "A calibration is in progress, are you sure you want to close?",
                "Calibration In Progress",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (result != DialogResult.Yes)
            {
                e.Cancel = true;
                return;
            }

            _closeRequested = true;

            try { SkipSoakTimer(); } catch { }

            // Best-effort safety shutdown actions before close.
            try { TurnOffChamberControl(_selectedChamber); } catch { }
            try { RollbackActiveCalibrations(); } catch { }
        }

        private void TurnOffChamberControl(Chamber chamber)
        {
            if (chamber == null)
                return;

            try { ChamberCommands.SetTempControl(chamber, false); } catch { }
            try { ChamberCommands.SetRHControl(chamber, false); } catch { }
        }

        private void RollbackActiveCalibrations()
        {
            List<string> ids;
            lock (_calibrationIdsLock)
            {
                ids = _activeCalibrationIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).ToList();
                _activeCalibrationIds.Clear();
            }

            if (ids.Count == 0)
                return;

            var dbPath = Data.GetDatabasePath();
            var connStr = string.Format(CultureInfo.InvariantCulture, "Data Source={0};Version=3;Foreign Keys=True;", dbPath);

            using (var conn = new SQLiteConnection(connStr))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = "DELETE FROM Calibration WHERE CalibrationId = @id;";
                        var p = cmd.Parameters.Add("@id", DbType.String);

                        foreach (var id in ids)
                        {
                            p.Value = id;
                            cmd.ExecuteNonQuery();
                        }
                    }

                    tx.Commit();
                }
            }
        }
        public void ChamberController(Chamber chamber, bool Manual, StepClass stepClass)
        {
            if (_closeRequested || IsDisposed)
                return;

            if (Manual)
            {
                if (_closeRequested || IsDisposed)
                    return;
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

                    if (_closeRequested || IsDisposed)
                        return;

                    RefreshStabilityIndicators();
                    Application.DoEvents(); // Keep UI responsive
                }

                RefreshStabilityIndicators();
            }
        }
        private void DataRecorder(RotProbe probe, Mirror mirror, Chamber chamber, StepClass step, string calibrationID, int stepNumber, StepTiming timing, List<SampleRecord> bufferedSamples)
        {
            if (probe == null || bufferedSamples == null)
                return;

            var rec = new SampleRecord
            {
                StepNumber = stepNumber,
                SampleUtc = DateTime.UtcNow,
                ProbeSerialNumber = probe.SerialNumber,

                ProbeHumidity = probe.Humidity,
                ProbeHumidityCount = probe.HumidityCount,
                ProbeHumidityRaw = probe.HumdityRaw,
                ProbeTemperatureC = probe.Temperature,
                ProbeTemperatureCount = probe.TemperatureCount,
                ProbeResistance = probe.Resistance,

                MirrorDewPointC = mirror != null ? (double?)mirror.DewPoint : null,
                MirrorFrostPointC = mirror != null ? (double?)mirror.FrostPoint : null,
                MirrorHumidity = mirror != null ? (double?)mirror.Humdity : null,
                ExternalTemperatureC = mirror != null ? (double?)mirror.ExternalTemp : null,
                MirrorTemperatureC = mirror != null ? (double?)mirror.MirrorTemp : null,

                ChamberTemperatureC = chamber != null ? (double?)chamber.Temperature : null,
                ChamberTemperatureSetpointC = chamber != null ? (double?)chamber.TemperatureSP : null,
                ChamberHumidity = chamber != null ? (double?)chamber.Humidity : null,
                ChamberHumiditySetpoint = chamber != null ? (double?)chamber.HumiditySP : null
            };

            bufferedSamples.Add(rec);
        }

        private void BeginSamplingUi(TimeSpan duration)
        {
            if (IsDisposed) return;

            if (InvokeRequired)
            {
                BeginInvoke((Action)(() => BeginSamplingUi(duration)));
                return;
            }

            _samplingInProgress = true;
            _samplingEndUtc = DateTime.UtcNow.Add(duration);

            _buttonTextBeforeSampling = button1.Text;
            button1.Enabled = false;
            button1.Text = "Sampling...";

            textBoxSoak.ReadOnly = true;
            textBoxSoak.TabStop = false;

            UpdateSamplingUi();
            _samplingCountdownTimer.Start();
        }

        private static TimeSpan GetSamplingUiDuration(int samplesPerStep, TimeSpan interval)
        {
            if (samplesPerStep <= 0) return TimeSpan.Zero;
            if (samplesPerStep == 1) return TimeSpan.Zero;
            if (interval < TimeSpan.Zero) interval = TimeSpan.Zero;
            return TimeSpan.FromTicks(interval.Ticks * (samplesPerStep - 1));
        }

        private void EndSamplingUi()
        {
            if (IsDisposed) return;

            if (InvokeRequired)
            {
                BeginInvoke((Action)EndSamplingUi);
                return;
            }

            _samplingInProgress = false;
            _samplingCountdownTimer.Stop();

            if (_calibrationRunning)
            {
                button1.Text = "Skip Soak";
                button1.Enabled = true;
            }
            else
            {
                button1.Text = string.IsNullOrWhiteSpace(_buttonTextBeforeSampling) ? "Start Calibration" : _buttonTextBeforeSampling;
                button1.Enabled = true;
            }
        }

        private void UpdateSamplingUi()
        {
            if (IsDisposed) return;

            if (InvokeRequired)
            {
                BeginInvoke((Action)UpdateSamplingUi);
                return;
            }

            if (!_samplingInProgress)
                return;

            var remaining = _samplingEndUtc - DateTime.UtcNow;
            if (remaining < TimeSpan.Zero)
                remaining = TimeSpan.Zero;

            textBoxSoak.Text = string.Format(CultureInfo.InvariantCulture, "{0:D2}:{1:D2}", (int)remaining.TotalMinutes, remaining.Seconds);
        }

        public string DataRecorderSnapShotStart(RotProbe probe, Mirror mirror, Chamber chamber)
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

            var startedUtcIso = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

            string probeSnapshotStartJson = null;
            if (probe != null)
            {
                probeSnapshotStartJson = "{"
                    + "\"ProbeType\":" + ToJsonString(probe.ProbeType) + ","
                    + "\"HumidityFactoryCorrection\":" + ToJsonNumber(probe.HumidityFactoryCorrection) + ","
                    + "\"HumidityUserCorrection\":" + ToJsonNumber(probe.HumidityUserCorrection) + ","
                    + "\"HumidityTemperatureCorrection\":" + ToJsonNumber(probe.HumidityTemperatureCorrection) + ","
                    + "\"HumidityDriftCorrection\":" + ToJsonNumber(probe.HumidityDriftCorrection) + ","
                    + "\"PT100CoeffA\":" + ToJsonNumber(probe.PT100CoeffA) + ","
                    + "\"PT100CoeffB\":" + ToJsonNumber(probe.PT100CoeffB) + ","
                    + "\"PT100CoeffC\":" + ToJsonNumber(probe.PT100CoeffC) + ","
                    + "\"TempOffset\":" + ToJsonNumber(probe.TempOffset) + ","
                    + "\"TempConversion\":" + ToJsonNumber(probe.TempConversion) + ","
                    + "\"DeviceModel\":" + ToJsonString(probe.DeviceModel) + ","
                    + "\"FirmwareVersion\":" + ToJsonString(probe.FirmwareVersion) + ","
                    + "\"SerialNumber\":" + ToJsonString(probe.SerialNumber) + ","
                    + "\"DeviceName\":" + ToJsonString(probe.DeviceName) + ","
                    + "\"DeviceType\":" + (probe.DeviceType == '\0' ? "null" : ToJsonString(probe.DeviceType.ToString()))
                    + "}";
            }

            string mirrorSnapshotStartJson = null;
            if (mirror != null)
            {
                mirrorSnapshotStartJson = "{"
                    + "\"ID\":" + ToJsonString(mirror.ID) + ","
                    + "\"IDN\":" + ToJsonString(mirror.IDN) + ","
                    + "\"SerialNumber\":" + ToJsonString(mirror.SerialNumber) + ","
                    + "\"LastCalibrationUtc\":null,"
                    + "\"NextDueUtc\":null"
                    + "}";
            }

            string chamberSnapshotStartJson = null;
            if (chamber != null)
            {
                chamberSnapshotStartJson = "{"
                    + "\"Name\":" + ToJsonString(chamber.Name) + ","
                    + "\"LastIpAddress\":" + ToJsonString(chamber.IPAddress) + ","
                    + "\"HC2SerialNumber\":" + ToJsonString(chamber.HC2Serial) + ","
                    + "\"ControlProbeCalibrationUtc\":null,"
                    + "\"ControlProbeNextDueUtc\":null"
                    + "}";
            }

            try
            {
                var dbPath = Data.GetDatabasePath();
                var connStr = string.Format(CultureInfo.InvariantCulture, "Data Source={0};Version=3;Foreign Keys=True;", dbPath);

                using (var conn = new SQLiteConnection(connStr))
                {
                    conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
INSERT INTO Calibration (
    CalibrationId,
    StartedUtc,
    OperatorName,
    Notes,
    ProbeSerialNumber,
    MirrorSerialNumber,
    ChamberControllerSerialNumber,
    ProbeSnapshotStartJson,
    MirrorSnapshotStartJson,
    ChamberSnapshotStartJson
) VALUES (
    @CalibrationId,
    @StartedUtc,
    @OperatorName,
    @Notes,
    @ProbeSerialNumber,
    @MirrorSerialNumber,
    @ChamberControllerSerialNumber,
    @ProbeSnapshotStartJson,
    @MirrorSnapshotStartJson,
    @ChamberSnapshotStartJson
);";

                        cmd.Parameters.AddWithValue("@CalibrationId", calibrationID);
                        cmd.Parameters.AddWithValue("@StartedUtc", startedUtcIso);
                        cmd.Parameters.AddWithValue("@OperatorName", "Operator");
                        cmd.Parameters.AddWithValue("@Notes", "None");
                        cmd.Parameters.AddWithValue("@ProbeSerialNumber", (object)(probe != null ? probe.SerialNumber : null) ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@MirrorSerialNumber", (object)(mirror != null ? mirror.SerialNumber : null) ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ChamberControllerSerialNumber", (object)(chamber != null ? chamber.ControllerSerial : null) ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ProbeSnapshotStartJson", (object)probeSnapshotStartJson ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@MirrorSnapshotStartJson", (object)mirrorSnapshotStartJson ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ChamberSnapshotStartJson", (object)chamberSnapshotStartJson ?? DBNull.Value);

                        cmd.ExecuteNonQuery();
                    }

                }
            }
            catch (Exception ex)
            {
                try
                {
                    MessageBox.Show("Failed to record calibration start snapshot: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch { }
            }

            return calibrationID;


        }

        public void DataRecorderSnapShotEnd(RotProbe probe, Mirror mirror, Chamber chamber, string calibrationID)
        {

            if (string.IsNullOrWhiteSpace(calibrationID))
                return;

            // Make sure any calibration changes to the probe are reflected in the master record.
            // This captures the probe's current status for future calibrations/operations.
            DataRecorderNewOrUpdate(probe, null, null);

            var endedUtcIso = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

            string probeSnapshotEndJson = null;
            if (probe != null)
            {
                probeSnapshotEndJson = "{"
                    + "\"ProbeType\":" + ToJsonString(probe.ProbeType) + ","
                    + "\"HumidityFactoryCorrection\":" + ToJsonNumber(probe.HumidityFactoryCorrection) + ","
                    + "\"HumidityUserCorrection\":" + ToJsonNumber(probe.HumidityUserCorrection) + ","
                    + "\"HumidityTemperatureCorrection\":" + ToJsonNumber(probe.HumidityTemperatureCorrection) + ","
                    + "\"HumidityDriftCorrection\":" + ToJsonNumber(probe.HumidityDriftCorrection) + ","
                    + "\"PT100CoeffA\":" + ToJsonNumber(probe.PT100CoeffA) + ","
                    + "\"PT100CoeffB\":" + ToJsonNumber(probe.PT100CoeffB) + ","
                    + "\"PT100CoeffC\":" + ToJsonNumber(probe.PT100CoeffC) + ","
                    + "\"TempOffset\":" + ToJsonNumber(probe.TempOffset) + ","
                    + "\"TempConversion\":" + ToJsonNumber(probe.TempConversion) + ","
                    + "\"DeviceModel\":" + ToJsonString(probe.DeviceModel) + ","
                    + "\"FirmwareVersion\":" + ToJsonString(probe.FirmwareVersion) + ","
                    + "\"SerialNumber\":" + ToJsonString(probe.SerialNumber) + ","
                    + "\"DeviceName\":" + ToJsonString(probe.DeviceName) + ","
                    + "\"DeviceType\":" + (probe.DeviceType == '\0' ? "null" : ToJsonString(probe.DeviceType.ToString()))
                    + "}";
            }

            string mirrorSnapshotEndJson = null;
            if (mirror != null)
            {
                mirrorSnapshotEndJson = "{"
                    + "\"ID\":" + ToJsonString(mirror.ID) + ","
                    + "\"IDN\":" + ToJsonString(mirror.IDN) + ","
                    + "\"SerialNumber\":" + ToJsonString(mirror.SerialNumber) + ","
                    + "\"LastCalibrationUtc\":null,"
                    + "\"NextDueUtc\":null"
                    + "}";
            }

            string chamberSnapshotEndJson = null;
            if (chamber != null)
            {
                chamberSnapshotEndJson = "{"
                    + "\"Name\":" + ToJsonString(chamber.Name) + ","
                    + "\"LastIpAddress\":" + ToJsonString(chamber.IPAddress) + ","
                    + "\"HC2SerialNumber\":" + ToJsonString(chamber.HC2Serial) + ","
                    + "\"ControlProbeCalibrationUtc\":null,"
                    + "\"ControlProbeNextDueUtc\":null"
                    + "}";
            }

            try
            {
                var dbPath = Data.GetDatabasePath();
                var connStr = string.Format(CultureInfo.InvariantCulture, "Data Source={0};Version=3;Foreign Keys=True;", dbPath);

                using (var conn = new SQLiteConnection(connStr))
                {
                    conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
UPDATE Calibration
SET
    EndedUtc = @EndedUtc,
    ProbeSnapshotEndJson = @ProbeSnapshotEndJson,
    MirrorSnapshotEndJson = @MirrorSnapshotEndJson,
    ChamberSnapshotEndJson = @ChamberSnapshotEndJson
WHERE CalibrationId = @CalibrationId;";

                        cmd.Parameters.AddWithValue("@EndedUtc", endedUtcIso);
                        cmd.Parameters.AddWithValue("@ProbeSnapshotEndJson", (object)probeSnapshotEndJson ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@MirrorSnapshotEndJson", (object)mirrorSnapshotEndJson ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ChamberSnapshotEndJson", (object)chamberSnapshotEndJson ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CalibrationId", calibrationID);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    MessageBox.Show("Failed to record calibration end snapshot: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch { }
            }
        }

        private static string ToJsonString(string value)
        {
            if (value == null) return "null";

            var sb = new StringBuilder(value.Length + 2);
            sb.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                var ch = value[i];
                switch (ch)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (ch < 0x20)
                            sb.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            sb.Append(ch);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        private static string ToJsonNumber(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return "null";
            return value.ToString("R", CultureInfo.InvariantCulture);
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
                                existsCmd.CommandText = "SELECT 1 FROM Probe WHERE SerialNumber = @sn LIMIT 1;";
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
                                existsCmd.CommandText = "SELECT 1 FROM Mirror WHERE SerialNumber = @sn LIMIT 1;";
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
                                    existsCmd.CommandText = "SELECT 1 FROM Chamber WHERE ControllerSerialNumber = @csn LIMIT 1;";
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
            if (!_samplingInProgress)
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

            if (!_samplingInProgress)
            {
                button1.Text = "Start Calibration";
                button1.Enabled = true;
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

        private void CacheStepId(string calibrationId, int stepNumber, int stepId)
        {
            if (!_stepIdByCalibrationAndStepNumber.TryGetValue(calibrationId, out var map))
            {
                map = new Dictionary<int, int>();
                _stepIdByCalibrationAndStepNumber[calibrationId] = map;
            }
            map[stepNumber] = stepId;
        }

        private bool TryGetStepId(string calibrationId, int stepNumber, out int stepId)
        {
            stepId = 0;
            if (_stepIdByCalibrationAndStepNumber.TryGetValue(calibrationId, out var map))
            {
                return map.TryGetValue(stepNumber, out stepId);
            }
            return false;
        }

        private StepTiming GetStepTiming(string calibrationId, int stepNumber)
        {
            if (!_stepTimingsByCalibration.TryGetValue(calibrationId, out var map))
            {
                map = new Dictionary<int, StepTiming>();
                _stepTimingsByCalibration[calibrationId] = map;
            }

            if (!map.TryGetValue(stepNumber, out var timing))
            {
                timing = new StepTiming();
                map[stepNumber] = timing;
            }

            return timing;
        }

        private void SetRampStartUtc(string calibrationId, int stepNumber, DateTime utc)
        {
            GetStepTiming(calibrationId, stepNumber).RampStartUtc = utc;
        }

        private void SetSoakStartUtc(string calibrationId, int stepNumber, DateTime utc)
        {
            GetStepTiming(calibrationId, stepNumber).SoakStartUtc = utc;
        }

        private void SetSoakEndUtc(string calibrationId, int stepNumber, DateTime utc)
        {
            GetStepTiming(calibrationId, stepNumber).SoakEndUtc = utc;
        }

        private static string ToIsoUtcOrNull(DateTime? utc)
        {
            if (!utc.HasValue) return null;
            var value = utc.Value;
            if (value.Kind != DateTimeKind.Utc)
                value = DateTime.SpecifyKind(value, DateTimeKind.Utc);
            return value.ToString("o", CultureInfo.InvariantCulture);
        }

        public bool Warning()
        {
            var result = MessageBox.Show("This will save any adjustment points to the probe's internal memory. Do you want to proceed?", "Confirm Probe Adjustment", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
                return true;
            return false;

        }

        public void AdvancedTemperatureAdjust(int IndexStart, int IndexEnd, RotProbe probe)
        {
            if (probe == null || string.IsNullOrWhiteSpace(probe.SerialNumber))
                return;

            if (IndexEnd < IndexStart)
                return;

            var startStepNumber = IndexStart + 1;
            var endStepNumber = IndexEnd + 1;

            double[] MirrorTemp = Array.Empty<double>();
            double[] ExternalTemp = Array.Empty<double>();
            double[] ProbeTemp = Array.Empty<double>();
            double[] ProbeTempCount = Array.Empty<double>();
            double[] ProbeResistance = Array.Empty<double>();

            /*query database using RotProbe serial number or other appropriate input to get last calibrationID associated with the probe (should be current calibration being done)
             * -> query for probe, find last cal ID, join to steps table by calibrationID join to sample table by StepID.
            Index start should correspond to stepID where advanced temperature data starts, and index end should correspond to stepID where advanced temperature data ends.
            relavent data for each sample is ProbeTemperatureC, ProbeTemperatureCount, ProbeTemperatureResistance, ExternalTemperatureC, MirrorTemperatureC

            For each series of five samples corresponding to each step, average each value. Use the averaged values in subsequent calculations

            pseudocode:
                   query results for step/samples
                   for each series of samples in the step
                        calculate average probe temp, average probe temp count, average probe resistance, average mirror temp, average external temp
                            assign average values to arrays (i.e. MirrorTemp[0] = average mirror temp for step 1, MirrorTemp[1] = average mirror temp for step 2, etc.) - step index may not start at 0 from database
                            but we don't need to get the step index back out of this method.

            */


            try
            {
                var dbPath = Data.GetDatabasePath();
                var connStr = string.Format(CultureInfo.InvariantCulture, "Data Source={0};Version=3;Foreign Keys=True;", dbPath);

                using (var conn = new SQLiteConnection(connStr))
                {
                    conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
WITH CurrentCalibration AS (
    SELECT CalibrationId
    FROM Calibration
    WHERE ProbeSerialNumber = @ProbeSerialNumber
    ORDER BY StartedUtc DESC
    LIMIT 1
)
SELECT
    s.StepNumber,
    AVG(sa.MirrorTemperatureC) AS AvgMirrorTemperatureC,
    AVG(sa.ExternalTemperatureC) AS AvgExternalTemperatureC,
    AVG(sa.ProbeTemperatureC) AS AvgProbeTemperatureC,
    AVG(sa.ProbeTemperatureCount) AS AvgProbeTemperatureCount,
    AVG(sa.ProbeResistance) AS AvgProbeResistance
FROM CurrentCalibration cc
JOIN Step s ON s.CalibrationId = cc.CalibrationId
JOIN Sample sa ON sa.StepId = s.StepId
WHERE s.StepNumber BETWEEN @StartStepNumber AND @EndStepNumber
GROUP BY s.StepId, s.StepNumber
ORDER BY s.StepNumber;";

                        cmd.Parameters.AddWithValue("@ProbeSerialNumber", probe.SerialNumber);
                        cmd.Parameters.AddWithValue("@StartStepNumber", startStepNumber);
                        cmd.Parameters.AddWithValue("@EndStepNumber", endStepNumber);

                        var mirrorTemps = new List<double>();
                        var externalTemps = new List<double>();
                        var probeTemps = new List<double>();
                        var probeTempCounts = new List<double>();
                        var probeResistances = new List<double>();

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                mirrorTemps.Add(reader["AvgMirrorTemperatureC"] == DBNull.Value ? double.NaN : Convert.ToDouble(reader["AvgMirrorTemperatureC"], CultureInfo.InvariantCulture));
                                externalTemps.Add(reader["AvgExternalTemperatureC"] == DBNull.Value ? double.NaN : Convert.ToDouble(reader["AvgExternalTemperatureC"], CultureInfo.InvariantCulture));
                                probeTemps.Add(reader["AvgProbeTemperatureC"] == DBNull.Value ? double.NaN : Convert.ToDouble(reader["AvgProbeTemperatureC"], CultureInfo.InvariantCulture));
                                probeTempCounts.Add(reader["AvgProbeTemperatureCount"] == DBNull.Value ? double.NaN : Convert.ToDouble(reader["AvgProbeTemperatureCount"], CultureInfo.InvariantCulture));
                                probeResistances.Add(reader["AvgProbeResistance"] == DBNull.Value ? double.NaN : Convert.ToDouble(reader["AvgProbeResistance"], CultureInfo.InvariantCulture));
                            }
                        }

                        MirrorTemp = mirrorTemps.ToArray();
                        ExternalTemp = externalTemps.ToArray();
                        ProbeTemp = probeTemps.ToArray();
                        ProbeTempCount = probeTempCounts.ToArray();
                        ProbeResistance = probeResistances.ToArray();

                        ApplyAdvancedTemperatureCoefficients(ProbeTemp, ProbeTempCount, ProbeResistance, MirrorTemp, probe);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to query advanced temperature adjustment data: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

        }

        private static void ApplyAdvancedTemperatureCoefficients(double[] probeTemp, double[] probeTempCount, double[] probeResistance, double[] mirrorTemp, RotProbe probe)
        {
            if (probe == null)
                return;

            var result = CoefficientCalc.Calculate(probeTemp, probeTempCount, probeResistance, mirrorTemp, probe.TempConversion);

            RotProbeCommands.SendNewTemperatureCoeffA(probe, result.A);
            RotProbeCommands.SendNewTemperatureCoeffB(probe, result.B);
            RotProbeCommands.SendNewTemperatureCoeffC(probe, result.C);
            RotProbeCommands.SendNewTemperatureOffset(probe, result.Offset);
        }

        public void StartCalRoutine(List<RotProbe> rotProbes, Mirror mirror, Chamber chamber, List<StepClass> steps)
        {
            _calibrationRunning = true;
            // Record initial master data with a single pass over the probe list.
            // Desired order:
            //1) foreach probe -> record initial probe data
            //2) record initial mirror data once
            //3) record initial chamber data once
            int endIndex = 0;
            int startIndex = 0;
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

            // Collect initial snapshot before beginning calibration.
            // CalibrationId is per-probe.
            var calibrationIdByProbeSerial = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var probe in rotProbes)
            {
                if (probe == null || string.IsNullOrWhiteSpace(probe.SerialNumber))
                    continue;

                var calId = DataRecorderSnapShotStart(probe, mirror, chamber);
                calibrationIdByProbeSerial[probe.SerialNumber] = calId;

                lock (_calibrationIdsLock)
                {
                    if (!string.IsNullOrWhiteSpace(calId))
                        _activeCalibrationIds.Add(calId);
                }
            }

            //iterate through each step in calibration procedure
            for (int stepIndex = 0; stepIndex < steps.Count; stepIndex++)
            {
                if (_closeRequested || IsDisposed)
                    break;

                var step = steps[stepIndex];
                var stepNumber = stepIndex + 1;

                // Handle "Adjust" step: save humidity and temperature adjustments to probe
                if (string.Equals(step.Step, "Adjust", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var probe in rotProbes)
                    {
                        if (probe == null || string.IsNullOrWhiteSpace(probe.SerialNumber)) continue;
                        RotProbeCommands.SendHumiditySaveAdjust(probe);
                        RotProbeCommands.SendTempTestPointAdjust(probe);
                    }
                    // Skip normal calibration processing for this step
                    continue;
                }
                if (string.Equals(step.Step, "AdvancedTempStart", StringComparison.OrdinalIgnoreCase))
                {
                    // note index number (or probably next index number since this is a marker step) for advanced temperature step
                    startIndex = stepIndex + 1;
                    continue;
                }
                if (string.Equals(step.Step, "AdvancedTempEnd", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var probe in rotProbes)
                    {
                        var calibrationID = calibrationIdByProbeSerial.TryGetValue(probe.SerialNumber, out var id) ? id : null;
                        // note index number for end of advanced temperature step block. Logic for advanced temp adjustment will likely need to reference both start and end step numbers to know which steps to pull data from for calculations.
                        endIndex = stepIndex - 1;
                        AdvancedTemperatureAdjust(startIndex, endIndex, probe);
                    }
                    continue;
                }

                var rampStartUtc = DateTime.UtcNow;
                foreach (var calId in calibrationIdByProbeSerial.Values)
                    SetRampStartUtc(calId, stepNumber, rampStartUtc);
                // sets chamber conditions or prompts user to set chamber conditions
                ChamberController(chamber, _manual, step);
                if (_closeRequested || IsDisposed)
                    break;
                // starts soak timer to ensure chamber conditions are stable
                var soakStartUtc = DateTime.UtcNow;
                foreach (var calId in calibrationIdByProbeSerial.Values)
                    SetSoakStartUtc(calId, stepNumber, soakStartUtc);
                SoakTimer(step);
                //wait step holds here until skip button is pressed or timer completes
                WaitForSoakToComplete();
                if (_closeRequested || IsDisposed)
                    break;
                var soakEndUtc = DateTime.UtcNow;
                foreach (var calId in calibrationIdByProbeSerial.Values)
                    SetSoakEndUtc(calId, stepNumber, soakEndUtc);

                // Sampling: AFTER soak.
                // Important: sample "rounds" (all probes captured) share the same delay, to avoid interval * probeCount.
                BeginSamplingUi(GetSamplingUiDuration(SamplesPerStep, SampleInterval));
                var bufferedSamplesByCalibration = new Dictionary<string, List<SampleRecord>>(StringComparer.Ordinal);
                foreach (var kvp in calibrationIdByProbeSerial)
                {
                    if (!bufferedSamplesByCalibration.ContainsKey(kvp.Value))
                        bufferedSamplesByCalibration[kvp.Value] = new List<SampleRecord>();
                }
                for (int sampleIndex = 0; sampleIndex < SamplesPerStep; sampleIndex++)
                {
                    if (_closeRequested || IsDisposed)
                        break;

                    var nowUtc = DateTime.UtcNow;
                    foreach (var probe in rotProbes)
                    {
                        if (probe == null || string.IsNullOrWhiteSpace(probe.SerialNumber)) continue;
                        if (!calibrationIdByProbeSerial.TryGetValue(probe.SerialNumber, out var calId)) continue;
                        if (!bufferedSamplesByCalibration.TryGetValue(calId, out var list)) continue;
                        var timing = GetStepTiming(calId, stepNumber);
                        DataRecorder(probe, mirror, chamber, step, calId, stepNumber, timing, list);
                    }

                    if (sampleIndex < (SamplesPerStep - 1))
                    {
                        var waitUntil = nowUtc.Add(SampleInterval);
                        while (!IsDisposed && DateTime.UtcNow < waitUntil)
                        {
                            if (_closeRequested)
                                break;
                            Application.DoEvents();
                            UpdateSamplingUi();
                            System.Threading.Thread.Sleep(50);
                        }
                    }

                }

                EndSamplingUi();

                // Handle adjustment logic at end of sampling
                if (step.Adjust)
                {
                    if (string.Equals(step.Step, "Humidity", StringComparison.OrdinalIgnoreCase))
                    {
                        // Save humidity test point with reference value from mirror
                        foreach (var probe in rotProbes)
                        {
                            if (probe == null || string.IsNullOrWhiteSpace(probe.SerialNumber)) continue;
                            RotProbeCommands.SendHumidityTestPointSave(probe, mirror);
                        }
                    }
                    else if (string.Equals(step.Step, "Temperature", StringComparison.OrdinalIgnoreCase))
                    {
                        // Save temperature test point with reference value from mirror
                        foreach (var probe in rotProbes)
                        {
                            if (probe == null || string.IsNullOrWhiteSpace(probe.SerialNumber)) continue;
                            RotProbeCommands.SendTempTestPointSave(probe, mirror);
                        }
                    }
                }

                // Flush buffered samples to DB (one transaction). This keeps UI responsive by offloading work.
                var stepCopy = step;
                var stepNumberCopy = stepNumber;
                foreach (var kvp in bufferedSamplesByCalibration)
                {
                    var calIdCopy = kvp.Key;
                    var samplesCopy = kvp.Value;
                    var stepInsertionTiming = GetStepTiming(calIdCopy, stepNumberCopy);
                    Task.Run(() => FlushStepAndSamplesToDatabase(calIdCopy, stepCopy, stepNumberCopy, stepInsertionTiming, samplesCopy));
                }
            }

            // records final probe/mirror/chamber conditions at end of calibration
            if (!_closeRequested && !IsDisposed)
            {
                foreach (var probe in rotProbes)
                {
                    if (probe == null || string.IsNullOrWhiteSpace(probe.SerialNumber)) continue;
                    if (!calibrationIdByProbeSerial.TryGetValue(probe.SerialNumber, out var calId)) continue;
                    DataRecorderSnapShotEnd(probe, mirror, chamber, calId);
                }
            }

            _calibrationRunning = false;

            lock (_calibrationIdsLock)
            {
                _activeCalibrationIds.Clear();
            }

            RefreshCalProgressGrid();
        }

        private void FlushStepAndSamplesToDatabase(string calibrationId, StepClass step, int stepNumber, StepTiming timing, List<SampleRecord> bufferedSamples)
        {
            if (string.IsNullOrWhiteSpace(calibrationId) || step == null || stepNumber <= 0 || bufferedSamples == null)
                return;

            var preserveFirstDisplayedRow = -1;
            var preserveFirstDisplayedCol = -1;
            var preserveSelectedRowIndex = -1;

            try
            {
                if (dataGridViewCalProgress.DataSource != null)
                {
                    preserveFirstDisplayedRow = dataGridViewCalProgress.FirstDisplayedScrollingRowIndex;
                    preserveFirstDisplayedCol = dataGridViewCalProgress.FirstDisplayedScrollingColumnIndex;
                    if (dataGridViewCalProgress.CurrentCell != null)
                        preserveSelectedRowIndex = dataGridViewCalProgress.CurrentCell.RowIndex;
                }
            }
            catch { }

            try
            {
                var dbPath = Data.GetDatabasePath();
                var connStr = string.Format(CultureInfo.InvariantCulture, "Data Source={0};Version=3;Foreign Keys=True;", dbPath);

                using (var conn = new SQLiteConnection(connStr))
                {
                    conn.Open();
                    using (var tx = conn.BeginTransaction())
                    {
                        // Ensure Step exists
                        if (!TryGetStepId(calibrationId, stepNumber, out var stepId))
                        {
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = @"
INSERT INTO Step (
    CalibrationId,
    StepNumber,
    StepName,
    HumiditySetpoint,
    TemperatureSetpointC,
    Accuracy,
    Adjustment,
    RampStartUtc,
    SoakStartUtc,
    SoakEndUtc
) VALUES (
    @CalibrationId,
    @StepNumber,
    @StepName,
    @HumiditySetpoint,
    @TemperatureSetpointC,
    @Accuracy,
    @Adjustment,
    @RampStartUtc,
    @SoakStartUtc,
    @SoakEndUtc
);
SELECT last_insert_rowid();";
                                cmd.Parameters.AddWithValue("@CalibrationId", calibrationId);
                                cmd.Parameters.AddWithValue("@StepNumber", stepNumber);
                                cmd.Parameters.AddWithValue("@StepName", (object)step.Step ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@HumiditySetpoint", step.SetPointRH);
                                cmd.Parameters.AddWithValue("@TemperatureSetpointC", step.SetPointTemp);
                                cmd.Parameters.AddWithValue("@Accuracy", step.Accuracy);
                                cmd.Parameters.AddWithValue("@Adjustment", step.Adjust ? 1 : 0);
                                cmd.Parameters.AddWithValue("@RampStartUtc", (object)ToIsoUtcOrNull(timing != null ? timing.RampStartUtc : null) ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@SoakStartUtc", (object)ToIsoUtcOrNull(timing != null ? timing.SoakStartUtc : null) ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@SoakEndUtc", (object)ToIsoUtcOrNull(timing != null ? timing.SoakEndUtc : null) ?? DBNull.Value);
                                stepId = Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
                            }
                            CacheStepId(calibrationId, stepNumber, stepId);
                        }

                        DateTime? firstUtc = null;
                        DateTime? lastUtc = null;

                        using (var insertCmd = conn.CreateCommand())
                        {
                            insertCmd.CommandText = @"
INSERT INTO Sample (
    StepId,
    CalibrationId,
    SampleUtc,
    ProbeHumidity,
    ProbeHumidityCount,
    ProbeHumidityRaw,
    ProbeTemperatureC,
    ProbeTemperatureCount,
    ProbeResistance,
    MirrorDewPointC,
    MirrorFrostPointC,
    MirrorHumidity,
    ExternalTemperatureC,
    MirrorTemperatureC,
    ChamberTemperatureC,
    ChamberTemperatureSetpointC,
    ChamberHumidity,
    ChamberHumiditySetpoint
) VALUES (
    @StepId,
    @CalibrationId,
    @SampleUtc,
    @ProbeHumidity,
    @ProbeHumidityCount,
    @ProbeHumidityRaw,
    @ProbeTemperatureC,
    @ProbeTemperatureCount,
    @ProbeResistance,
    @MirrorDewPointC,
    @MirrorFrostPointC,
    @MirrorHumidity,
    @ExternalTemperatureC,
    @MirrorTemperatureC,
    @ChamberTemperatureC,
    @ChamberTemperatureSetpointC,
    @ChamberHumidity,
    @ChamberHumiditySetpoint
);";

                            var pStepId = insertCmd.CreateParameter(); pStepId.ParameterName = "@StepId"; insertCmd.Parameters.Add(pStepId);
                            var pCalId = insertCmd.CreateParameter(); pCalId.ParameterName = "@CalibrationId"; insertCmd.Parameters.Add(pCalId);
                            var pUtc = insertCmd.CreateParameter(); pUtc.ParameterName = "@SampleUtc"; insertCmd.Parameters.Add(pUtc);
                            var pPH = insertCmd.CreateParameter(); pPH.ParameterName = "@ProbeHumidity"; insertCmd.Parameters.Add(pPH);
                            var pPHC = insertCmd.CreateParameter(); pPHC.ParameterName = "@ProbeHumidityCount"; insertCmd.Parameters.Add(pPHC);
                            var pPHR = insertCmd.CreateParameter(); pPHR.ParameterName = "@ProbeHumidityRaw"; insertCmd.Parameters.Add(pPHR);
                            var pPT = insertCmd.CreateParameter(); pPT.ParameterName = "@ProbeTemperatureC"; insertCmd.Parameters.Add(pPT);
                            var pPTC = insertCmd.CreateParameter(); pPTC.ParameterName = "@ProbeTemperatureCount"; insertCmd.Parameters.Add(pPTC);
                            var pPR = insertCmd.CreateParameter(); pPR.ParameterName = "@ProbeResistance"; insertCmd.Parameters.Add(pPR);
                            var pMDP = insertCmd.CreateParameter(); pMDP.ParameterName = "@MirrorDewPointC"; insertCmd.Parameters.Add(pMDP);
                            var pMFP = insertCmd.CreateParameter(); pMFP.ParameterName = "@MirrorFrostPointC"; insertCmd.Parameters.Add(pMFP);
                            var pMH = insertCmd.CreateParameter(); pMH.ParameterName = "@MirrorHumidity"; insertCmd.Parameters.Add(pMH);
                            var pET = insertCmd.CreateParameter(); pET.ParameterName = "@ExternalTemperatureC"; insertCmd.Parameters.Add(pET);
                            var pMT = insertCmd.CreateParameter(); pMT.ParameterName = "@MirrorTemperatureC"; insertCmd.Parameters.Add(pMT);
                            var pCT = insertCmd.CreateParameter(); pCT.ParameterName = "@ChamberTemperatureC"; insertCmd.Parameters.Add(pCT);
                            var pCTS = insertCmd.CreateParameter(); pCTS.ParameterName = "@ChamberTemperatureSetpointC"; insertCmd.Parameters.Add(pCTS);
                            var pCH = insertCmd.CreateParameter(); pCH.ParameterName = "@ChamberHumidity"; insertCmd.Parameters.Add(pCH);
                            var pCHS = insertCmd.CreateParameter(); pCHS.ParameterName = "@ChamberHumiditySetpoint"; insertCmd.Parameters.Add(pCHS);

                            foreach (var s in bufferedSamples)
                            {
                                var utcIso = s.SampleUtc.ToString("o", CultureInfo.InvariantCulture);
                                if (!firstUtc.HasValue || s.SampleUtc < firstUtc.Value) firstUtc = s.SampleUtc;
                                if (!lastUtc.HasValue || s.SampleUtc > lastUtc.Value) lastUtc = s.SampleUtc;

                                pStepId.Value = stepId;
                                pCalId.Value = calibrationId;
                                pUtc.Value = utcIso;
                                pPH.Value = (object)s.ProbeHumidity ?? DBNull.Value;
                                pPHC.Value = (object)s.ProbeHumidityCount ?? DBNull.Value;
                                pPHR.Value = (object)s.ProbeHumidityRaw ?? DBNull.Value;
                                pPT.Value = (object)s.ProbeTemperatureC ?? DBNull.Value;
                                pPTC.Value = (object)s.ProbeTemperatureCount ?? DBNull.Value;
                                pPR.Value = (object)s.ProbeResistance ?? DBNull.Value;
                                pMDP.Value = (object)s.MirrorDewPointC ?? DBNull.Value;
                                pMFP.Value = (object)s.MirrorFrostPointC ?? DBNull.Value;
                                pMH.Value = (object)s.MirrorHumidity ?? DBNull.Value;
                                pET.Value = (object)s.ExternalTemperatureC ?? DBNull.Value;
                                pMT.Value = (object)s.MirrorTemperatureC ?? DBNull.Value;
                                pCT.Value = (object)s.ChamberTemperatureC ?? DBNull.Value;
                                pCTS.Value = (object)s.ChamberTemperatureSetpointC ?? DBNull.Value;
                                pCH.Value = (object)s.ChamberHumidity ?? DBNull.Value;
                                pCHS.Value = (object)s.ChamberHumiditySetpoint ?? DBNull.Value;

                                insertCmd.ExecuteNonQuery();
                            }
                        }

                        if (firstUtc.HasValue && lastUtc.HasValue)
                        {
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = @"
UPDATE Step
SET
    FirstSampleUtc = COALESCE(FirstSampleUtc, @FirstSampleUtc),
    LastSampleUtc = @LastSampleUtc
WHERE StepId = @StepId;";
                                cmd.Parameters.AddWithValue("@FirstSampleUtc", firstUtc.Value.ToString("o", CultureInfo.InvariantCulture));
                                cmd.Parameters.AddWithValue("@LastSampleUtc", lastUtc.Value.ToString("o", CultureInfo.InvariantCulture));
                                cmd.Parameters.AddWithValue("@StepId", stepId);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        tx.Commit();
                    }
                }

                try
                {
                    if (!IsDisposed)
                        BeginInvoke((Action)RefreshCalProgressGrid);
                }
                catch { }
            }
            catch
            {
                // Swallow to avoid crashing background thread; UI already manages user feedback elsewhere.
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