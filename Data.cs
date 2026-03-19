using System;
using System.Globalization;
using System.IO;
using System.Data.SQLite;

namespace Rotronic
{
    internal class Data
    {
        private const string DatabaseFileName = "rotronic.db";

        public static string GetDatabasePath()
        {
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appDir = Path.Combine(baseDir, "Rotronic");
            Directory.CreateDirectory(appDir);
            return Path.Combine(appDir, DatabaseFileName);
        }

        private static string GetConnectionString()
        {
            var path = GetDatabasePath();
            return string.Format(CultureInfo.InvariantCulture, "Data Source={0};Version=3;Foreign Keys=True;", path);
        }

        public static void InitializeDatabase()
        {
            using (var conn = new SQLiteConnection(GetConnectionString()))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA foreign_keys = ON;";
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"

CREATE TABLE IF NOT EXISTS Probe (
    SerialNumber TEXT NOT NULL PRIMARY KEY,
    ProbeType TEXT NULL,
    DeviceModel TEXT NULL,
    FirmwareVersion TEXT NULL,
    DeviceName TEXT NULL,
    DeviceType TEXT NULL,
    HumidityFactoryCorrection REAL NULL,
    HumidityUserCorrection REAL NULL,
    HumidityTemperatureCorrection REAL NULL,
    HumidityDriftCorrection REAL NULL,
    PT100CoeffA REAL NULL,
    PT100CoeffB REAL NULL,
    PT100CoeffC REAL NULL,
    TempOffset REAL NULL,
    TempConversion REAL NULL,
    LastCalibrationUtc TEXT NULL,
    NextDueUtc TEXT NULL
);

CREATE TABLE IF NOT EXISTS Mirror (
    SerialNumber TEXT NOT NULL PRIMARY KEY,
    ID TEXT NULL,
    IDN TEXT NULL,
    LastCalibrationUtc TEXT NULL,
    NextDueUtc TEXT NULL
);

CREATE TABLE IF NOT EXISTS Chamber (
    ControllerSerialNumber TEXT NOT NULL PRIMARY KEY,
    Name TEXT NULL,
    LastIpAddress TEXT NULL,
    HC2SerialNumber TEXT NULL,
    DessicantSerialNumber TEXT NULL,
    ControlProbeCalibrationUtc TEXT NULL,
    ControlProbeNextDueUtc TEXT NULL
);

CREATE TABLE IF NOT EXISTS Calibration (
    CalibrationId TEXT NOT NULL PRIMARY KEY,
    StartedUtc TEXT NOT NULL,
    EndedUtc TEXT NULL,
    OperatorName TEXT NULL,
    Notes TEXT NULL,
    ProbeSerialNumber TEXT NULL,
    MirrorSerialNumber TEXT NULL,
    ChamberControllerSerialNumber TEXT NULL,
    ProbeSnapshotStartJson TEXT NULL,
    ProbeSnapshotEndJson TEXT NULL,
    MirrorSnapshotStartJson TEXT NULL,
    MirrorSnapshotEndJson TEXT NULL,
    ChamberSnapshotStartJson TEXT NULL,
    ChamberSnapshotEndJson TEXT NULL,
    FOREIGN KEY(ProbeSerialNumber) REFERENCES Probe(SerialNumber),
    FOREIGN KEY(MirrorSerialNumber) REFERENCES Mirror(SerialNumber),
    FOREIGN KEY(ChamberControllerSerialNumber) REFERENCES Chamber(ControllerSerialNumber)
);

CREATE TABLE IF NOT EXISTS Step (
    StepId INTEGER PRIMARY KEY AUTOINCREMENT,
    CalibrationId TEXT NOT NULL,
    StepNumber INTEGER NOT NULL,
    StepName TEXT NULL,
    HumiditySetpoint REAL NULL,
    TemperatureSetpointC REAL NULL,
    Accuracy REAL NULL,
    Adjustment INTEGER NULL,
    RampStartUtc TEXT NULL,
    SoakStartUtc TEXT NULL,
    SoakEndUtc TEXT NULL,
    FirstSampleUtc TEXT NULL,
    LastSampleUtc TEXT NULL,
    FOREIGN KEY(CalibrationId) REFERENCES Calibration(CalibrationId) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS UX_Step_Calibration_StepNumber ON Step(CalibrationId, StepNumber);

CREATE TABLE IF NOT EXISTS Sample (
    SampleId INTEGER PRIMARY KEY AUTOINCREMENT,
    StepId INTEGER NOT NULL,
    CalibrationId TEXT NOT NULL,
    SampleUtc TEXT NOT NULL,
    ProbeHumidity REAL NULL,
    ProbeHumidityCount INTEGER NULL,
    ProbeHumidityRaw REAL NULL,
    ProbeTemperatureC REAL NULL,
    ProbeTemperatureCount INTEGER NULL,
    ProbeResistance REAL NULL,
    MirrorDewPointC REAL NULL,
    MirrorFrostPointC REAL NULL,
    MirrorHumidity REAL NULL,
    ExternalTemperatureC REAL NULL,
    MirrorTemperatureC REAL NULL,
    ChamberTemperatureC REAL NULL,
    ChamberTemperatureSetpointC REAL NULL,
    ChamberHumidity REAL NULL,
    ChamberHumiditySetpoint REAL NULL,
    FOREIGN KEY(StepId) REFERENCES Step(StepId) ON DELETE CASCADE,
    FOREIGN KEY(CalibrationId) REFERENCES Calibration(CalibrationId) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IX_Sample_StepId_SampleUtc ON Sample(StepId, SampleUtc);
CREATE INDEX IF NOT EXISTS IX_Sample_CalibrationId_SampleUtc ON Sample(CalibrationId, SampleUtc);
";
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
/* Database Tables/Criteria/Keys

        Probe table:
      - string ProbeType
      - double HumidityFactoryCorrection
      - double HumidityUserCorrection
      - double HumidityTemperatureCorrection
      - double HumidityDriftCorrection
      - double PT100CoeffA
      - double PT100CoeffB
      - double PT100CoeffC
      - double TempOffset
      - double TempConversion
      - string DeviceModel
      - string FirmwareVersion
      - string SerialNumber    ****Primary Key **** Unique
      - string DeviceName
      - char DeviceType
      - string ProbeAddress
      - date/time stamp of last calibration
      - date/time stamp of next due date, nominally 1 year after last calibration, but may be adjusted based on user input or other factors
 
     Mirror table:

      - string SerialNumber { get; set; }   ****Primary Key **** Unique
      - string ID { get; set; }
      - string IDN { get; set; }
      - date of last cal
      - due date

      Chamber table:
        - last IP address
        - HC2 Serial
        - Controller Serial - Primary Key
        - Dessicant Serial
        - Name
        - Control Probe Calibration Date
        - Control Probe Next Due Date

        Calibration Data table:
        * Calibration ID (Primary Key) - autogenerate based on date/time. Multiple probes can start calibrations at once
        * so needs to be robust enough to avoid collisions. Maybe YYYYMMDDHHMMSS### Format, where ### can be the rotprobe list index number
        * Date/Time start
        * Date/Time end
        * Operator Name
        * Notes
        *  - Probe /// Foreign key to probe table,       snapshot of data at start of calibration, and again at end of calibration as means of tracking cal constants, due dates, etc. Unsure if this should be a foreign key, or a static copy of the information since some data (probe name, calibration dates, etc may change over time. Static copy is better if foreign key will force it to reference updated data years later instead of what it was at the time of calibhration.)
        *  - Mirror /// Foreign key to mirror table,     snapshot of data at start of calibration, and again at end of calibration as means of tracking cal constants, due dates, etc. Unsure if this should be a foreign key, or a static copy of the information since some data (probe name, calibration dates, etc may change over time. Static copy is better if foreign key will force it to reference updated data years later instead of what it was at the time of calibhration.)
        *  - Chamber /// Foreign key to chamber table,   snapshot of data at start of calibration, and again at end of calibration as means of tracking cal constants, due dates, etc. Unsure if this should be a foreign key, or a static copy of the information since some data (probe name, calibration dates, etc may change over time. Static copy is better if foreign key will force it to reference updated data years later instead of what it was at the time of calibhration.)
        
        Step Table:
        * Step ID. Primary Key, GUID or autoincrement int.
        * Calibration ID. Foreign Key to Calibration table.
        * Step Number
        * Step Name (e.g. Humidity, Temperature)
        * Step Humidity Setpoint
        * Step Temperature Setpoint
        * Step Accuracy
        * date/time of ramp start
        * date/time of soak start
        * date/time of soak end/first sample
        * date/time of last sample
        
        Sample Table:
        * Sample ID. Primary Key, GUID or autoincrement int.
        * Step ID. Foreign Key to Step table.
        * Calibration ID. Foreign Key to Calibration table, for ease of querying.
        * Sample Date/Time
        * Probe Humidity
        * Probe Humidity Count
        * Probe Humdity Raw
        * Probe Temperature
        * Probe Temperature Count
        * Probe Resistance
        * Mirror Dew Point
        * Mirror Frost Point
        * Mirror Humidity
        * External Temperature
        * Mirror Temperature
        * Chamber Temperature
        * Chamber Temperature Setpoint
        * Chamber Humidity
        * Chamber Humidity Setpoint

// end of table criteria/keys


 * Info for database design:
 *             static data
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