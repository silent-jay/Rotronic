using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Data;

namespace Rotronic
{
    internal class InitialSeed
    {
        public static void SeedDefaultProcedures(SQLiteConnection conn)
        {
            if (conn == null)
                throw new ArgumentNullException(nameof(conn));

            if (conn.State != ConnectionState.Open)
                throw new InvalidOperationException("Database connection must be open.");

            if (ProcedureCount(conn) > 0)
                return;

            SeedProcedure(conn, "Advanced Temperature Adjustment", string.Empty,
                new StepClass { Step = "AdvancedTempStart", SetPointRH = double.NaN, SetPointTemp = double.NaN, SoakTime = string.Empty, Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 0d, SoakTime = "00:15", Accuracy = 0d, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 12.5d, SoakTime = "00:15", Accuracy = 0d, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 25d, SoakTime = "00:15", Accuracy = 0d, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 37.5d, SoakTime = "00:15", Accuracy = 0d, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 50d, SoakTime = "00:15", Accuracy = 0d, Adjust = false },
                new StepClass { Step = "AdvancedTempEnd", SetPointRH = double.NaN, SetPointTemp = double.NaN, SoakTime = string.Empty, Accuracy = double.NaN, Adjust = false });

            SeedProcedure(conn, "Temperature And Humidity", "Collects data for temperature and humidity. Can be used for as-found or as-left readings. Markers may be added (as-found/as-left start/end) to make data review easier, if desired.",
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 5d, SoakTime = "00:15", Accuracy = 0d, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 25d, SoakTime = "00:15", Accuracy = 0d, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 50d, SoakTime = "00:15", Accuracy = 0d, Adjust = false },
                new StepClass { Step = "Humidity", SetPointRH = 10d, SetPointTemp = 23d, SoakTime = "00:15", Accuracy = 0d, Adjust = false },
                new StepClass { Step = "Humidity", SetPointRH = 50d, SetPointTemp = 23d, SoakTime = "00:15", Accuracy = 0d, Adjust = false },
                new StepClass { Step = "Humidity", SetPointRH = 90d, SetPointTemp = 23d, SoakTime = "00:15", Accuracy = 0d, Adjust = false });

            SeedProcedure(conn, "Temperature Only", "Calibration for temperature only. Can be used for as left or as found measurement. Markers for left/found can be added if desired.",
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 5d, SoakTime = "00:15", Accuracy = 0d, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 25d, SoakTime = "00:15", Accuracy = 0d, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 50d, SoakTime = "00:15", Accuracy = 0d, Adjust = false },
                new StepClass { Step = "Adjust", SetPointRH = double.NaN, SetPointTemp = double.NaN, SoakTime = string.Empty, Accuracy = double.NaN, Adjust = false });

            SeedProcedure(conn, "Humidity Only", "Humidity Calibration only. Can be used for as left or as found data. Add found/left markers as desired.",
                new StepClass { Step = "Humidity", SetPointRH = 10d, SetPointTemp = 23d, SoakTime = "00:15", Accuracy = 0d, Adjust = false },
                new StepClass { Step = "Humidity", SetPointRH = 50d, SetPointTemp = 23d, SoakTime = "00:15", Accuracy = 0d, Adjust = false },
                new StepClass { Step = "Humidity", SetPointRH = 90d, SetPointTemp = 23d, SoakTime = "00:15", Accuracy = 0d, Adjust = false });

