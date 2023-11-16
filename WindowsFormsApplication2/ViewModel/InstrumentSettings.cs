namespace WindowsFormsApplication2.ViewModel
{
    class InstrumentSettings
    {
        double wavelength;
        double resolution;
        double beamWidth;
        double sampleSize;
        double scaling;

        public double Wavelength => wavelength;
        public double Resolution => resolution;
        public double BeamWidth => beamWidth;
        public double SampleSize => sampleSize;
        public double Scaling => scaling;

        public InstrumentSettings(double wavelength, double resolution, double beamWidth, double sampleSize, double scaling)
        {
            this.wavelength = wavelength;
            this.resolution = resolution;
            this.beamWidth = beamWidth;
            this.sampleSize = sampleSize;
            this.scaling = scaling;
        }

        public void UpdateScaling(double scaling)
        {
            this.scaling = scaling;
        }
    }
}
