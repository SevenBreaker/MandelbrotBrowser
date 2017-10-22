using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MandelbrotBrowser
{

    // Represents a complext number
    // a + bi
    public struct aComplexNumber
    {
        private double _real;
        private double _imaginary;

        public aComplexNumber(double r, double i)
        {
            _real = r;
            _imaginary = i;
        }

        public aComplexNumber(aComplexNumber c)
        {
            _real = c._real;
            _imaginary = c._imaginary;
        }

        public double Real { get => _real; set => _real = value; }
        public double Imaginary { get => _imaginary; set => _imaginary = value; }

        // Adddition
        public static aComplexNumber operator +(aComplexNumber a, aComplexNumber b)
        {
            return new aComplexNumber(a.Real + b.Real, a.Imaginary + b.Imaginary);
        }

        // Multiplication
        //
        //   (a + bi)(c + di) = ac + adi + cbi + (bi)(di)
        // = ac + (ad + cb)i + bdi^2
        // = (ac - bd) + (ad + cb)i
        public static aComplexNumber operator *(aComplexNumber c1, aComplexNumber c2)
        {
            double r = (c1.Real * c2.Real) - (c1.Imaginary * c2.Imaginary);
            double i = (c1.Real * c2.Imaginary) + (c2.Real * c1.Imaginary);

            return new aComplexNumber(r, i);
        }

        // POWER OF TWO
        //
        // (a + bi)^2
        // 
        // = a^2 + 2abi + (bi)^2
        // = a^2 + 2abi - b^2
        // = (a^2 - b^2) + 2abi
        //
        public aComplexNumber Squared()
        {
            return new aComplexNumber((Real * Real) - (Imaginary * Imaginary), (2 * Real * Imaginary));
        }

        // Absolute value or Magnitude or Modulus
        //
        // m = |a+bi|
        public double Magnitude()
        {
            return Math.Sqrt((Real * Real) + (Imaginary * Imaginary));
        }

        // Divergence (Mandelbrot Set)
        // This test for divergence of the complex number.
        // The complex number is said to diverge if:
        // |Z| >= 2
        //
        public bool IsDivergent()
        {
            // We *could* just do the straightforward test for the magnitude
            // But there is a more efficient way.
            // return Magnitude() > 2;

            return ((Real * Real) + (Imaginary * Imaginary)) > 4;
        }


        // Displays a string representation of our complex number
        public override string ToString()
        {
            return $"{Imaginary} + {Real}i";
        }
    }
}
