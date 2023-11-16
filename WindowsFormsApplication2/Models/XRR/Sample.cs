using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WindowsFormsApplication2.ViewModel;

namespace WindowsFormsApplication2.Models
{
    class ElementAndIndex
    {
        public string symbol;
        public double index;

        public ElementAndIndex(string symbol, double index)
        {
            this.symbol = symbol;
            this.index = index;
        }
    }

    class Medium
    {
        const double AtomicMassUnit = 1.6605390666e-27;

        double roughness;
        bool fixRoughness;
        double density;
        bool fixDensity;
        double massAttenuationCoeff;
        double atomicMassToZ;

        double electronDensity;
        double attenuation;

        private List<ElementAndIndex> substance;

        public double Roughness
        {
            get
            {
                return roughness;
            }
        }

        public bool FixRoughness
        {
            get
            {
                return fixRoughness;
            }
        }

        public double Density
        {
            get
            {
                return density;
            }

        }

        public bool FixDensity
        {
            get
            {
                return fixDensity;
            }

        }

        public double ElectronDensity
        {
            get
            {
                return electronDensity;
            }
        }

        public double Attenuation
        {
            get
            {
                return attenuation;
            }
        }

        public Medium(List<ElementAndIndex> substance, double density, bool fixDensity, double roughness, bool fixRoughness, double wavelength)
        {
            this.substance = substance;
            this.density = density;
            this.fixDensity = fixDensity;
            this.roughness = roughness;
            this.fixRoughness = fixRoughness;

            double molecularMass = 0;
            double att = 0;
            double z = 0;
            foreach (var element in substance)
            {
                var atomicNumberAndWeight = Util.getAtomicNumberAndWeightByElement(element.symbol);
                var elementMass = element.index * atomicNumberAndWeight.Item3;
                molecularMass = molecularMass + elementMass;
                z = z + atomicNumberAndWeight.Item2;

                var elementMassAttenuation = Util.getMassAttenuationCoefficientByElementAndEnergy(element.symbol, 1.239841984e-9 / wavelength);
                att = att + elementMass * elementMassAttenuation;
            }

            atomicMassToZ = AtomicMassUnit * molecularMass / z;
            electronDensity = 1000 * density / atomicMassToZ;

            massAttenuationCoeff = att / molecularMass;
            attenuation = 100 * density * massAttenuationCoeff;
        }
    }

    class Layer : Medium
    {
        double thickness;
        bool fixThickness;

        public Layer(List<ElementAndIndex> substance, double density, bool fixDensity, double thickness, bool fixThickness, double roughness, bool fixRoughness, double wavelength) 
            : base(substance, density, fixDensity, roughness, fixRoughness, wavelength)
        {
            this.thickness = thickness;
            this.fixThickness = fixThickness;
        }

        public double Thickness => thickness;
        public bool FixThickness => fixThickness;

    }

    class Sample
    {
        Medium substrate;

        // The list of the layers starting from the substrate
        List<Layer> layers;

        public Medium Substrate => substrate;
        public List<Layer> Layers => layers;

        public Sample(List<Layer> layers, Medium substrate)
        {
            this.substrate = substrate;
            this.layers = layers;
        }

        public Sample(SampleDto sampleDto, double wavelength)
        {
            var substrateDto = sampleDto.Substrate;
            substrate = new Medium(getElementAndIndexList(substrateDto.Formula), substrateDto.Density.Value, substrateDto.Density.Fix,
                substrateDto.Roughness.Value, substrateDto.Roughness.Fix, wavelength);

            foreach (var structureDto in sampleDto.Structures)
            {
                int numberOfPeriods = structureDto.Quantity;
                for (int i = 0; i < numberOfPeriods; i++)
                {
                    foreach (var layerDto in structureDto.Layers)
                    {
                        var substance = getElementAndIndexList(layerDto.Formula);
                        layers.Add(new Layer(substance, layerDto.Density.Value, layerDto.Density.Fix, layerDto.Thickness.Value, layerDto.Thickness.Fix,
                            layerDto.Roughness.Value, layerDto.Roughness.Fix, wavelength));
                    }
                }
            }

            layers.Reverse();
        }

        private List<ElementAndIndex> getElementAndIndexList(string formula)
        {
            var substance = new List<ElementAndIndex>();
            var elements = Regex.Split(formula.Trim(), @"(?<!^)(?=[A-Z])");
            foreach (var el in elements)
            {
                var s = Regex.Split(el, @"(\d.*)");
                var symbol = s[0];
                var index = (s.Count() == 1) ? 1 : double.Parse(s[1], System.Globalization.CultureInfo.InvariantCulture);
                substance.Add(new ElementAndIndex(symbol, index));
            }

            return substance;
        }
    }
}
