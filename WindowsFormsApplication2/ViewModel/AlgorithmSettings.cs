namespace WindowsFormsApplication2.ViewModel
{
    enum XrrAlgorithm
    {
        DifferentialEvolution,
        LevenbergMarquardt
    }

    enum FigureOfMerit
    {
        Log,
        ChiSquare,
        LeastSquares,
        LeastLogSquares
    }

    abstract class XrrFitSettings
    {
        public XrrAlgorithm Algorithm { get; protected set; }
        public FigureOfMerit Fom { get; protected set; }
        public double Xmin { get; protected set; }
        public double Xmax { get; protected set; }
    }

    class DifferentialEvolutionSettings : XrrFitSettings
    {
        public double Km { get; }
        public double Kr { get; }
        public ushort PopulationSize { get; }
        public ushort MaxGenerations { get; }

        public DifferentialEvolutionSettings(FigureOfMerit fom, double xmin, double xmax,
            double km, double kr, ushort populationSize, ushort maxGenerations)
        {
            Algorithm = XrrAlgorithm.DifferentialEvolution;
            Fom = fom;
            Xmin = xmin;
            Xmax = xmax;
            Km = km;
            Kr = kr;
            PopulationSize = populationSize;
            MaxGenerations = maxGenerations;
        }
    }

    class LevenbergMarquardtSettings : XrrFitSettings
    {
        public ushort MaxIterations { get; }

        public LevenbergMarquardtSettings(FigureOfMerit fom, double xmin, double xmax,
            ushort maxIterations)
        {
            Algorithm = XrrAlgorithm.LevenbergMarquardt;
            Fom = fom;
            Xmin = xmin;
            Xmax = xmax;
            MaxIterations = maxIterations;
        }
    }
}