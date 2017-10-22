using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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
        private int _Threshold;         // threshold iteration 
        private int _colorpalette;      // colour palette selection

        private int _Height;            // height of view window
        private int _Width;             // width of view window
        private bool _isRendering;      // indicates current rendering
        private bool _isResizing;

        public Form1()
        {
            InitializeComponent();

            _isRendering = false;
            _isResizing = false;
            toolStripProgressBar.Visible = false;

            // colour palettes
            int pos = comboBoxPalette.Items.Add("Greyscale");
            SetDefaultValues();
        }
        
        /// <summary>
        /// 
        /// </summary>
        private void Form1_Load(object sender, EventArgs e)
        {
        }


        private void Form1_Shown(object sender, EventArgs e)
        {
            //Render();
        }

        //
        private void buttonRender_Click(object sender, EventArgs e)
        {
            Render();
        }

        private void buttonReset_Click(object sender, EventArgs e)
        {
            SetDefaultValues();
            Render();
        }

        private void SetDefaultValues()
        {
            _minX = -2.4;
            _maxX = 1.0;
            _minY = -1.8;
            _maxY = 1.8;
            _colorpalette = 1;
            _Threshold = 20;

            _Width = PictureBox.Width;
            _Height = PictureBox.Height;

            comboBoxPalette.SelectedIndex = 0;
            textBoxThreshold.Text = _Threshold.ToString();
        }

        private void comboBoxPalette_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            _isResizing = true;
        }

        /// <summary>
        /// Render the image if the user has resized the window
        /// </summary>
        private void Form1_ResizeEnd(object sender, EventArgs e)
        {
            if (_isResizing)
            {
                _isResizing = false;
                _Width = PictureBox.Width;
                _Height = PictureBox.Height;

                Render();
            }
        }

        /// <summary>
        /// Main Rendering Code
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        private void Render()
        {
            // Get the current threshold value in the edit box.
            // We need to validate this and alert the user if wrong.
            if( !Int32.TryParse( textBoxThreshold.Text, out _Threshold ) )
            {
                MessageBox.Show("Please enter a valid numeric threshold", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            _isRendering = true;

            Bitmap bitmap = new Bitmap(_Width, _Height);
            Graphics graphics = PictureBox.CreateGraphics();

            // start progress
            toolStripProgressBar.Value = 0;
            toolStripProgressBar.Visible = true;

            graphics.FillRectangle(new SolidBrush(Color.White), 0, 0, Width, Height);

            for(int y = 0; y < _Height; y++)
            {
                for(int x =0; x < _Width; x++)
                {
                    Color clr = CalculateEscapeColour(x, y);
                    bitmap.SetPixel(x, y, clr);
                }

                graphics.DrawImage(bitmap, 0, 0);

                // calculate progress
                int perc = ((_Width * y) * 100) / (_Height * _Width);
                toolStripProgressBar.Value = perc;
            }

            toolStripProgressBar.Value = 100;
            toolStripProgressBar.Visible = false;
            PictureBox.Image = bitmap;

            _isRendering = false;
        }

        /// <summary>
        /// Main calculation
        /// </summary>
        /// <param name="Width"></param>
        /// <param name="Height"></param>
        /// <param name="y"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        private Color CalculateEscapeColour(int x, int y)
        {
            aComplexNumber Z = new aComplexNumber(0, 0);
            aComplexNumber C = new aComplexNumber();

            TransformCoord(x, y, ref C);

            int i = 0;
            for (i = 0; i < _Threshold; i++)
            {
                aComplexNumber Zn = Z.Squared() + C;
                if (Zn.Magnitude() >= 2)
                {
                    // calculate escape colour based on the number of iterations
                    int intensity = (i * 255) / _Threshold;
                    Color color = Color.FromArgb(intensity, intensity, intensity);
                    return color;
                }

                Z = Zn;
            }

            return Color.Black;
        }



        /// <summary>
        /// Displays the coordinates on the screen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isRendering)
                return; 

            aComplexNumber C = new aComplexNumber();
            TransformCoord(e.X, e.Y, ref C);

            // output the coordinates to the screen
            statusLabel.Text = $"x: {C.Real:0.###} y:{C.Imaginary:0.###}";
        }

        /// <summary>
        /// Transform the given screen coordinate to the represented complex number.
        /// </summary>
        private void TransformCoord(Point screenPoint, ref aComplexNumber complexPoint)
        {
            TransformCoord(screenPoint.X, screenPoint.Y, ref complexPoint);
        }

        /// <summary>
        /// </summary>
        private void TransformCoord(int x, int y, ref aComplexNumber complexPoint)
        {
            complexPoint.Real = ((x * (_maxX - _minX)) / _Width) + _minX;
            complexPoint.Imaginary = (((_Height - y) * (_maxY - _minY)) / _Height) + _minY;
        }

    }
}
