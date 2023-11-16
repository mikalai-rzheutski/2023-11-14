using System;
using System.Linq;

namespace WindowsFormsApplication2.Models
{
    class MyRandom
    {
        ulong k;

        public MyRandom(ulong k)
        {
            this.k = 193548387;
        }

        public double NextDouble()
        {
            k = RotateAndGetMiddleNineDigits(k*k);

            int digits = (int)Math.Ceiling(Math.Log10(k));
            double d = k / Math.Pow(10, digits);
            return d;
        }

        public int Next(int min, int max)
        {
            return min + (int)((max - min) * NextDouble());
        }

        private ulong RotateAndGetMiddleNineDigits(ulong z)
        {
            var s = z.ToString().ToCharArray();
            Array.Reverse(s);
            int k = s.Count();
            int startIndex = (int)Math.Floor(0.5 * (s.Count() - 9));
            char[] sub = new char[9];
            Array.Copy(s, startIndex, sub, 0, 9);

            return ulong.Parse(new string(sub));
        }
    }
}
