using System;

namespace System.Drawing
{
    public static class ColorExtensions
    {
        public static void HSVToRGB(this Color clr, double H, double S, double V, out double R, out double G, out double B)
        {
            if (H == 1.0)
            {
                H = 0.0;
            }

            Double step = 1.0 / 6.0;
            Double vh = H / step;

            int i = (int)System.Math.Floor(vh);

            Double f = vh - i;
            Double p = V * (1.0 - S);
            Double q = V * (1.0 - (S * f));
            Double t = V * (1.0 - (S * (1.0 - f)));

            switch (i)
            {
                case 0:
                    {
                        R = V;
                        G = t;
                        B = p;
                        break;
                    }
                case 1:
                    {
                        R = q;
                        G = V;
                        B = p;
                        break;
                    }
                case 2:
                    {
                        R = p;
                        G = V;
                        B = t;
                        break;
                    }
                case 3:
                    {
                        R = p;
                        G = q;
                        B = V;
                        break;
                    }
                case 4:
                    {
                        R = t;
                        G = p;
                        B = V;
                        break;
                    }
                case 5:
                    {
                        R = V;
                        G = p;
                        B = q;
                        break;
                    }
                default:
                    {
                        // not possible - if we get here it is an internal error
                        throw new ArgumentException();
                    }
            }
        }
    }
}
