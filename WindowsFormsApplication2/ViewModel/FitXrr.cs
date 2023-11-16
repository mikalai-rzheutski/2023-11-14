using System.Collections.Generic;
using System.Windows.Forms;
using WindowsFormsApplication2.Models.XRR;
using WindowsFormsApplication2.ViewModel.converters;

namespace WindowsFormsApplication2.ViewModel
{
    class FitXrr
    {
        MainForm mainForm;
        Fit fit;

        public FitXrr(MainForm mainForm)
        {
            this.mainForm = mainForm;
            fit = new Fit(this);
        }

        public void StartFit(Dictionary<double, double> experimentalData, string id,
            DataGridView sampleTable, DataGridView instrumentSettingsTable, XrrFitSettings xrrFitSettings)
        {
            fit.Start(SampleConverters.ConvertTableToSampleDto(id, sampleTable), 
                InstrumentSettingsConverters.ConvertTableToInstrumentSettings(instrumentSettingsTable), 
                xrrFitSettings, experimentalData);
        }

        public void PlotXrrCalculated(Dictionary<double, double> calculatedData)
        {
            mainForm.PlotXrrCalculated(calculatedData);
        }

        public void AddPointToErrorChart(double iteration, double errorValue)
        {
            mainForm.AddPointToErrorChart(iteration, errorValue);
        }

        public void DisplaySampleParameters(Sample1 sample)
        {
            mainForm.UpdateSampleParameters(SampleConverters.ConvertSampleToSampleDto(sample));
        }

        public void DisplayInstrumentSettings(InstrumentSettings instrumentSettings)
        {
            mainForm.UpdateInstrumentSettings(instrumentSettings.Scaling);
        }

        public void DisplayIterationTime(double time)
        {
            mainForm.UpdateIterationTime(time);
        }
    }
}
