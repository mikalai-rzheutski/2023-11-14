using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using WindowsFormsApplication2.Models;
//using WindowsFormsApplication2.Models.XRR;
//using WindowsFormsApplication2.Models.XRR;
using WindowsFormsApplication2.ViewModel;
using WindowsFormsApplication2.views;

namespace WindowsFormsApplication2
{
    public partial class MainForm : Form
    {
        const string XrrDifferentialEvolutionMethod = "Differential Evolution";
        const string XrrLevenbergMarquardtMethod = "Levenberg-Marquardt";

        Dictionary<string, XrrAlgorithm> xrrAlgorithmMapp = new Dictionary<string, XrrAlgorithm> {
            { XrrDifferentialEvolutionMethod, XrrAlgorithm.DifferentialEvolution },
            { XrrLevenbergMarquardtMethod, XrrAlgorithm.LevenbergMarquardt }
        };

        const string Log = "Log";
        const string ChiSquared = "Chi-Squared";
        const string LeastSquares = "Least Squares";
        const string LeastLogSquares = "Least Log Squares";

        Dictionary<string, FigureOfMerit> xrrFomMapp = new Dictionary<string, FigureOfMerit> {
            { Log, FigureOfMerit.Log },
            { ChiSquared, FigureOfMerit.ChiSquare },
            { LeastSquares, FigureOfMerit.LeastSquares },
            { LeastLogSquares, FigureOfMerit.LeastLogSquares }
        };

        const int calcXrrSeriesIdx = 1;

        Dictionary<double, double> experiment = new Dictionary<double, double>();
        List<double> twoThetaValuesExp;

        FitXrr fitXrr;


        public MainForm()
        {
            InitializeComponent();
            FineInitialization();
        }

        private void FineInitialization()
        {
            string[] xrrFitMethods = { XrrDifferentialEvolutionMethod, XrrLevenbergMarquardtMethod };
            xrrMethods.Items.AddRange(xrrFitMethods);
            xrrMethods.SelectedIndex = 0;

            string[] xrrFitFomTypes = { Log, ChiSquared, LeastSquares, LeastLogSquares };
            fomTypes.Items.AddRange(xrrFitFomTypes);
            fomTypes.SelectedIndex = 0;

            FullRangeXrrFit.Checked = true;

            xrrDiffEvPopSize.SelectedIndex = 0;
        }

        private void exitButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            var data = chart1.Series[0].Points.ToList();
            string ascii = "";
            IFormatProvider format = System.Globalization.CultureInfo.InvariantCulture;
            foreach (var el in data)
            {
                ascii = ascii + el.XValue.ToString(format) + "\t" + el.YValues[0].ToString(format) + Environment.NewLine;
            }

            SaveFileDialog file = new SaveFileDialog();
            file.Filter = "Text (*.txt)|*.txt";
            file.ShowDialog();
            if (file.FileName != "")
            {
                System.IO.File.WriteAllText(file.FileName, ascii);
            }
        }

        private void clearFitButton_Click(object sender, EventArgs e)
        {
            chart1.Series[1].Points.Clear();
            chart2.Series[0].Points.Clear();

            textBox2.Text = "";
            textBox1.Text = "";
            textBox3.Text = "";
            textBox4.Text = "";
            textBox5.Text = "";
        }

        private void button4_Click(object sender, EventArgs e)
        {
            chart1.Series[0].Points.Clear();

            if (openFileDialog1.ShowDialog() == DialogResult.Cancel)
                return;
            string filename = openFileDialog1.FileName;
            string fileText = System.IO.File.ReadAllText(filename);

            using (System.IO.StringReader reader = new System.IO.StringReader(fileText))
            {
                string line = reader.ReadLine();
                int i = 0;
                while (!string.IsNullOrWhiteSpace(line))
                {
                    i++;
                    var values = line.Split(' ', '\t');
                    try
                    {
                        var x = double.Parse(values[0], System.Globalization.CultureInfo.InvariantCulture);
                        var y = double.Parse(values[1], System.Globalization.CultureInfo.InvariantCulture);
                        experiment.Add(x, y);
                    }
                    catch { }
                    line = reader.ReadLine();
                };
            }
            foreach (var point in experiment)
            {
                chart1.Series[0].Points.AddXY(point.Key, point.Value);
            }

            var minScale = experiment.Values.Min() / 2;
            var maxScale = experiment.Values.Max() * 2;

            chart1.ChartAreas[0].AxisY.Minimum = minScale;
            chart1.ChartAreas[0].AxisY.Maximum = maxScale;
            chart1.ChartAreas[0].AxisX.Interval = 1;

            twoThetaValuesExp = new List<double>(experiment.Keys);

            instSettDataGridView.Rows[1].Cells[1].Value = "0.03";
            instSettDataGridView.Rows[2].Cells[1].Value = "30";
            instSettDataGridView.Rows[3].Cells[1].Value = "0.12";
            double scaleFactor = experiment.Values.Max<double>();
            instSettDataGridView.Rows[4].Cells[1].Value = scaleFactor.ToString();
        }

