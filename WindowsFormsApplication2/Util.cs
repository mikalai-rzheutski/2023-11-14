using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApplication2
{
    class Util
    {
        const string DatabaseFolderName = "NIST_db";

        public static double getMassAttenuationCoefficientByElementAndEnergy(string elementSymbol, double hv)
        {
            string filename = elementSymbol + ".txt";
            var path = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), DatabaseFolderName, filename);
            if (System.IO.File.Exists(path))
            {
                string fileContent = System.IO.File.ReadAllText(path, Encoding.UTF8);
                double energy1 = 0;
                double attenuation1 = 0;

                using (System.IO.StringReader reader = new System.IO.StringReader(fileContent))
                {
                    string line = reader.ReadLine();
                    while (!string.IsNullOrWhiteSpace(line))
                    {
                        var values = line.Split(' ', '\t');
                        try
                        {
                            var currentEnergy = double.Parse(values[0], System.Globalization.CultureInfo.InvariantCulture);
                            if (currentEnergy >= hv)
                            {
                                var currentAttenuation = double.Parse(values[1], System.Globalization.CultureInfo.InvariantCulture);
                                return attenuation1 + (currentAttenuation - attenuation1) * (hv - energy1) / (currentEnergy - energy1);
                            }

                            energy1 = double.Parse(values[0], System.Globalization.CultureInfo.InvariantCulture);
                            attenuation1 = double.Parse(values[1], System.Globalization.CultureInfo.InvariantCulture);
                        }
                        catch { }
                        line = reader.ReadLine();
                    };
                }
            }
            return 0;
        }

        public static Tuple<string, int, double> getAtomicNumberAndWeightByElement(string elementSymbol)
        {
            string filename = "AtomicNumbersWeights.txt";
            var path = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), DatabaseFolderName, filename);
            if (System.IO.File.Exists(path))
            {
                string fileContent = System.IO.File.ReadAllText(path, Encoding.UTF8);
                using (System.IO.StringReader reader = new System.IO.StringReader(fileContent))
                {
                    string line = reader.ReadLine();
                    while (!string.IsNullOrWhiteSpace(line))
                    {
                        var values = line.Split(' ', '\t');
                        try
                        {
                            string symbol = values[0];
                            if (symbol == elementSymbol)
                            {
                                int z = int.Parse(values[1]);
                                double weight = double.Parse(values[2], System.Globalization.CultureInfo.InvariantCulture);
                                return Tuple.Create(symbol, z, weight);
                            } 
                        }
                        catch { }
                        line = reader.ReadLine();
                    };
                }
            }
            return null;
        }
    }
}
