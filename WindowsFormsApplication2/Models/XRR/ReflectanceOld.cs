using System;
using System.Numerics;
using System.Collections.Generic;

namespace WindowsFormsApplication2.Models
{
    class ReflectanceOld
    {
        public const double Lambda = 1.540562e-10;
        public const double HV = 1.239841984e-9 / Lambda;
        double k = 2 * Pi / Lambda;
        const double Pi = 3.14;
        const double Re = 2.8179403227e-15;

        private Sample structure;

        public ReflectanceOld()
        {}

        public ReflectanceOld(Sample structure)
        {
            this.structure = structure;
        }

        private Complex Qi(Medium medium, double theta)
        {
            double delta = 0.5 * Lambda * Lambda * Re * medium.ElectronDensity / Pi;
            double beta = 0.25 * Lambda * medium.Attenuation / Pi;
            double Q = 2 * k * Math.Sin(theta);

            return Complex.Sqrt(Q * Q - 8 * k * k * delta + new Complex(0, 8 * k * k * beta));
        }

        public double TotalReflectance3(Sample structure, double theta)
        {
            var qiVacuum = 2 * k * Math.Sin(theta);
            var layers = structure.Layers;
            var count = layers.Count;
            var qi = Qi(structure.Substrate, theta);
            var qiNext = Qi(layers[0], theta);
            var roughCoeff = Complex.Exp(-0.5 * Math.Pow(structure.Substrate.Roughness, 2) * qi * qiNext);
            var rs = roughCoeff * (qiNext - qi) / (qiNext + qi);

            for (int i = 0; i < count; i++)
            {
                roughCoeff = Complex.Exp(-0.5 * Math.Pow(layers[i].Roughness, 2) * qi * qiNext);
                qi = qiNext;
                qiNext = i == count - 1 ? qiVacuum : Qi(layers[i + 1], theta);
                var ri = roughCoeff * (qiNext - qi) / (qiNext + qi);
                var phaseFactor = Complex.Exp(Complex.Multiply(new Complex(0, 1), qi * layers[i].Thickness));
                rs = (ri + rs * phaseFactor) / (1 + ri * rs * phaseFactor);
            }

            return Complex.Abs(rs * rs);
        }

        public Dictionary<double, double> GetXRR(Sample structure, List<double> twoThetaValues)
        {
            var result = new Dictionary<double, double>();
            this.structure = structure;

            foreach (var twoTheta in twoThetaValues)
            {
                result.Add(twoTheta, TotalReflectance3(structure, Pi * twoTheta / 360));
            }

            return result;
        }

        public double GetError(Sample structure, Dictionary<double, double> experiment)
        {
            this.structure = structure;
            double error = 0;

            foreach (var point in experiment)
            {
                var xrrValue = TotalReflectance3(structure, Pi * point.Key / 360);
                error = error + Math.Pow(Math.Log10(point.Value) - Math.Log10(xrrValue), 2);
            }

            return error;
        }
    }
}