        private void startXrrFitButton_Click(object sender, EventArgs e)
        {
            int k = textBox7.Text == "" ? 0 : int.Parse(textBox7.Text) + 1;
            textBox7.Text = k.ToString();

            string seriesName = textBox7.Text + "N5645";
            chart2.Series.Add(seriesName);
            chart2.Series[seriesName].ChartType = SeriesChartType.Line;
            chart2.Series[seriesName].BorderWidth = 2;

            //string fitMethod = xrrMethods.SelectedItem.ToString();
            //string fom = fomTypes.SelectedItem.ToString();

            XrrAlgorithm xrrAlgorithm = xrrAlgorithmMapp[xrrMethods.SelectedItem.ToString()];
            FigureOfMerit fom = xrrFomMapp[fomTypes.SelectedItem.ToString()];

            var xmin = (double)TwoThetaMinValue.Value;
            var xmax = (double)TwoThetaMaxValue.Value;

            XrrFitSettings algorithmSettings;

            switch (xrrAlgorithm)
            {
                case XrrAlgorithm.DifferentialEvolution:
                    var km = (double)xrrDiffEvolKm.Value;
                    var kr = (double)xrrDiffEvolKr.Value;
                    ushort n;
                    ushort populationSize = ushort.TryParse(xrrDiffEvPopSize.Text, out n) ? n : (ushort)0;
                    ushort maxGenerations = decimal.ToUInt16(xrrDiffEvolMaxGen.Value);
                    algorithmSettings = new DifferentialEvolutionSettings(fom, xmin, xmax, km, kr, populationSize, maxGenerations);
                    break;
                default:
                    algorithmSettings = new LevenbergMarquardtSettings(fom, xmin, xmax, 0);
                    break;
            }

            fitXrr.StartFit(experiment, "", sampleTable, instSettDataGridView, algorithmSettings);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            chart1.Series[1].Points.Clear();
            chart2.Series[0].Points.Clear();

            textBox2.Text = "";
            textBox1.Text = "";
            textBox3.Text = "";
            textBox4.Text = "";
            textBox5.Text = "";
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var data = chart1.Series[0].Points.ToList();
            string ascii = "";
            IFormatProvider format = System.Globalization.CultureInfo.InvariantCulture;
            foreach (var el in data)
            {
                ascii = ascii + el.XValue.ToString(format) + "\t" + el.YValues[0].ToString(format) + Environment.NewLine;
            }

            SaveFileDialog file = new SaveFileDialog();
            file.Filter = "Text (*.txt)|*.txt";
            file.ShowDialog();
            if (file.FileName != "")
            {
                System.IO.File.WriteAllText(file.FileName, ascii);
            }
        }

        private void insert_Click(object sender, EventArgs e)
        {
            int selected = sampleTable.CurrentCell.RowIndex;
            sampleTable.Rows.Insert(selected);
        }

        private void getSelected_Click(object sender, EventArgs e)
        {
            var selectedRows = sampleTable.SelectedRows;
            List<int> indexes = new List<int>() { 1000000 };
            foreach (DataGridViewRow row in selectedRows)
            {
                indexes.Add(row.Index);
            }

            int count = selectedRows.Count;
            int colIdx = 11;
            int rowIdx = indexes.Min();
            SpannedDataGridViewNet2.DataGridViewTextBoxCellEx dataGridViewCell =
                (SpannedDataGridViewNet2.DataGridViewTextBoxCellEx)sampleTable[colIdx, rowIdx];
            dataGridViewCell.RowSpan = count;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            ToolTip toolTip = new ToolTip();
            toolTip.SetToolTip(openProjectButton, "Open project");
            toolTip.SetToolTip(saveProjectButton, "Save project");
            toolTip.SetToolTip(exitButton, "Exit");

            InitSampleTable();
            InitInstrumentSettingsTable();

            fitXrr = new FitXrr(this);

            //init Sample table
            sampleTable.Rows[0].Cells[2].Value = "Si";
            sampleTable.Rows[0].Cells[4].Value = "2.33";
            sampleTable.Rows[0].Cells[8].Value = "Gauss";
            sampleTable.Rows[0].Cells[9].Value = "0.5";

            sampleTable.Rows.Insert(0);
            sampleTable.Rows[0].Cells[1].Value = "layer";
            sampleTable.Rows[0].Cells[2].Value = "AlN";
            sampleTable.Rows[0].Cells[4].Value = "3.26";
            sampleTable.Rows[0].Cells[6].Value = "50";
            sampleTable.Rows[0].Cells[8].Value = "Gauss";
            sampleTable.Rows[0].Cells[9].Value = "2";
            sampleTable.Rows[0].Cells[11].Value = "1";

            textBox6.Text = "start";
        }

