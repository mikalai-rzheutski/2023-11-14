using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WindowsFormsApplication2.ViewModel;

namespace WindowsFormsApplication2.Models.XRR
{
    class Fit
    {
        FitXrr fitXrr;
        delegate double GetError(Sample1 sample, Dictionary<double, double> experiment, InstrumentSettings instrumentSettings, double xmin, double xmax);

        public Fit(FitXrr fitXrr)
        {
            this.fitXrr = fitXrr;
        }


        public void Start(SampleDto sampleDto, InstrumentSettings instrumentSettings, 
            XrrFitSettings fitSettings, Dictionary<double, double> experiment)
        {
            Sample1 sample = new Sample1(sampleDto, instrumentSettings.Wavelength);
            Reflectance reflectance = new Reflectance(instrumentSettings);

            switch (fitSettings.Algorithm)
            {
                case XrrAlgorithm.DifferentialEvolution:
                    DifferentialEvolutionFit(sample, reflectance, (DifferentialEvolutionSettings)fitSettings, instrumentSettings, experiment);
                    break;
            }
        }

        private async void DifferentialEvolutionFit(Sample1 sample, Reflectance reflectance,
            DifferentialEvolutionSettings fitSettings, InstrumentSettings instrumentSettings, Dictionary<double, double> experiment)
        {
            Random rnd = new Random();

            GetError getError;

            switch (fitSettings.Fom)
            {
                case FigureOfMerit.Log:
                    getError = reflectance.GetErrorLog;
                    break;
                case FigureOfMerit.ChiSquare:
                    getError = reflectance.GetErrorChiSquared;
                    break;
                case FigureOfMerit.LeastSquares:
                    getError = reflectance.GetErrorLeastSquares;
                    break;
                default:
                    getError = reflectance.GetErrorLeastLogSquares;
                    break;
            }
            
            int numberOfGenerations = fitSettings.MaxGenerations;
            int populationSize = fitSettings.PopulationSize;
            double km = fitSettings.Km;
            double kr = fitSettings.Kr;
            double xmin = fitSettings.Xmin;
            double xmax = fitSettings.Xmax;

            var parameters = sample.getParameters();
            var p0 = new List<double>();
            var pmin = new List<double>();
            var pmax = new List<double>();
            var fix = new List<bool>();

            var scaling = instrumentSettings.Scaling;
            p0.Add(scaling);
            pmin.Add(scaling / 10);
            pmax.Add(scaling * 10);
            fix.Add(false);

            foreach (var parameter in parameters)
            {
                p0.Add(parameter.Value);
                pmin.Add(parameter.Min);
                pmax.Add(parameter.Max);
                fix.Add(parameter.Fix);
            }

            int numberOfParameters = p0.Count();
            int numberOfVariedParameters = fix.Count(c => !c);
            int np = populationSize > 0 ? populationSize : 10 * numberOfVariedParameters;

            // creating of initial generation
            var oldGeneration = new List<List<double>>() { p0 };
            var errors = new List<double>();

            for (int i = 1; i < np; i++)
            {
                var p = new List<double>();
                for (int k = 0; k < numberOfParameters; k++)
                {
                    var value = (!fix[k]) ? pmin[k] + rnd.NextDouble() * (pmax[k] - pmin[k]) : p0[k];
                    p.Add(value);
                }
                oldGeneration.Add(p);
            }

            // process generations
            for (int g = 0; g < numberOfGenerations; g++)
            {
                var startTime = DateTime.Now;
                errors = new List<double>();
                double minError = double.MaxValue;
                int minimumErrorIndex = 0;

                for (int i = 0; i < oldGeneration.Count; i++)
                {
                    sample.UpdateParameters(oldGeneration[i]);
                    instrumentSettings.UpdateScaling(oldGeneration[i][0]);
                    var error = getError(sample, experiment, instrumentSettings, xmin, xmax);
                    if (error < minError)
                    {
                        minError = error;
                        minimumErrorIndex = i;
                    }
                    errors.Add(error);
                }

                // best-fit vector b
                var b = oldGeneration[minimumErrorIndex];

                // plot error point
                await Task.Run(() => fitXrr.AddPointToErrorChart(g, minError));

                // plot calculated XRR
                sample.UpdateParameters(b);
                instrumentSettings.UpdateScaling(b[0]);
                var bestFit = reflectance.GetXRR(sample, instrumentSettings, experiment.First().Key, experiment.Last().Key, experiment.Count);
                await Task.Run(() => fitXrr.PlotXrrCalculated(bestFit));

                // display current best parameters
                await Task.Run(() => fitXrr.DisplaySampleParameters(sample));
                await Task.Run(() => fitXrr.DisplayInstrumentSettings(instrumentSettings));
                
                // new generation
                var newGeneration = new List<List<double>>();

                // populate new generation
                for (int j = 0; j < np; j++)
                {
                    // select random pa and pb vectors
                    var ab = GetRndInts(0, np - 1, j, minimumErrorIndex, rnd);
                    var pa = oldGeneration[ab.Item1];
                    var pb = oldGeneration[ab.Item2];

                    // mutated best-fit vector
                    var bMut = new List<double>();
                    for (int i = 0; i < numberOfParameters; i++)
                    {
                        var bMuti = b[i] + km * (pa[i] - pb[i]);
                        if ((bMuti < pmin[i]) || (bMuti > pmax[i]))
                        {
                            bMuti = pmin[i] + rnd.NextDouble() * (pmax[i] - pmin[i]);
                        }
                        bMut.Add(bMuti);
                    }

                    // trial vector
                    var t = new List<double>();
                    for (int i = 0; i < numberOfParameters - 1; i++)
                    {
                        t.Add(rnd.NextDouble() <= kr ? bMut[i] : oldGeneration[j][i]);
                    }
                    t.Add(bMut[numberOfParameters - 1]);

                    // select vector for new generation
                    sample.UpdateParameters(t);
                    instrumentSettings.UpdateScaling(t[0]);
                    var newVector = (getError(sample, experiment, instrumentSettings, xmin, xmax) <= errors[j]) ? t : oldGeneration[j];

                    // add the selected vector to the new generation
                    newGeneration.Add(newVector);
                }

                oldGeneration = newGeneration;

                var time = DateTime.Now - startTime;
                // display iteration time
                await Task.Run(() => fitXrr.DisplayIterationTime(time.Milliseconds));
            }
        }

        private static Tuple<int, int> GetRndInts(int minValue, int maxValue, int exclude1, int exclude2, Random rnd)
        {
            int a = exclude1, b = exclude1;

            while ((a == b) || (a == exclude1) || (b == exclude1) || (a == exclude2) || (b == exclude2))
            {
                a = rnd.Next(minValue, maxValue);
                b = rnd.Next(minValue, maxValue);
            }

            return new Tuple<int, int>(a, b);
        }
    }
}
