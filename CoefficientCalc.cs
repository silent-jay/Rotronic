using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rotronic
{
    internal static class CoefficientCalc
    {
        /*
         * The probe temperature element is a PT100 RTD.
         * The coefficients are typically generic values for a Callendar-Van Dusen equation
         * R(T) = R0 * (1 + A*T + B*T^2 + C*(T-100)*T^3)
         * A = 0.003908299841 B = -5.77499974951934E-07 C =4.79382386195842E-21
         * R(0) = 100.0
         * The pdf in notes E-T-2P TempAdj_10 contains instructions for setting a 2 point temperature
         * linearization by only changing the A and offset variables. I've reverse engineered memory locations
         * for B and C, and created commands to write to those locations in RotProbeCommands.
         * Goal of the algorithm is to take at least 4 points of data with reference temperature and probe
         * resistance/count data and calculate coefficients unique to the probe rather than using the generic
         * factory values. The more points the better, but 4 is the minimum for a cubic equation.
         * 
         * Complicating factors are: the TempOffset is stored as counts/ohm on probe memory, and the
         * temperature chamber will have difficulty acheiving 0°C, so resistance and count at 0°C
         * must be extrapolated from the data based on shape of curve and best fit. The algorithm should be able to handle data sets that do not include 0°C, but including a point near 0°C will likely improve accuracy of the coefficients.
         * 
         * Take a look at the notes, it includes an example of how to calculate the offset and write to that memory
         * location. We aren't using that method, as it is for two point linearization, and assumes one the new offset
         * will NOT be anywhere near 0°C.
         * 
         * To summarize: Take the array of probe temp, probe temp count, probe resistance, and mirror temp, calculate coefficients, calculate offset, and return those values in result. Caller will then write them to probe memory.
         * 
         */

        internal sealed class Result
        {
            public Result(double r0, double a, double b, double c, double offset)
            {
                R0 = r0;
                A = a;
                B = b;
                C = c;
                Offset = offset;
            }

            public double R0 { get; }
            public double A { get; }
            public double B { get; }
            public double C { get; }
            public double Offset { get; }
        }

        public static Result Calculate(double[] probeTemp, double[] probeTempCount, double[] probeResistance, double[] mirrorTemp)
        {
            return Calculate(probeTemp, probeTempCount, probeResistance, mirrorTemp, double.NaN);
        }

        public static Result Calculate(double[] probeTemp, double[] probeTempCount, double[] probeResistance, double[] mirrorTemp, double tempConvCountsPerOhm)
        {
            if (probeTemp == null) throw new ArgumentNullException(nameof(probeTemp));
            if (probeTempCount == null) throw new ArgumentNullException(nameof(probeTempCount));
            if (probeResistance == null) throw new ArgumentNullException(nameof(probeResistance));
            if (mirrorTemp == null) throw new ArgumentNullException(nameof(mirrorTemp));

            if (probeTemp.Length != probeTempCount.Length || probeTemp.Length != probeResistance.Length || probeTemp.Length != mirrorTemp.Length)
                throw new ArgumentException("All input arrays must have the same length.");

            if (probeTemp.Length < 4)
                throw new ArgumentException("Advanced temperature adjustment requires at least 4 points.");

            // Solve PT100 Callendar–Van Dusen coefficients from reference temperatures (mirrorTemp, °C)
            // and measured resistance (probeResistance, Ω).
            //
            // For T >= 0 (typical use here):
            //   R(T) = R0 * (1 + A*T + B*T^2)
            // => R = x0 + x1*T + x2*T^2, where x0=R0, x1=R0*A, x2=R0*B
            //
            // If you ever include sub-zero points:
            //   R(T) = R0 * (1 + A*T + B*T^2 + C*(T-100)*T^3)
            // => R = x0 + x1*T + x2*T^2 + x3*((T-100)*T^3), where x3=R0*C

            bool hasNegative = false;
            for (int i = 0; i < mirrorTemp.Length; i++)
            {
                if (!double.IsNaN(mirrorTemp[i]) && mirrorTemp[i] < 0)
                {
                    hasNegative = true;
                    break;
                }
            }

            var fitR = hasNegative
                ? FitLinearLeastSquares(mirrorTemp, probeResistance, includeC: true)
                : FitLinearLeastSquares(mirrorTemp, probeResistance, includeC: false);

            var r0 = fitR[0];
            if (r0 == 0 || double.IsNaN(r0) || double.IsInfinity(r0))
                return new Result(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN);

            var a = fitR.Length > 1 ? (fitR[1] / r0) : double.NaN;
            var b = fitR.Length > 2 ? (fitR[2] / r0) : double.NaN;
            var c = fitR.Length > 3 ? (fitR[3] / r0) : 0.0;

            // --- Offset suggestion (counts/ohm) ---
            // AirChip relation from the PDF:
            //   Count = R * (TempConv + TempOffset)
            // => TempOffset = Count/R - TempConv
            //
            // If we can estimate Count at 0°C (Count0) and R at 0°C (R0), then:
            //   TempOffset ≈ (Count0 / R0) - TempConv
            //
            // Here we estimate Count0 by fitting the same basis to counts vs temperature.
            // TempConv is a fixed probe constant (counts/ohm). If not provided, offset will remain NaN.
            double offset = double.NaN;
            try
            {
                var fitCount = hasNegative
                    ? FitLinearLeastSquares(mirrorTemp, probeTempCount, includeC: true)
                    : FitLinearLeastSquares(mirrorTemp, probeTempCount, includeC: false);

                var count0 = fitCount[0];
                if (!double.IsNaN(tempConvCountsPerOhm) && !double.IsInfinity(tempConvCountsPerOhm) &&
                    !double.IsNaN(count0) && !double.IsInfinity(count0) &&
                    r0 != 0.0)
                {
                    offset = (count0 / r0) - tempConvCountsPerOhm;
                }
            }
            catch
            {
                offset = double.NaN;
            }

            return new Result(r0, a, b, c, offset);
        }

        private static double[] FitLinearLeastSquares(double[] tC, double[] rOhms, bool includeC)
        {
            // Build normal equations for X^T X and X^T y.
            // Parameter vector is [x0, x1, x2] or [x0, x1, x2, x3]
            int p = includeC ? 4 : 3;
            var xtx = new double[p, p];
            var xty = new double[p];

            int n = 0;
            for (int i = 0; i < tC.Length; i++)
            {
                var t = tC[i];
                var r = rOhms[i];
                if (double.IsNaN(t) || double.IsInfinity(t) || double.IsNaN(r) || double.IsInfinity(r))
                    continue;

                var phi0 = 1.0;
                var phi1 = t;
                var phi2 = t * t;

                double phi3 = 0.0;
                if (includeC)
                {
                    // (T - 100) * T^3
                    phi3 = (t - 100.0) * t * t * t;
                }

                var phi = includeC
                    ? new[] { phi0, phi1, phi2, phi3 }
                    : new[] { phi0, phi1, phi2 };

                for (int row = 0; row < p; row++)
                {
                    xty[row] += phi[row] * r;
                    for (int col = 0; col < p; col++)
                        xtx[row, col] += phi[row] * phi[col];
                }

                n++;
            }

            if (n < p)
                return includeC
                    ? new[] { double.NaN, double.NaN, double.NaN, double.NaN }
                    : new[] { double.NaN, double.NaN, double.NaN };

            return SolveLinearSystem(xtx, xty);
        }

        private static double[] SolveLinearSystem(double[,] a, double[] b)
        {
            // Gaussian elimination with partial pivoting.
            int n = b.Length;
            var m = new double[n, n + 1];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                    m[i, j] = a[i, j];
                m[i, n] = b[i];
            }

            for (int k = 0; k < n; k++)
            {
                int pivot = k;
                double max = Math.Abs(m[k, k]);
                for (int i = k + 1; i < n; i++)
                {
                    var v = Math.Abs(m[i, k]);
                    if (v > max)
                    {
                        max = v;
                        pivot = i;
                    }
                }

                if (max == 0.0 || double.IsNaN(max) || double.IsInfinity(max))
                    return CreateNaNs(n);

                if (pivot != k)
                {
                    for (int j = k; j <= n; j++)
                    {
                        var tmp = m[k, j];
                        m[k, j] = m[pivot, j];
                        m[pivot, j] = tmp;
                    }
                }

                var diag = m[k, k];
                for (int j = k; j <= n; j++)
                    m[k, j] /= diag;

                for (int i = 0; i < n; i++)
                {
                    if (i == k) continue;
                    var factor = m[i, k];
                    if (factor == 0.0) continue;
                    for (int j = k; j <= n; j++)
                        m[i, j] -= factor * m[k, j];
                }
            }

            var x = new double[n];
            for (int i = 0; i < n; i++)
                x[i] = m[i, n];
            return x;
        }

        private static double[] CreateNaNs(int length)
        {
            var a = new double[length];
            for (int i = 0; i < length; i++) a[i] = double.NaN;
            return a;
        }
    }
}
