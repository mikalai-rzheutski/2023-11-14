using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using WindowsFormsApplication2.Models.XRR;
using WindowsFormsApplication2.ViewModel;

namespace WindowsFormsApplication2.Models
{
    class Reflectance
    {
        const double Pi = 3.14;
        const double Re = 2.8179403227e-15;

        double wavelength;
        double hv;
        double k;

        public Reflectance(InstrumentSettings instrumentSettings)
        {
            wavelength = instrumentSettings.Wavelength;
            hv = 1.239841984e-9 / wavelength;
            k = 2 * Pi / wavelength;
        }

        public Dictionary<double, double> GetXRR(Sample1 sample, InstrumentSettings instrumentSettings, double twoThetaStart,
            double twoThetaEnd, int pointCnt)
        {
            var result = new Dictionary<double, double>();
            var scaling = instrumentSettings.Scaling;

            var step = (twoThetaEnd - twoThetaStart) / (pointCnt - 1);
            double resolution = instrumentSettings.Resolution;
            int addedPointCnt = (int)Math.Ceiling(3 * resolution / step);
            double twoTheta = twoThetaStart - addedPointCnt * step;

            for (int i = 0; i < pointCnt + 2 * addedPointCnt; i++)
            {
                var theta = Pi * twoTheta / 360;
                var factor = SizeCorrectionFactor(instrumentSettings, theta);
                result.Add(twoTheta, scaling * factor * TotalReflectance(sample, theta));
                twoTheta += step;
            }

            return GetConvoluted(result, resolution).Skip(addedPointCnt).Take(pointCnt).ToDictionary(k => k.Key, v => v.Value);
        }

        public double GetErrorLog(Sample1 sample, Dictionary<double, double> experiment, InstrumentSettings instrumentSettings, double xmin, double xmax)
        {
            double error = 0;
            var scaling = instrumentSettings.Scaling;

            var calculated = GetXRR(sample, instrumentSettings, experiment.First().Key, experiment.Last().Key, experiment.Count);

            int interpolationIndex = 0;

            foreach (var point in experiment)
            {
                double x = point.Key;
                if ((x >= xmin) && (x <= xmax))
                {
                    var xrrValue = Interpolate(calculated, point.Key, ref interpolationIndex);
                    var expPoint = point.Value;
                    error += Math.Abs(Math.Log10(expPoint) - Math.Log10(xrrValue));
                }
            }

            return error;
        }

        public double GetErrorLeastSquares(Sample1 sample, Dictionary<double, double> experiment, InstrumentSettings instrumentSettings, double xmin, double xmax)
        {
            double error = 0;
            var scaling = instrumentSettings.Scaling;

            var calculated = GetXRR(sample, instrumentSettings, experiment.First().Key, experiment.Last().Key, experiment.Count);

            int interpolationIndex = 0;

            foreach (var point in experiment)
            {
                double x = point.Key;
                if ((x >= xmin) && (x <= xmax))
                {
                    var xrrValue = Interpolate(calculated, point.Key, ref interpolationIndex);
                    var expPoint = point.Value;
                    error += Math.Pow((expPoint - xrrValue), 2);
                }
            }

            return error;
        }

        public double GetErrorChiSquared(Sample1 sample, Dictionary<double, double> experiment, InstrumentSettings instrumentSettings, double xmin, double xmax)
        {
            double error = 0;
            var scaling = instrumentSettings.Scaling;

            var calculated = GetXRR(sample, instrumentSettings, experiment.First().Key, experiment.Last().Key, experiment.Count);

            int interpolationIndex = 0;

            foreach (var point in experiment)
            {
                double x = point.Key;
                if ((x >= xmin) && (x <= xmax))
                {
                    var xrrValue = Interpolate(calculated, point.Key, ref interpolationIndex);
                    var expPoint = point.Value;
                    error += Math.Pow((expPoint - xrrValue), 2) / xrrValue;
                }
            }

            return error;
        }

        public double GetErrorLeastLogSquares(Sample1 sample, Dictionary<double, double> experiment, InstrumentSettings instrumentSettings, double xmin, double xmax)
        {
            double error = 0;
            var scaling = instrumentSettings.Scaling;

            var calculated = GetXRR(sample, instrumentSettings, experiment.First().Key, experiment.Last().Key, experiment.Count);

            int interpolationIndex = 0;

            foreach (var point in experiment)
            {
                double x = point.Key;
                if ((x >= xmin) && (x <= xmax))
                {
                    var xrrValue = Interpolate(calculated, point.Key, ref interpolationIndex);
                    var expPoint = point.Value;
                    error += Math.Pow((Math.Log10(expPoint) - Math.Log10(xrrValue)), 2);
                }
            }

            return error;
        }

