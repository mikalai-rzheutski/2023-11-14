using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WindowsFormsApplication2.ViewModel;

namespace WindowsFormsApplication2.Models.XRR
{
    class Sample1
    {
        string id;
        Medium substrate;
        List<Structure> structures;

        public string Id => id;
        public Medium Substrate => substrate;
        public List<Structure> Structures => structures;

        public Sample1(string id, Medium substrate, List<Structure> structures)
        {
            this.id = id;
            this.substrate = substrate;
            this.structures = structures;
        }

        public Sample1(SampleDto sampleDto, double wavelength)
        {
            id = sampleDto.Id;
            structures = new List<Structure>();

            substrate = new Medium(sampleDto.Substrate.Formula, getParameter(sampleDto.Substrate.Density),
                getParameter(sampleDto.Substrate.Roughness), getInterfaceType(sampleDto.Substrate.InterfaceType), wavelength);

            foreach (var structureDto in sampleDto.Structures)
            {
                var layers = new List<Layer>();
                foreach (var layerDto in structureDto.Layers)
                {
                    layers.Add(new Layer(layerDto.Formula, getParameter(layerDto.Density), getParameter(layerDto.Thickness),
                        getParameter(layerDto.Roughness), getInterfaceType(layerDto.InterfaceType), wavelength));
                }
                structures.Add(new Structure(structureDto.Quantity, layers));
            }
        }

        public List<Parameter> getParameters()
        {
            var parameters = new List<Parameter>();
            parameters.Add(substrate.Density);
            parameters.Add(substrate.Roughness);
            foreach (var structure in structures)
            {
                foreach (var layer in structure.Layers)
                {
                    parameters.Add(layer.Density);
                    parameters.Add(layer.Thickness);
                    parameters.Add(layer.Roughness);
                }
            }
            return parameters;
        }

        public void UpdateParameters(List<double> parameters)
        {
            substrate.updateDensity(parameters[1]);
            substrate.Roughness.Value = parameters[2];
            int index = 3;
            foreach (var structure in structures)
            {
                foreach (var layer in structure.Layers)
                {
                    layer.updateDensity(parameters[index++]);
                    layer.Thickness.Value = parameters[index++];
                    layer.Roughness.Value = parameters[index++];
                }
            }
        }

        private Parameter getParameter(ParameterDto parameterDto)
        {
            return new Parameter(parameterDto.Value, parameterDto.Fix, parameterDto.MinValue, parameterDto.MaxValue);
        }

        private SurfaceRoughnessType getInterfaceType(SurfaceRoughnessTypeDto surfaceRoughnessTypeDto)
        {
            return (SurfaceRoughnessType)Enum.Parse(typeof(SurfaceRoughnessType), surfaceRoughnessTypeDto.ToString());
        }
    }

    class Structure
    {
        int quantity;
        List<Layer> layers;

        public int Quantity => quantity;
        public List<Layer> Layers => layers;

        public Structure(int quantity, List<Layer> layers)
        {
            this.quantity = quantity;
            this.layers = layers;
        }
    }

    class Layer : Medium
    {
        Parameter thickness;

        public Parameter Thickness
        {
            get { return thickness; }
        }

        public Layer(string formula, Parameter density, Parameter thickness, Parameter roughness,
            SurfaceRoughnessType interfaceType, double wavelength) : base(formula, density, roughness, interfaceType, wavelength)
        {
            this.thickness = thickness;
        }
    }

    class Medium
    {
        const double AtomicMassUnit = 1.6605390666e-27;

        Parameter density;
        Parameter roughness;
        SurfaceRoughnessType interfaceType;
        List<ElementAndIndex> substance;

        double electronDensity;
        double attenuation;

        double atomicMassToZ;
        double massAttenuationCoeff;

        public Parameter Density
        {
            get { return density; }
        }

        public void updateDensity(double density)
        {
            this.density.Value = density;
            electronDensity = 1000 * density / atomicMassToZ;
            attenuation = 100 * density * massAttenuationCoeff;
        }

        public Parameter Roughness
        {
            get { return roughness; }
        }

        public SurfaceRoughnessType InterfaceType
        {
            get { return interfaceType; }
        }

        public List<ElementAndIndex> Substance => substance;

        public double ElectronDensity => electronDensity;
        public double Attenuation => attenuation;

        public Medium(string formula, Parameter density, Parameter roughness, SurfaceRoughnessType interfaceType, double wavelength)
        {
            substance = getElementAndIndexList(formula);
            this.density = density;
            this.roughness = roughness;
            this.interfaceType = interfaceType;

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
            electronDensity = 1000 * density.Value / atomicMassToZ;

            massAttenuationCoeff = att / molecularMass;
            attenuation = 100 * density.Value * massAttenuationCoeff;
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

    class ElementAndIndex
    {
        public string symbol;
        public double index;

        public ElementAndIndex(string symbol, double index)
        {
            this.symbol = symbol;
            this.index = index;
        }


        public override string ToString()
        {
            return symbol + ((index == 1) ? "" : index.ToString());
        }
    }

    class Parameter
    {
        double val;
        bool fix;

        double minValue;
        double maxValue;

        public double Min => minValue;
        public double Max => maxValue;
        public bool Fix => fix;

        public double Value
        {
            get { return val; }
            set { val = value; }
        }

        public Parameter(double value, bool fix, double minValue, double maxValue)
        {
            this.val = value;
            this.fix = fix;
            this.minValue = minValue;
            this.maxValue = maxValue;
        }
    }

    enum SurfaceRoughnessType
    {
        Gauss,
        ErrorFunction
    }
}