            SeedProcedure(conn, "Adjust-Temperature, single point", "Performs single point adjustment on probe's temperature detection. This is the default method rotronic typically uses, and is good for probes in relatively stable environments. As-Found data should be taken before running this, unless it is an initial calibration.",
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 23d, SoakTime = "00:15", Accuracy = double.NaN, Adjust = true });

            SeedProcedure(conn, "Adjust-Humidity", "Performs multipoint adjustment on humidity accuracy. Any temperature adjustments should be done before running this procedure. As-Found data should be taken before running this, unless it is an initial calibration.",
                new StepClass { Step = "Humidity", SetPointRH = 10d, SetPointTemp = 23d, SoakTime = "01:00", Accuracy = double.NaN, Adjust = true },
                new StepClass { Step = "Humidity", SetPointRH = 50d, SetPointTemp = 23d, SoakTime = "01:00", Accuracy = double.NaN, Adjust = true },
                new StepClass { Step = "Humidity", SetPointRH = 90d, SetPointTemp = 23d, SoakTime = "01:00", Accuracy = double.NaN, Adjust = true },
                new StepClass { Step = "Adjust", SetPointRH = double.NaN, SetPointTemp = double.NaN, SoakTime = string.Empty, Accuracy = double.NaN, Adjust = false });

            SeedProcedure(conn, "Adjust, Temperature Single point, Humidity multipoint", "Performs adjustment to temperature, and then humidity to minimize temperature accuracy affects on humidity accuracy. As-Found data should be taken before running this, unless it is an initial calibration.",
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 23d, SoakTime = "00:15", Accuracy = double.NaN, Adjust = true },
                new StepClass { Step = "Adjust", SetPointRH = double.NaN, SetPointTemp = double.NaN, SoakTime = string.Empty, Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "Humidity", SetPointRH = 10d, SetPointTemp = 23d, SoakTime = "01:00", Accuracy = double.NaN, Adjust = true },
                new StepClass { Step = "Humidity", SetPointRH = 50d, SetPointTemp = 23d, SoakTime = "01:00", Accuracy = double.NaN, Adjust = true },
                new StepClass { Step = "Humidity", SetPointRH = 90d, SetPointTemp = 23d, SoakTime = "01:00", Accuracy = double.NaN, Adjust = true },
                new StepClass { Step = "Adjust", SetPointRH = double.NaN, SetPointTemp = double.NaN, SoakTime = string.Empty, Accuracy = double.NaN, Adjust = false });

            SeedProcedure(conn, "Adjust, Advanced Temperature and Humidity", "Performs advanced temperature adjustment, creating new PT100 coefficients and upload to probe, Followed by Humidity adjustment. As-Found data should be taken before running this, unless it is an initial calibration.",
                new StepClass { Step = "AdvancedTempStart", SetPointRH = double.NaN, SetPointTemp = double.NaN, SoakTime = string.Empty, Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 0d, SoakTime = "00:15", Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 12.5d, SoakTime = "00:15", Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 25d, SoakTime = "00:15", Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 37.5d, SoakTime = "00:15", Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 50d, SoakTime = "00:15", Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "AdvancedTempEnd", SetPointRH = double.NaN, SetPointTemp = double.NaN, SoakTime = string.Empty, Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "Humidity", SetPointRH = 10d, SetPointTemp = 23d, SoakTime = "01:00", Accuracy = double.NaN, Adjust = true },
                new StepClass { Step = "Humidity", SetPointRH = 50d, SetPointTemp = 23d, SoakTime = "01:00", Accuracy = double.NaN, Adjust = true },
                new StepClass { Step = "Humidity", SetPointRH = 90d, SetPointTemp = 23d, SoakTime = "01:00", Accuracy = double.NaN, Adjust = true },
                new StepClass { Step = "Adjust", SetPointRH = double.NaN, SetPointTemp = double.NaN, SoakTime = string.Empty, Accuracy = double.NaN, Adjust = false });

            SeedProcedure(conn, "Adjustment, Advanced Temperature Coefficients", "Takes a minimum of 4 temperature points to generate new PT100 coefficients and offset, and uploads to probe. Points should be evenly distributed to avoid biasing the equation. More points will generate more accuracted A/B/C/R(0) values.",
                new StepClass { Step = "AdvancedTempStart", SetPointRH = double.NaN, SetPointTemp = double.NaN, SoakTime = string.Empty, Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 0d, SoakTime = "00:15", Accuracy = 0d, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 12.5d, SoakTime = "00:15", Accuracy = 0d, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 25d, SoakTime = "00:15", Accuracy = 0d, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 37.5d, SoakTime = "00:15", Accuracy = 0d, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 50d, SoakTime = "00:15", Accuracy = 0d, Adjust = false },
                new StepClass { Step = "AdvancedTempEnd", SetPointRH = double.NaN, SetPointTemp = double.NaN, SoakTime = string.Empty, Accuracy = double.NaN, Adjust = false });

            SeedProcedure(conn, "Full Calibration with single point temperature adjustment", "Full temperature and humdity calibration with single point temperature adjustment, multipoint humidity adjustment and markers for as-left and as-found start and end points.",
                new StepClass { Step = "As-FoundStart", SetPointRH = double.NaN, SetPointTemp = double.NaN, SoakTime = string.Empty, Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 5d, SoakTime = "00:15", Accuracy = 0d, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 25d, SoakTime = "00:15", Accuracy = 0d, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 50d, SoakTime = "00:15", Accuracy = 0d, Adjust = false },
                new StepClass { Step = "Humidity", SetPointRH = 10d, SetPointTemp = 23d, SoakTime = "00:15", Accuracy = 0d, Adjust = false },
                new StepClass { Step = "Humidity", SetPointRH = 50d, SetPointTemp = 23d, SoakTime = "00:15", Accuracy = 0d, Adjust = false },
                new StepClass { Step = "Humidity", SetPointRH = 90d, SetPointTemp = 23d, SoakTime = "00:15", Accuracy = 0d, Adjust = false },
                new StepClass { Step = "As-FoundEnd", SetPointRH = double.NaN, SetPointTemp = double.NaN, SoakTime = string.Empty, Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 23d, SoakTime = "00:15", Accuracy = 0d, Adjust = true },
                new StepClass { Step = "Adjust", SetPointRH = double.NaN, SetPointTemp = double.NaN, SoakTime = string.Empty, Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "Humidity", SetPointRH = 10d, SetPointTemp = 23d, SoakTime = "01:00", Accuracy = 0d, Adjust = true },
                new StepClass { Step = "Humidity", SetPointRH = 50d, SetPointTemp = 23d, SoakTime = "01:00", Accuracy = 0d, Adjust = true },
                new StepClass { Step = "Humidity", SetPointRH = 90d, SetPointTemp = 23d, SoakTime = "01:00", Accuracy = 0d, Adjust = true },
                new StepClass { Step = "Adjust", SetPointRH = double.NaN, SetPointTemp = double.NaN, SoakTime = string.Empty, Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "As-LeftStart", SetPointRH = double.NaN, SetPointTemp = double.NaN, SoakTime = string.Empty, Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 5d, SoakTime = "00:15", Accuracy = 0d, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 25d, SoakTime = "00:15", Accuracy = 0d, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 50d, SoakTime = "00:15", Accuracy = 0d, Adjust = false },
                new StepClass { Step = "Humidity", SetPointRH = 10d, SetPointTemp = 23d, SoakTime = "00:15", Accuracy = 0d, Adjust = false },
                new StepClass { Step = "Humidity", SetPointRH = 50d, SetPointTemp = 23d, SoakTime = "00:15", Accuracy = 0d, Adjust = false },
                new StepClass { Step = "Humidity", SetPointRH = 90d, SetPointTemp = 23d, SoakTime = "00:15", Accuracy = 0d, Adjust = false },
                new StepClass { Step = "As-LeftEnd", SetPointRH = double.NaN, SetPointTemp = double.NaN, SoakTime = string.Empty, Accuracy = double.NaN, Adjust = false });

            SeedProcedure(conn, "Full Calibration with advanced temperature adjustment", "Full range calibration with makers for as-left and as-found start and end points. Calculates PT100 coefficients from data and uploads to the probe.",
                new StepClass { Step = "As-FoundStart", SetPointRH = double.NaN, SetPointTemp = double.NaN, SoakTime = string.Empty, Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 5d, SoakTime = "00:15", Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 25d, SoakTime = "00:15", Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 50d, SoakTime = "00:15", Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "Humidity", SetPointRH = 10d, SetPointTemp = 23d, SoakTime = "00:15", Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "Humidity", SetPointRH = 50d, SetPointTemp = 23d, SoakTime = "00:15", Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "Humidity", SetPointRH = 90d, SetPointTemp = 23d, SoakTime = "00:15", Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "As-FoundEnd", SetPointRH = double.NaN, SetPointTemp = double.NaN, SoakTime = string.Empty, Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "AdvancedTempStart", SetPointRH = double.NaN, SetPointTemp = double.NaN, SoakTime = string.Empty, Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 0d, SoakTime = "00:15", Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 12.5d, SoakTime = "00:15", Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 25d, SoakTime = "00:15", Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 37.5d, SoakTime = "00:15", Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 50d, SoakTime = "00:15", Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "AdvancedTempEnd", SetPointRH = double.NaN, SetPointTemp = double.NaN, SoakTime = string.Empty, Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "Humidity", SetPointRH = 10d, SetPointTemp = 23d, SoakTime = "01:00", Accuracy = double.NaN, Adjust = true },
                new StepClass { Step = "Humidity", SetPointRH = 50d, SetPointTemp = 23d, SoakTime = "01:00", Accuracy = double.NaN, Adjust = true },
                new StepClass { Step = "Humidity", SetPointRH = 90d, SetPointTemp = 23d, SoakTime = "01:00", Accuracy = double.NaN, Adjust = true },
                new StepClass { Step = "Adjust", SetPointRH = double.NaN, SetPointTemp = double.NaN, SoakTime = string.Empty, Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "As-LeftStart", SetPointRH = double.NaN, SetPointTemp = double.NaN, SoakTime = string.Empty, Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 5d, SoakTime = "00:15", Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 25d, SoakTime = "00:15", Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "Temperature", SetPointRH = 20d, SetPointTemp = 50d, SoakTime = "00:15", Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "Humidity", SetPointRH = 10d, SetPointTemp = 23d, SoakTime = "00:15", Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "Humidity", SetPointRH = 50d, SetPointTemp = 23d, SoakTime = "00:15", Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "Humidity", SetPointRH = 90d, SetPointTemp = 23d, SoakTime = "00:15", Accuracy = double.NaN, Adjust = false },
                new StepClass { Step = "As-LeftEnd", SetPointRH = double.NaN, SetPointTemp = double.NaN, SoakTime = string.Empty, Accuracy = double.NaN, Adjust = false });
        }

        private static int ProcedureCount(SQLiteConnection conn)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM Procedure;";
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private static void SeedProcedure(SQLiteConnection conn, string name, string description, params StepClass[] steps)
        {
            using (var tx = conn.BeginTransaction())
            {
                long procedureId;

                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"INSERT INTO Procedure (Name, Description)
VALUES (@Name, @Description);";
                    cmd.Parameters.AddWithValue("@Name", name ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Description", (object)(description ?? string.Empty));
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = "SELECT last_insert_rowid();";
                    procedureId = (long)cmd.ExecuteScalar();
                }

                if (steps != null)
                {
                    foreach (var step in steps)
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = tx;
                            cmd.CommandText = @"INSERT INTO StepDef
(ProcedureId, Step, HumiditySetpoint, TemperatureSetpointC, SoakTime, Accuracy, Adjustment)
VALUES
(@ProcedureId, @Step, @HumiditySetpoint, @TemperatureSetpointC, @SoakTime, @Accuracy, @Adjustment);";

                            cmd.Parameters.AddWithValue("@ProcedureId", procedureId);
                            cmd.Parameters.AddWithValue("@Step", (step?.Step ?? string.Empty).Trim());
                            cmd.Parameters.AddWithValue("@HumiditySetpoint", GetDoubleOrDbNull(step?.SetPointRH));
                            cmd.Parameters.AddWithValue("@TemperatureSetpointC", GetDoubleOrDbNull(step?.SetPointTemp));
                            cmd.Parameters.AddWithValue("@SoakTime", (step?.SoakTime ?? string.Empty).Trim());
                            cmd.Parameters.AddWithValue("@Accuracy", GetDoubleOrDbNull(step?.Accuracy));
                            cmd.Parameters.AddWithValue("@Adjustment", step != null && step.Adjust ? 1 : 0);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                tx.Commit();
            }
        }

        private static object GetDoubleOrDbNull(double? value)
        {
            if (!value.HasValue || double.IsNaN(value.Value))
                return DBNull.Value;

            return value.Value;
        }
    }
}
