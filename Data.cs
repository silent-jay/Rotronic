using System;
using System.Globalization;
using System.IO;
using System.Data.SQLite;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Collections.Generic;
using System.Data;

namespace Rotronic
{
    internal class Data
    {
        private const string DatabaseFileName = "rotronic.db";

        
        public static string GetDatabasePath()
        {
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var dataDir = Path.Combine(exeDir, "Data");
            Directory.CreateDirectory(dataDir);
            return Path.Combine(dataDir, DatabaseFileName);
        }

        private static string TryParseDdMmmYyyyToIsoUtcOrNull(string ddMmmYyyy)
        {
            if (string.IsNullOrWhiteSpace(ddMmmYyyy))
                return null;

            var t = ddMmmYyyy.Trim();
            if (string.Equals(t, "DD-MMM-YYYY", StringComparison.OrdinalIgnoreCase) || string.Equals(t, "__-___-____", StringComparison.OrdinalIgnoreCase))
                return null;

            if (!DateTime.TryParseExact(
                t,
                "dd-MMM-yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var dt))
            {
                return null;
            }

            // Store as ISO UTC, consistent with CalProgressFrm.
            dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            return dt.ToString("o", CultureInfo.InvariantCulture);
        }

        public static void UpdateMirrorInventory(string serialNumber, string name, string lastCalibrationDdMmmYyyy, string nextDueDdMmmYyyy)
        {
            if (string.IsNullOrWhiteSpace(serialNumber))
                throw new ArgumentException("Mirror serial number is required.", nameof(serialNumber));

            var lastUtc = TryParseDdMmmYyyyToIsoUtcOrNull(lastCalibrationDdMmmYyyy);
            var nextUtc = TryParseDdMmmYyyyToIsoUtcOrNull(nextDueDdMmmYyyy);

            using (var conn = new SQLiteConnection(GetConnectionString()))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"UPDATE Mirror
SET Name = @Name,
    LastCalibrationUtc = @LastCalibrationUtc,
    NextDueUtc = @NextDueUtc
WHERE SerialNumber = @SerialNumber;";

                    cmd.Parameters.AddWithValue("@SerialNumber", serialNumber.Trim());
                    cmd.Parameters.AddWithValue("@Name", string.IsNullOrWhiteSpace(name) ? (object)DBNull.Value : name.Trim());
                    cmd.Parameters.AddWithValue("@LastCalibrationUtc", string.IsNullOrWhiteSpace(lastUtc) ? (object)DBNull.Value : lastUtc);
                    cmd.Parameters.AddWithValue("@NextDueUtc", string.IsNullOrWhiteSpace(nextUtc) ? (object)DBNull.Value : nextUtc);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static string GetChamberControllerSerialByHC2Serial(string hc2SerialNumber)
        {
            if (string.IsNullOrWhiteSpace(hc2SerialNumber))
                return null;

            using (var conn = new SQLiteConnection(GetConnectionString()))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT ControllerSerialNumber FROM Chamber WHERE HC2SerialNumber = @HC2SerialNumber LIMIT 1;";
                    cmd.Parameters.AddWithValue("@HC2SerialNumber", hc2SerialNumber.Trim());
                    var v = cmd.ExecuteScalar();
                    if (v == null || v == DBNull.Value)
                        return null;
                    return v.ToString();
                }
            }
        }

        public static void UpdateChamberInventory(string hc2SerialNumber, string controlProbeCalibrationDdMmmYyyy, string controlProbeNextDueDdMmmYyyy)
        {
            if (string.IsNullOrWhiteSpace(hc2SerialNumber))
                throw new ArgumentException("HC2 serial number is required.", nameof(hc2SerialNumber));

            var calUtc = TryParseDdMmmYyyyToIsoUtcOrNull(controlProbeCalibrationDdMmmYyyy);
            var dueUtc = TryParseDdMmmYyyyToIsoUtcOrNull(controlProbeNextDueDdMmmYyyy);

            using (var conn = new SQLiteConnection(GetConnectionString()))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"UPDATE Chamber
SET ControlProbeCalibrationUtc = @ControlProbeCalibrationUtc,
    ControlProbeNextDueUtc = @ControlProbeNextDueUtc
WHERE HC2SerialNumber = @HC2SerialNumber;";

                    cmd.Parameters.AddWithValue("@HC2SerialNumber", hc2SerialNumber.Trim());
                    cmd.Parameters.AddWithValue("@ControlProbeCalibrationUtc", string.IsNullOrWhiteSpace(calUtc) ? (object)DBNull.Value : calUtc);
                    cmd.Parameters.AddWithValue("@ControlProbeNextDueUtc", string.IsNullOrWhiteSpace(dueUtc) ? (object)DBNull.Value : dueUtc);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        internal sealed class MirrorRow
        {
            public string Name { get; set; }
            public string SerialNumber { get; set; }
            public string LastCalibrationUtc { get; set; }
            public string NextDueUtc { get; set; }
        }

        internal sealed class ChamberRow
        {
            public string Name { get; set; }
            public string HC2SerialNumber { get; set; }
            public string ControlProbeCalibrationUtc { get; set; }
            public string ControlProbeNextDueUtc { get; set; }
        }

        public static IList<MirrorRow> GetMirrors()
        {
            var mirrors = new List<MirrorRow>();

            using (var conn = new SQLiteConnection(GetConnectionString()))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Name, SerialNumber, LastCalibrationUtc, NextDueUtc FROM Mirror ORDER BY COALESCE(Name, ''), SerialNumber;";
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            mirrors.Add(new MirrorRow
                            {
                                Name = r.IsDBNull(0) ? null : r.GetString(0),
                                SerialNumber = r.IsDBNull(1) ? null : r.GetString(1),
                                LastCalibrationUtc = r.IsDBNull(2) ? null : r.GetString(2),
                                NextDueUtc = r.IsDBNull(3) ? null : r.GetString(3)
                            });
                        }
                    }
                }
            }

