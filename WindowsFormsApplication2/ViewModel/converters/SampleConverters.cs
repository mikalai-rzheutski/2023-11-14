using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using WindowsFormsApplication2.Models.XRR;

namespace WindowsFormsApplication2.ViewModel
{
    class SampleConverters
    {
        private const int LayerCol = 1;
        private const int MaterialCol = 2;
        private const int DensityCol = 4;
        private const int DensityFixCol = 5;
        private const int ThicknessCol = 6;
        private const int ThicknessFixCol = 7;
        private const int InterfaceCol = 8;
        private const int RoughnessCol = 9;
        private const int RoughnessFixCol = 10;
        private const int PeriodCol = 11;

        public static SampleDto ConvertTableToSampleDto(string id, DataGridView dataGridView)
        {
            var format = System.Globalization.CultureInfo.InvariantCulture;
            var rows = dataGridView.Rows;
            int rowCnt = rows.Count;

            var substrateRow = rows[rowCnt - 1];

            MediumDto substrate = new MediumDto(substrateRow.Cells[MaterialCol].Value.ToString(),
                new ParameterDto(double.Parse(substrateRow.Cells[DensityCol].Value.ToString(), format), Convert.ToBoolean(substrateRow.Cells[DensityFixCol].Value)),
                new ParameterDto(double.Parse(substrateRow.Cells[RoughnessCol].Value.ToString(), format), Convert.ToBoolean(substrateRow.Cells[RoughnessFixCol].Value)),
                (SurfaceRoughnessTypeDto)Enum.Parse(typeof(SurfaceRoughnessTypeDto), substrateRow.Cells[InterfaceCol].Value.ToString()));


            var structures = new List<StructureDto>();

            for (int i = 0; i < rowCnt - 1; i++)
            {
                SpannedDataGridViewNet2.DataGridViewTextBoxCellEx dataGridViewCell =
                            (SpannedDataGridViewNet2.DataGridViewTextBoxCellEx)dataGridView[PeriodCol, i];
                int span = dataGridViewCell.RowSpan;
                int quantity = int.Parse(rows[i].Cells[PeriodCol].Value.ToString());

                var layers = new List<LayerDto>();
                for (int j = 0; j < span; j++)
                {
                    layers.Add(RowToLayer(rows[i + j]));
                }
                i = i + span - 1;
                structures.Add(new StructureDto(quantity, layers));
            }
            return new SampleDto(id, substrate, structures);
        }

        public static SampleDto ConvertSampleToSampleDto(Sample1 sample)
        {
            var substrateFormula = string.Join("", sample.Substrate.Substance.Select(el => el.ToString()));

            MediumDto substrateDto = new MediumDto(substrateFormula, getParameterDto(sample.Substrate.Density), getParameterDto(sample.Substrate.Roughness),
                (SurfaceRoughnessTypeDto)Enum.Parse(typeof(SurfaceRoughnessTypeDto), sample.Substrate.InterfaceType.ToString()));

            var structuresDto = new List<StructureDto>();
            foreach (var structure in sample.Structures)
            {
                var layersDto = new List<LayerDto>();
                foreach (var layer in structure.Layers)
                {
                    var layerFormula = string.Join("", layer.Substance.Select(el => el.ToString()));
                    layersDto.Add(new LayerDto(layerFormula, getParameterDto(layer.Density), getParameterDto(layer.Thickness), getParameterDto(layer.Roughness), 
                        (SurfaceRoughnessTypeDto)Enum.Parse(typeof(SurfaceRoughnessTypeDto), layer.InterfaceType.ToString())));
                }
                structuresDto.Add(new StructureDto(structure.Quantity, layersDto));
            }

            return new SampleDto(sample.Id, substrateDto, structuresDto);
        }

        private static ParameterDto getParameterDto(Parameter parameter)
        {
            return new ParameterDto(parameter.Value, parameter.Fix);
        }

        private static LayerDto RowToLayer(DataGridViewRow row)
        {
            var format = System.Globalization.CultureInfo.InvariantCulture;

            string layer = row.Cells[LayerCol].Value.ToString();
            string material = row.Cells[MaterialCol].Value.ToString();
            double densityValue = double.Parse(row.Cells[DensityCol].Value.ToString(), format);
            bool densityFix = Convert.ToBoolean(row.Cells[DensityFixCol].Value);
            double thicknessValue = double.Parse(row.Cells[ThicknessCol].Value.ToString(), format);
            bool thicknessFix = Convert.ToBoolean(row.Cells[ThicknessFixCol].Value);
            SurfaceRoughnessTypeDto interfaceType = (SurfaceRoughnessTypeDto)Enum.Parse(typeof(SurfaceRoughnessTypeDto), row.Cells[InterfaceCol].Value.ToString());
            double roughnessValue = double.Parse(row.Cells[RoughnessCol].Value.ToString(), format);
            bool roughnessFix = Convert.ToBoolean(row.Cells[RoughnessFixCol].Value);

            var density = new ParameterDto(densityValue, densityFix);
            var thickness = new ParameterDto(thicknessValue, thicknessFix);
            var roughness = new ParameterDto(roughnessValue, roughnessFix);

            return new LayerDto(material, density, thickness, roughness, interfaceType);
        }
    }
}