        private double TotalReflectance(Sample1 sample, double theta)
        {
            var qiVacuum = 2 * k * Math.Sin(theta);
            var layers = new List<XRR.Layer>();

            // rearrange layers
            foreach (var structure in sample.Structures)
            {
                int numberOfPeriods = structure.Quantity;
                for (int i = 0; i < numberOfPeriods; i++)
                {
                    foreach (var layer in structure.Layers)
                    {
                        layers.Add(layer);
                    }
                }
            }
            layers.Reverse();

            var count = layers.Count;
            var qi = Qi(sample.Substrate, theta);
            var qiNext = Qi(layers[0], theta);
            var roughCoeff = Complex.Exp(-0.5 * Math.Pow(sample.Substrate.Roughness.Value * 1e-9, 2) * qi * qiNext);
            var rs = roughCoeff * (qiNext - qi) / (qiNext + qi);

            for (int i = 0; i < count; i++)
            {
                roughCoeff = Complex.Exp(-0.5 * Math.Pow(layers[i].Roughness.Value * 1e-9, 2) * qi * qiNext);
                qi = qiNext;
                qiNext = i == count - 1 ? qiVacuum : Qi(layers[i + 1], theta);
                var ri = roughCoeff * (qiNext - qi) / (qiNext + qi);
                var phaseFactor = Complex.Exp(Complex.Multiply(new Complex(0, 1), qi * layers[i].Thickness.Value * 1e-9));
                rs = (ri + rs * phaseFactor) / (1 + ri * rs * phaseFactor);
            }

            return Math.Pow(Complex.Abs(rs), 2);
        }

        private Complex Qi(XRR.Medium medium, double theta)
        {
            double delta = 0.5 * wavelength * wavelength * Re * medium.ElectronDensity / Pi;
            double beta = 0.25 * wavelength * medium.Attenuation / Pi;
            double Q = 2 * k * Math.Sin(theta);

            return Complex.Sqrt(new Complex(Q * Q - 8 * k * k * delta, 8 * k * k * beta));
        }

        private double SizeCorrectionFactor(InstrumentSettings instrumentSettings, double theta)
        {
            var factor = (instrumentSettings.SampleSize / instrumentSettings.BeamWidth) * Math.Sin(theta);

            return factor < 1 ? factor : 1;
        }

        private Dictionary<double, double> GetConvoluted(Dictionary<double, double> xrr, double resolution)
        {
            if (resolution == 0) return xrr;

            var xValues = xrr.Keys.ToList();
            var yValues = xrr.Values.ToList();

            Dictionary<double, double> convoluted = new Dictionary<double, double>();
            int size = xrr.Count;
            double step = (xrr.Last().Key - xrr.First().Key) / (size - 1);
            double sigma = resolution / (2 * Math.Sqrt(2 * Math.Log(2)));
            double sigma2 = -2 * sigma * sigma;

            int gaussSize = (int)Math.Ceiling(4 * resolution / step);
            var gauss = new double[gaussSize];
            for (int i = 0; i < gaussSize; i++)
            {
                gauss[i] = Math.Exp(Math.Pow(step * i, 2) / (sigma2));
            }

            var c = step / (sigma * Math.Sqrt(2 * Pi));

            for (int j = 0; j < size; j++)
            {
                double value = 0;
                int kmin = Math.Max(0, j - gaussSize + 1);
                int kmax = Math.Min(size, j + gaussSize);
                for (int k = kmin; k < kmax; k++)
                {
                    double normFactor = yValues[k] * c;
                    value = value + normFactor * gauss[Math.Abs(j - k)];
                }
                convoluted.Add(xValues[j], value);
            }
            return convoluted;
        }

        private double Interpolate(Dictionary<double, double> sortedDictionary, double xValue, ref int startIndex)
        {
            var xValues = sortedDictionary.Keys.ToList();
            var yValues = sortedDictionary.Values.ToList();

            int size = sortedDictionary.Count;

            for (int i = startIndex; i < size; i++)
            {
                startIndex = i == 0 ? 0 : i - 1;
                var xValue_i = xValues[i];

                if (xValue_i == xValue)
                {
                    return yValues[i];
                }

                if ((i == 0) && (xValue < xValue_i))
                {
                    return yValues[0] - (xValues[0] - xValue) * (yValues[1] - yValues[0]) / (xValues[1] - xValues[0]);
                }

                if ((i == size - 1) && (xValue > xValue_i))
                {
                    return yValues[size - 1] + (xValue - xValues[size - 1]) *
                        (yValues[size - 1] - yValues[size - 2]) / (xValues[size - 1] - xValues[size - 2]);
                }

                if (xValue_i > xValue)
                {
                    var yValue_i = yValues[i];
                    var yValue_prev = yValues[i - 1];
                    var xValue_prev = xValues[i - 1];

                    return yValue_prev + (xValue - xValue_prev) * (yValue_i - yValue_prev) / (xValue_i - xValue_prev);
                }
            }
            return double.NaN;
        }

    }
}
