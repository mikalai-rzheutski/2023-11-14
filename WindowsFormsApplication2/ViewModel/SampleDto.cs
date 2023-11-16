using System.Collections.Generic;

namespace WindowsFormsApplication2.ViewModel
{
    public class SampleDto
    {
        string id;
        MediumDto substrate;
        List<StructureDto> structures;

        public string Id => id;
        public MediumDto Substrate => substrate;
        public List<StructureDto> Structures => structures;

        public SampleDto(string id, MediumDto substrate, List<StructureDto> structures)
        {
            this.id = id;
            this.substrate = substrate;
            this.structures = structures;
        }

        public override string ToString()
        {
            string s = "";
            foreach (var structure in this.structures)
            {
                s = s + structure.ToString() + "\r\n";
            }

            return "Sample Id: " + id + "\r\n" +
                "Substrate: " + substrate.ToString() + "\r\n"
                + "Structures:  " + s;
        }
    }

    public class StructureDto
    {
        int quantity;
        List<LayerDto> layers;

        public int Quantity => quantity;
        public List<LayerDto> Layers => layers;

        public StructureDto(int quantity, List<LayerDto> layers)
        {
            this.quantity = quantity;
            this.layers = layers;
        }

        public override string ToString()
        {
            string s = "";
            foreach (var layer in this.layers)
            {
                s = s + layer.ToString() + ", ";
            }
            return quantity.ToString() + "x[" + s + "]";
        }
    }

    public class LayerDto : MediumDto
    {
        ParameterDto thickness;

        public ParameterDto Thickness => thickness;

        public LayerDto(string formula, ParameterDto density, ParameterDto thickness, ParameterDto roughness,
            SurfaceRoughnessTypeDto interfaceType) : base(formula, density, roughness, interfaceType)
        {
            this.thickness = thickness;
        }

        public override string ToString()
        {
            return "Layer: " + base.ToString() + ", Thickness: " + thickness.ToString();
        }
    }

    public class MediumDto
    {
        string formula;
        ParameterDto density;
        ParameterDto roughness;
        SurfaceRoughnessTypeDto interfaceType;

        public string Formula => formula;
        public ParameterDto Density => density;
        public ParameterDto Roughness => roughness;
        public SurfaceRoughnessTypeDto InterfaceType => interfaceType;

        public MediumDto(string formula, ParameterDto density, ParameterDto roughness, SurfaceRoughnessTypeDto interfaceType)
        {
            this.formula = formula;
            this.density = density;
            this.roughness = roughness;
            this.interfaceType = interfaceType;
        }

        public override string ToString()
        {
            return formula + ", Density: " + density.ToString() + ", Roughness: " + roughness.ToString() + 
                ", Interface: " + interfaceType.ToString();
        }
    }

    public class ParameterDto
    {
        double value;
        bool fix;

        double minValue;
        double maxValue;

        public double Value => value;
        public bool Fix => fix;

        public double MinValue => minValue;
        public double MaxValue => maxValue;

        public ParameterDto(double value, bool fix)
        {
            this.value = value;
            this.fix = fix;
            minValue = value / 2;
            maxValue = value * 2;
        }

        public ParameterDto(double value, bool fix, double minValue, double maxValue)
        {
            this.value = value;
            this.fix = fix;
            this.minValue = minValue;
            this.maxValue = maxValue;
        }

        public override string ToString()
        {
            string fix = this.fix ? "(fixed)" : "(unfixed)";
            return value.ToString() + fix; 
        }
    }

    public enum SurfaceRoughnessTypeDto
    {
        Gauss,
        ErrorFunction
    }
}