        private void InitSampleTable()
        {
            var disabledStyle = new DataGridViewCellStyle();
            disabledStyle.BackColor = Color.LightGray;
            disabledStyle.SelectionBackColor = Color.LightGray;

            sampleTable.Rows.Add();
            var boldFontStyle = sampleTable.Rows[0].Cells["Layer"].Style.Font;
            sampleTable.Rows[0].Cells["Layer"].Value = "Substrate";

            var boldFont = new Font("Arial", 8, FontStyle.Bold);
            sampleTable.Rows[0].Cells["Layer"].Style.Font = boldFont;

            sampleTable.Rows[0].Cells["DensFix"].Value = false;
            sampleTable.Rows[0].Cells["RoughFix"].Value = false;

            sampleTable.Rows[0].Cells["Thickness"].Value = "-//-";
            sampleTable.Rows[0].Cells["Thickness"].ReadOnly = true;
            sampleTable.Rows[0].Cells["Thickness"].Style = disabledStyle;

            sampleTable.Rows[0].Cells["ThickFix"].ReadOnly = true;
            sampleTable.Rows[0].Cells["ThickFix"].Style = disabledStyle;

            sampleTable.Rows[0].Cells["ThickFix"].Value = false;
            sampleTable.Rows[0].Cells["ThickFix"] = new DataGridViewTextBoxCell();
            sampleTable.Rows[0].Cells["ThickFix"].Value = "";
            sampleTable.Rows[0].Cells["ThickFix"].Style = disabledStyle;


            sampleTable.Rows[0].Cells["Period"].Value = "-//-";
            sampleTable.Rows[0].Cells["Period"].ReadOnly = true;
            sampleTable.Rows[0].Cells["Period"].Style = disabledStyle;
        }

        private void InitInstrumentSettingsTable()
        {
            var rows = instSettDataGridView.Rows;
            rows.Add(5);
            rows[0].Cells[0].Value = "Wavelength [Å]";
            rows[1].Cells[0].Value = "2Θ Resolution [°]";
            rows[2].Cells[0].Value = "Sample size [mm]";
            rows[3].Cells[0].Value = "Beam size [mm]";
            rows[4].Cells[0].Value = "Scaling";

            DataGridViewComboBoxCell comboBoxCell = new DataGridViewComboBoxCell();
            comboBoxCell.FlatStyle = FlatStyle.Flat;
            comboBoxCell.Style.BackColor = Color.White;
            comboBoxCell.Items.AddRange(new string[] { "1.5406 - Cu", "1.788965 - Co", "0.7093 - Mo", "1.65791 - Ni", "2.2897 - Cr", "1.936042 - Fe" });
            instSettDataGridView[1, 0] = comboBoxCell;
            instSettDataGridView[1, 0].Value = "1.5406 - Cu";

        }