            return mirrors;
        }

        public static IList<ChamberRow> GetChambers()
        {
            var chambers = new List<ChamberRow>();

            using (var conn = new SQLiteConnection(GetConnectionString()))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Name, HC2SerialNumber, ControlProbeCalibrationUtc, ControlProbeNextDueUtc FROM Chamber ORDER BY COALESCE(Name, ''), HC2SerialNumber;";
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            chambers.Add(new ChamberRow
                            {
                                Name = r.IsDBNull(0) ? null : r.GetString(0),
                                HC2SerialNumber = r.IsDBNull(1) ? null : r.GetString(1),
                                ControlProbeCalibrationUtc = r.IsDBNull(2) ? null : r.GetString(2),
                                ControlProbeNextDueUtc = r.IsDBNull(3) ? null : r.GetString(3)
                            });
                        }
                    }
                }
            }

            return chambers;
        }

        public static MirrorRow GetMirrorBySerial(string serialNumber)
        {
            if (string.IsNullOrWhiteSpace(serialNumber))
                return null;

            serialNumber = serialNumber.Trim();

            using (var conn = new SQLiteConnection(GetConnectionString()))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Name, SerialNumber, LastCalibrationUtc, NextDueUtc FROM Mirror WHERE SerialNumber = @SerialNumber LIMIT 1;";
                    cmd.Parameters.AddWithValue("@SerialNumber", serialNumber);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read())
                            return null;

                        return new MirrorRow
                        {
                            Name = r.IsDBNull(0) ? null : r.GetString(0),
                            SerialNumber = r.IsDBNull(1) ? null : r.GetString(1),
                            LastCalibrationUtc = r.IsDBNull(2) ? null : r.GetString(2),
                            NextDueUtc = r.IsDBNull(3) ? null : r.GetString(3)
                        };
                    }
                }
            }
        }

        public static ChamberRow GetChamberByHC2Serial(string hc2SerialNumber)
        {
            if (string.IsNullOrWhiteSpace(hc2SerialNumber))
                return null;

            hc2SerialNumber = hc2SerialNumber.Trim();

            using (var conn = new SQLiteConnection(GetConnectionString()))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Name, HC2SerialNumber, ControlProbeCalibrationUtc, ControlProbeNextDueUtc FROM Chamber WHERE HC2SerialNumber = @HC2SerialNumber LIMIT 1;";
                    cmd.Parameters.AddWithValue("@HC2SerialNumber", hc2SerialNumber);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read())
                            return null;

                        return new ChamberRow
                        {
                            Name = r.IsDBNull(0) ? null : r.GetString(0),
                            HC2SerialNumber = r.IsDBNull(1) ? null : r.GetString(1),
                            ControlProbeCalibrationUtc = r.IsDBNull(2) ? null : r.GetString(2),
                            ControlProbeNextDueUtc = r.IsDBNull(3) ? null : r.GetString(3)
                        };
                    }
                }
            }
        }

        public static bool MirrorExists(string serialNumber)
        {
            if (string.IsNullOrWhiteSpace(serialNumber))
                return false;

            using (var conn = new SQLiteConnection(GetConnectionString()))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT 1 FROM Mirror WHERE SerialNumber = @SerialNumber LIMIT 1;";
                    cmd.Parameters.AddWithValue("@SerialNumber", serialNumber.Trim());
                    var v = cmd.ExecuteScalar();
                    return v != null && v != DBNull.Value;
                }
            }
        }

        public static bool ChamberExists(string controllerSerialNumber)
        {
            if (string.IsNullOrWhiteSpace(controllerSerialNumber))
                return false;

            using (var conn = new SQLiteConnection(GetConnectionString()))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT 1 FROM Chamber WHERE ControllerSerialNumber = @ControllerSerialNumber LIMIT 1;";
                    cmd.Parameters.AddWithValue("@ControllerSerialNumber", controllerSerialNumber.Trim());
                    var v = cmd.ExecuteScalar();
                    return v != null && v != DBNull.Value;
                }
            }
        }

        public static void UpsertMirror(string serialNumber, string name = null)
        {
            if (string.IsNullOrWhiteSpace(serialNumber))
                throw new ArgumentException("Mirror serial number is required.", nameof(serialNumber));

            using (var conn = new SQLiteConnection(GetConnectionString()))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO Mirror (SerialNumber, Name)
VALUES (@SerialNumber, @Name)
ON CONFLICT(SerialNumber) DO UPDATE SET Name = COALESCE(excluded.Name, Mirror.Name);";
                    cmd.Parameters.AddWithValue("@SerialNumber", serialNumber.Trim());
                    cmd.Parameters.AddWithValue("@Name", string.IsNullOrWhiteSpace(name) ? (object)DBNull.Value : name.Trim());
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void UpsertChamber(string controllerSerialNumber, string hc2SerialNumber = null, string name = null)
        {
            if (string.IsNullOrWhiteSpace(controllerSerialNumber))
                throw new ArgumentException("Chamber controller serial number is required.", nameof(controllerSerialNumber));

            using (var conn = new SQLiteConnection(GetConnectionString()))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO Chamber (ControllerSerialNumber, Name, HC2SerialNumber)
VALUES (@ControllerSerialNumber, @Name, @HC2SerialNumber)
ON CONFLICT(ControllerSerialNumber) DO UPDATE SET
    Name = COALESCE(excluded.Name, Chamber.Name),
    HC2SerialNumber = COALESCE(excluded.HC2SerialNumber, Chamber.HC2SerialNumber);";
                    cmd.Parameters.AddWithValue("@ControllerSerialNumber", controllerSerialNumber.Trim());
                    cmd.Parameters.AddWithValue("@Name", string.IsNullOrWhiteSpace(name) ? (object)DBNull.Value : name.Trim());
                    cmd.Parameters.AddWithValue("@HC2SerialNumber", string.IsNullOrWhiteSpace(hc2SerialNumber) ? (object)DBNull.Value : hc2SerialNumber.Trim());
                    cmd.ExecuteNonQuery();
                }
            }
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
    Name TEXT NULL,
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

