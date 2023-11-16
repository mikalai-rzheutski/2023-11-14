using System;
using System.Collections.Generic;
using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApplication2.views
{
    class Graphs
    {
        //const int expXrrSeriesIdx = 0;
        //const int calcXrrSeriesIdx = 1;

        //public static void PlotXrrCalculated(Dictionary<double, double> data)
        //{
        //    PlotData(data, chart1, calcXrrSeriesIdx);
        //}

        public static void PlotData(Dictionary<double, double> data, Chart chart, int seriesIdx)
        {
            chart.Series[seriesIdx].Points.Clear();
            foreach (var point in data)
            {
                chart.Series[seriesIdx].Points.AddXY(point.Key, point.Value);
            }
            chart.Update();
        }
    }
}