        private void dataGridView1_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            e.Control.KeyPress -= new KeyPressEventHandler(Column_KeyPress);
            int column = sampleTable.CurrentCell.ColumnIndex;
            if ((column == 4) || (column == 6) || (column == 9) || (column == 11))
            {
                TextBox tb = e.Control as TextBox;
                if (tb != null)
                {
                    tb.KeyPress += new KeyPressEventHandler(Column_KeyPress);
                }
            }
        }

        private void Column_KeyPress(object sender, KeyPressEventArgs e)
        {
            int column = sampleTable.CurrentCell.ColumnIndex;
            var separator = System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator.ToCharArray()[0];
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (column == 11))
            {
                e.Handled = true;
            }
            else if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != separator)
            {
                e.Handled = true;
            }

            if ((e.KeyChar == separator) && ((sender as TextBox).Text.IndexOf(separator) > -1))
            {
                e.Handled = true;
            }
        }

        private void testBtn_Click(object sender, EventArgs e)
        {
            textBox6.Text = xrrDiffEvolKm.Value.ToString();


        }


        public void PlotXrrCalculated(Dictionary<double, double> data)
        {
            Invoke(new Action(() =>
            {
                Graphs.PlotData(data, chart1, calcXrrSeriesIdx);
            }));
        }

        public void AddPointToErrorChart(double iteration, double errorValue)
        {
            Invoke(new Action(() =>
            {
                string seriesName = textBox7.Text + "N5645";
                chart2.Series[seriesName].Points.AddXY(iteration, errorValue);
                chart2.Update();
            }));
        }

        public void insertSampleParameters(SampleDto sample)
        {
            //var rows = dataGridView1.Rows;
            //int rowCnt = rows.Count;
            //for (int i = 0; i < rowCnt - 1; i++)
            //{
            //    rows.RemoveAt(i);
            //}

            //var substrateRow = rows[0];

            //var substrate = sample.Substrate;
            //string substrateFormula = substrate.Formula;
            //substrateRow.Cells[2].Value = substrateFormula;
            //substrateRow.Cells[4].Value = substrate.Density.Value.ToString("N");
            //substrateRow.Cells[5].Value = substrate.Density.Fix;
            //substrateRow.Cells[9].Value = substrate.Roughness.Value.ToString("N");
            //substrateRow.Cells[10].Value = substrate.Roughness.Fix;
            //substrateRow.Cells[7].Value = (SurfaceRoughnessTypeDto)Enum.Parse(typeof(SurfaceRoughnessTypeDto),
            //    substrate.InterfaceType.ToString());


            //foreach (var structure in sample.Structures)
            //{

            //}
        }

        public void UpdateSampleParameters(SampleDto sample)
        {
            var rows = sampleTable.Rows;
            int count = rows.Count;
            var substrateRow = rows[count - 1];

            DataGridViewCellStyle changedCellStyle = new DataGridViewCellStyle();
            changedCellStyle.SelectionForeColor = Color.Blue;
            changedCellStyle.ForeColor = Color.Blue;

            var substrate = sample.Substrate;
            substrateRow.Cells[4].Value = substrate.Density.Value.ToString("N");
            substrateRow.Cells[4].Style = changedCellStyle;
            substrateRow.Cells[9].Value = substrate.Roughness.Value.ToString("N");
            substrateRow.Cells[9].Style = changedCellStyle;

            int index = 0;
            foreach (var structure in sample.Structures)
            {
                foreach (var layer in structure.Layers)
                {
                    rows[index].Cells[4].Value = layer.Density.Value.ToString("N");
                    rows[index].Cells[4].Style = changedCellStyle;
                    rows[index].Cells[6].Value = layer.Thickness.Value.ToString("N");
                    rows[index].Cells[6].Style = changedCellStyle;
                    rows[index].Cells[9].Value = layer.Roughness.Value.ToString("N");
                    rows[index++].Cells[9].Style = changedCellStyle;
                }
            }
        }

        public void UpdateInstrumentSettings(double scaling)
        {
            DataGridViewCellStyle changedCellStyle = new DataGridViewCellStyle();
            changedCellStyle.SelectionForeColor = Color.Blue;
            changedCellStyle.ForeColor = Color.Blue;

            instSettDataGridView.Rows[4].Cells[1].Value = scaling.ToString("N");
            instSettDataGridView.Rows[4].Cells[1].Style = changedCellStyle;
        }

        public void UpdateIterationTime(double time)
        {
            Invoke((MethodInvoker)delegate
            {
                textBox6.Text = time.ToString();
            });
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            //DataGridViewCellStyle manualCellStyle = new DataGridViewCellStyle();
            //manualCellStyle.SelectionForeColor = Color.Black;
            //manualCellStyle.ForeColor = Color.Black;

            ////    textBox6.Text = e.RowIndex.ToString() + " " + e.ColumnIndex.ToString();

            //if ((e.ColumnIndex == 4) || (e.ColumnIndex == 6) || (e.ColumnIndex == 9))
            //{
            //    //    dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Style = manualCellStyle;

            //    textBox8.AppendText(e.RowIndex.ToString() + " " + e.ColumnIndex.ToString() + "\r\n");
            //}

        }

        private void Fit_Click(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckStateChanged(object sender, EventArgs e)
        {
            bool checkState = Convert.ToBoolean(FullRangeXrrFit.CheckState);
            TwoThetaMinValue.Enabled = !checkState;
            TwoThetaMaxValue.Enabled = !checkState;
        }
    }
}
