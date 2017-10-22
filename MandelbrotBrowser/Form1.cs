using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MandelbrotBrowser
{
    public partial class Form1 : Form
    {

        private double _minX;
        private double _maxX;
        private double _minY;
        private double _maxY;

        public Form1()
        {
            InitializeComponent();

            _minX = -2.0;
            _maxX = 2.0;
            _minY = -2.0;
            _maxY = 2.0;
        }

        //
        private void buttonRender_Click(object sender, EventArgs e)
        {
            Render();
        }


        // Main rendering code
        private void Render()
        {
            int Width = PictureBox.Width;
            int Height = PictureBox.Height;

            Bitmap bitmap = new Bitmap(Width, Height);
            Graphics graphics = PictureBox.CreateGraphics();

            for(int y = 0; y < Height; y++)
            {
                for(int x =0; x < Width; x++)
                {
                    Color clr = CalculateEscapeColour(Width, Height, y, x);
                    bitmap.SetPixel(x, y, clr);
                }
                graphics.DrawImage(bitmap, 0, 0);
            }
            graphics.DrawImage(bitmap, 0, 0);
        }


        /***************************************************************************
         * 
         ****************************************************************************/
        private Color CalculateEscapeColour(int Width, int Height, int y, int x)
        {
            // transform current coordinates to the complex number equivalent
            double re = ((x * (_maxX - _minX)) / (double)Width) + _minX;
            double img = ((y * (_maxY - _minY)) / (double)Height) + _minY;

            aComplexNumber Z = new aComplexNumber(0, 0);
            aComplexNumber C = new aComplexNumber(re, img);

            int i = 0;
            for (i = 0; i < 20; i++)
            {
                aComplexNumber Zn = Z.Squared() + C;
                if (Zn.Magnitude() >= 2)
                    return Color.Black;

                Z = Zn;
            }

            // calculate escape colour based on the number of iterations
            int intensity = (i * 255) / 20;
            Color color = Color.FromArgb(intensity, intensity, intensity);
            return color;
        }
    }
}