CREATE TABLE IF NOT EXISTS Procedure (
    ProcedureId INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Step TEXT NOT NULL,
    HumiditySetpoint REAL NULL,
    TemperatureSetpointC REAL NULL,
    SoakTime TEXT NULL,
    Accuracy REAL NULL,
    Adjustment INTEGER NULL,
    Description TEXT NULL
);

CREATE INDEX IF NOT EXISTS IX_Sample_StepId_SampleUtc ON Sample(StepId, SampleUtc);
CREATE INDEX IF NOT EXISTS IX_Sample_CalibrationId_SampleUtc ON Sample(CalibrationId, SampleUtc);
";
                    cmd.ExecuteNonQuery();
                }

                EnsureSampleSchema(conn);

            }
        }

        private static void EnsureSampleSchema(SQLiteConnection conn)
        {
            // If the DB already existed, CREATE TABLE IF NOT EXISTS won't add newly introduced columns.
            // CalProgressFrm writes Sample rows using these columns, so ensure they exist.
            EnsureColumn(conn, "Sample", "StepId", "INTEGER NOT NULL");
            EnsureColumn(conn, "Sample", "CalibrationId", "TEXT NOT NULL");
            EnsureColumn(conn, "Sample", "SampleUtc", "TEXT NOT NULL");

            EnsureColumn(conn, "Sample", "ProbeHumidity", "REAL NULL");
            EnsureColumn(conn, "Sample", "ProbeHumidityCount", "INTEGER NULL");
            EnsureColumn(conn, "Sample", "ProbeHumidityRaw", "REAL NULL");
            EnsureColumn(conn, "Sample", "ProbeTemperatureC", "REAL NULL");
            EnsureColumn(conn, "Sample", "ProbeTemperatureCount", "INTEGER NULL");
            EnsureColumn(conn, "Sample", "ProbeResistance", "REAL NULL");

            EnsureColumn(conn, "Sample", "MirrorDewPointC", "REAL NULL");
            EnsureColumn(conn, "Sample", "MirrorFrostPointC", "REAL NULL");
            EnsureColumn(conn, "Sample", "MirrorHumidity", "REAL NULL");
            EnsureColumn(conn, "Sample", "ExternalTemperatureC", "REAL NULL");
            EnsureColumn(conn, "Sample", "MirrorTemperatureC", "REAL NULL");

            EnsureColumn(conn, "Sample", "ChamberTemperatureC", "REAL NULL");
            EnsureColumn(conn, "Sample", "ChamberTemperatureSetpointC", "REAL NULL");
            EnsureColumn(conn, "Sample", "ChamberHumidity", "REAL NULL");
            EnsureColumn(conn, "Sample", "ChamberHumiditySetpoint", "REAL NULL");
        }

        private static void EnsureColumn(SQLiteConnection conn, string tableName, string columnName, string columnSqlType)
        {
            if (conn == null)
                throw new ArgumentNullException(nameof(conn));
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name is required.", nameof(tableName));
            if (string.IsNullOrWhiteSpace(columnName))
                throw new ArgumentException("Column name is required.", nameof(columnName));
            if (string.IsNullOrWhiteSpace(columnSqlType))
                throw new ArgumentException("Column type is required.", nameof(columnSqlType));

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = string.Format(CultureInfo.InvariantCulture, "PRAGMA table_info({0});", tableName);
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        var name = r["name"] as string;
                        if (string.Equals(name, columnName, StringComparison.OrdinalIgnoreCase))
                            return;
                    }
                }
            }

            using (var alter = conn.CreateCommand())
            {
                alter.CommandText = string.Format(CultureInfo.InvariantCulture, "ALTER TABLE {0} ADD COLUMN {1} {2};", tableName, columnName, columnSqlType);
                alter.ExecuteNonQuery();
            }
        }

        internal sealed class ProcedureRow
        {
            public long ProcedureId { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Step { get; set; }
            public double? HumiditySetpoint { get; set; }
            public double? TemperatureSetpointC { get; set; }
            public string SoakTime { get; set; }
            public double? Accuracy { get; set; }
            public bool? Adjustment { get; set; }
        }

        public static IList<string> GetProcedureNames()
        {
            var names = new List<string>();

            using (var conn = new SQLiteConnection(GetConnectionString()))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT DISTINCT Name FROM Procedure ORDER BY Name;";
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            names.Add(r.IsDBNull(0) ? string.Empty : r.GetString(0));
                        }
                    }
                }
            }

            return names;
        }

        public static void DeleteProcedure(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Procedure name is required.", nameof(name));

            using (var conn = new SQLiteConnection(GetConnectionString()))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Procedure WHERE Name = @Name;";
                    cmd.Parameters.AddWithValue("@Name", name.Trim());
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static IList<ProcedureRow> GetProcedureRows(string name)
        {
            var rows = new List<ProcedureRow>();

            if (string.IsNullOrWhiteSpace(name))
                return rows;

            using (var conn = new SQLiteConnection(GetConnectionString()))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT ProcedureId, Name, Description, Step, HumiditySetpoint, TemperatureSetpointC, SoakTime, Accuracy, Adjustment
FROM Procedure
WHERE Name = @Name
ORDER BY ProcedureId;";
                    cmd.Parameters.AddWithValue("@Name", name.Trim());
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            rows.Add(new ProcedureRow
                            {
                                ProcedureId = r.GetInt64(0),
                                Name = r.IsDBNull(1) ? null : r.GetString(1),
                                Description = r.IsDBNull(2) ? null : r.GetString(2),
                                Step = r.IsDBNull(3) ? null : r.GetString(3),
                                HumiditySetpoint = r.IsDBNull(4) ? (double?)null : r.GetDouble(4),
                                TemperatureSetpointC = r.IsDBNull(5) ? (double?)null : r.GetDouble(5),
                                SoakTime = r.IsDBNull(6) ? null : r.GetString(6),
                                Accuracy = r.IsDBNull(7) ? (double?)null : r.GetDouble(7),
                                Adjustment = r.IsDBNull(8) ? (bool?)null : (r.GetInt32(8) != 0)
                            });
                        }
                    }
                }
            }

            return rows;
        }

        public static void SaveProcedure(string name, string description, IEnumerable<StepClass> steps)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Procedure name is required.", nameof(name));
            if (steps == null)
                throw new ArgumentNullException(nameof(steps));

            name = name.Trim();
            description = description ?? string.Empty;

            using (var conn = new SQLiteConnection(GetConnectionString()))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    using (var del = conn.CreateCommand())
                    {
                        del.Transaction = tx;
                        del.CommandText = "DELETE FROM Procedure WHERE Name = @Name;";
                        del.Parameters.AddWithValue("@Name", name);
                        del.ExecuteNonQuery();
                    }

                    using (var ins = conn.CreateCommand())
                    {
                        ins.Transaction = tx;
                        ins.CommandText = @"INSERT INTO Procedure (Name, Step, HumiditySetpoint, TemperatureSetpointC, SoakTime, Accuracy, Adjustment, Description)
VALUES (@Name, @Step, @HumiditySetpoint, @TemperatureSetpointC, @SoakTime, @Accuracy, @Adjustment, @Description);";

                        var pName = ins.Parameters.Add("@Name", System.Data.DbType.String);
                        var pStep = ins.Parameters.Add("@Step", System.Data.DbType.String);
                        var pHum = ins.Parameters.Add("@HumiditySetpoint", System.Data.DbType.Double);
                        var pTemp = ins.Parameters.Add("@TemperatureSetpointC", System.Data.DbType.Double);
                        var pSoak = ins.Parameters.Add("@SoakTime", System.Data.DbType.String);
                        var pAcc = ins.Parameters.Add("@Accuracy", System.Data.DbType.Double);
                        var pAdj = ins.Parameters.Add("@Adjustment", System.Data.DbType.Int32);
                        var pDesc = ins.Parameters.Add("@Description", System.Data.DbType.String);

                        foreach (var s in steps)
                        {
                            pName.Value = name;
                            pStep.Value = (s?.Step ?? string.Empty).Trim();

                            pHum.Value = (s != null && !double.IsNaN(s.SetPointRH)) ? (object)s.SetPointRH : DBNull.Value;
                            pTemp.Value = (s != null && !double.IsNaN(s.SetPointTemp)) ? (object)s.SetPointTemp : DBNull.Value;
                            pSoak.Value = (s?.SoakTime ?? string.Empty).Trim();
                            pAcc.Value = (s != null && !double.IsNaN(s.Accuracy)) ? (object)s.Accuracy : DBNull.Value;
                            pAdj.Value = (s != null && s.Adjust) ? 1 : 0;
                            pDesc.Value = description;

                            ins.ExecuteNonQuery();
                        }
                    }

                    tx.Commit();
                }
            }
        }
        public static void DefaultStepsInDatabase()
        {
            // TBI
            // Still need to create table in database for CalibrationProcedures.
            // Use this function to pre-populate with validated default procedures.

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