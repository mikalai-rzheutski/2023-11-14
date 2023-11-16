using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace WindowsFormsApplication2.ViewModel.converters
{
    class InstrumentSettingsConverters
    {
        public static InstrumentSettings ConvertTableToInstrumentSettings(DataGridView instrumentSettings)
        {
            var format = System.Globalization.CultureInfo.InvariantCulture;
            var rows = instrumentSettings.Rows;

            string pattern = @"(\d(\.|\,)\d*)";
            Regex regex = new Regex(pattern);
            var wl = regex.Matches(rows[0].Cells[1].Value.ToString())[0].Groups[1].Value;

            var wavelength = double.Parse(wl, format) * 1e-10;
            var resolution = double.Parse(rows[1].Cells[1].Value.ToString(), format);
            var sampleSize = double.Parse(rows[2].Cells[1].Value.ToString(), format);
            var beamWidth = double.Parse(rows[3].Cells[1].Value.ToString(), format);
            var scaling = double.Parse(rows[4].Cells[1].Value.ToString(), format);

            return new InstrumentSettings(wavelength, resolution, beamWidth, sampleSize, scaling);
        }
    }
}
