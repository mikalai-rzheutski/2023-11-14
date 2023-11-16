using System;
using System.Collections.Generic;
using System.ComponentModel;
using WindowsFormsApplication2.ViewModel;

namespace WindowsFormsApplication2.Models.XRR
{
    class FitResult : INotifyPropertyChanged
    {
        Dictionary<double, double> bestFit;
        double error;
        int iteration;
        SampleDto sample;

        public Dictionary<double, double> BestFit
        {
            get { return bestFit; }
            set
            {
                bestFit = value;
           //     PlotXrrCalculated(value);
            }
        }

        public double Error
        {
            get { return error; }
            set { error = value; }
        }

        public int Iteration
        {
            get { return iteration; }
            set { iteration = value; }
        }

        public SampleDto SampleValue
        {
            get { return sample; }
            set { sample = value; }
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
    }
}
