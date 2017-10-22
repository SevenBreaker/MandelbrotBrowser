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
    public enum PaletteType
    {
        GreyScalePalette = 0,
        RedBlueShift = 1
    }

    public partial class Form1 : Form
    {

        private double _minX;
        private double _maxX;
        private double _minY;
        private double _maxY;
        private int _Threshold;         // threshold iteration 

        private int _Height;            // height of view window
        private int _Width;             // width of view window
        private bool _isRendering;      // indicates current rendering
        private bool _isResizing;

        // mouse drag resize
        private bool _isZooming = false;
        private Point _startZoomPoint = Point.Empty;
        private Rectangle _ZoomWindow;
        private Brush _ZoomSelectionBrush = new SolidBrush(Color.FromArgb(128, 72, 145, 220));


        private int _palettesize = 256;
        private List<Color> _colourtable;

        public Form1()
        {
            InitializeComponent();

            _isRendering = false;
            _isResizing = false;
            _colourtable = new List<Color>(_palettesize);
            toolStripProgressBar.Visible = false;

            // colour palettes
            int pos = comboBoxPalette.Items.Add("Greyscale");
            pos = comboBoxPalette.Items.Add("Red-Blue Shift");
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
            _Threshold = 20;

            _Width = PictureBox.Width;
            _Height = PictureBox.Height;

            comboBoxPalette.SelectedIndex = 0;
            textBoxThreshold.Text = _Threshold.ToString();
        }

        private void comboBoxPalette_SelectedIndexChanged(object sender, EventArgs e)
        {
            statusLabel.Text = $"Colour palette changed to {comboBoxPalette.GetItemText(comboBoxPalette.SelectedItem)}";
            BuildColourPalette();
        }

        private void BuildColourPalette()
        {
            _colourtable.Clear();
            int whichPalette = comboBoxPalette.SelectedIndex;

            switch(whichPalette)
            {
                case (int)PaletteType.RedBlueShift:
                    {
                        Color start = Color.DarkRed;
                        Color end = Color.BlueViolet;
                        float deltaR = (end.R - start.R) / (float)_palettesize;
                        float deltaG = (end.G - start.G) / (float)_palettesize;
                        float deltaB = (end.B - start.B) / (float)_palettesize;
                        float fR = start.R;
                        float fG = start.G;
                        float fB = start.B;

                        for (int i = 0; i < _palettesize; i++)
                        {
                            fR += deltaR;
                            fG += deltaG;
                            fB += deltaB;
                            _colourtable.Add(Color.FromArgb((int)(fR),(int)fG, (int)fB));
                        }
                    }
                    break;
                case (int)PaletteType.GreyScalePalette:
                default:
                    {
                        for (int i = 0; i < _palettesize; i++)
                            _colourtable.Add(Color.FromArgb(i, i, i));
                    }
                    break;
            }

            Debug.Assert(_colourtable.Count == _palettesize);
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

            // Prevents other updates from happening while we are drawing to the screen
            _isRendering = true;

            Bitmap bitmap = new Bitmap(_Width, _Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            Graphics graphics = PictureBox.CreateGraphics();

            // start progress
            Stopwatch stopwatch = new Stopwatch();
            toolStripProgressBar.Value = 0;
            toolStripProgressBar.Visible = true;

            // clear existing screen
            /*graphics.FillRectangle(new SolidBrush(Color.White), 0, 0, Width, Height);*/

            aComplexNumber Z = new aComplexNumber();
            aComplexNumber C = new aComplexNumber();

            stopwatch.Start();

            for (int y = 0; y < _Height; y++)
            {
                for(int x =0; x < _Width; x++)
                {
                    // Set initial value of Zn to 0+0i
                    Z.Real = 0;
                    Z.Imaginary = 0;

                    // get the imaginary coordinate that is represented by our current position
                    TransformCoord(x, y, ref C);

                    // To test for divergence, we perform an iterative loop of the form:
                    // Zn+1 = Zn^2 + C
                    //
                    // If the magnitude of Z goes above 2, then it is within the Mandelbrot set.
                    double ZrSq = Z.Real * Z.Real;
                    double ZiSq = Z.Imaginary * Z.Imaginary;

                    int iteration = 0;
                    for (iteration = 0; iteration < _Threshold; iteration++)
                    {
#if false
                        aComplexNumber Zn = Z.Squared() + C;
                        if (Zn.IsDivergent())
                            break;

                        Z = Zn;
#endif

                        // optimisation of above code

                        // Test for divergence
                        // No need to do the square root for a little perf. increase.
                        if (ZrSq + ZiSq > 4.0)
                            break;

                        Z.Imaginary = Z.Real * Z.Imaginary;
                        Z.Imaginary += Z.Imaginary;
                        Z.Imaginary += C.Imaginary;

                        Z.Real = ZrSq - ZiSq + C.Real;

                        ZrSq = Z.Real * Z.Real;
                        ZiSq = Z.Imaginary * Z.Imaginary;
                    }

                    if (iteration >= _Threshold)
                    {
                        // point is in the mandelbrot set.
                        // Color to the first item in our color table.
                        bitmap.SetPixel(x, y, Color.Black);
                    }
                    else
                    {
                        // Retrieve the colour that will represent this pixel.
                        // This will be an index into our colour table, based on the value of our cycle value.
                        int colourindex = (iteration * (_palettesize - 1)) / _Threshold;
                        Debug.Assert(colourindex >= 0);
                        Debug.Assert(colourindex < _palettesize);

                        if (colourindex > _palettesize)
                            colourindex = _palettesize - 1;

                        bitmap.SetPixel(x, y, _colourtable[colourindex]);
                    }
                }

                // calculate and show progress
                graphics.DrawImage(bitmap, 0, 0);

                int perc = ((_Width * y) * 100) / (_Height * _Width);
                toolStripProgressBar.Value = perc;
            }

            toolStripProgressBar.Value = 100;
            toolStripProgressBar.Visible = false;

            if (PictureBox.Image != null)
                PictureBox.Image.Dispose();

            PictureBox.Image = bitmap;
            graphics.Dispose();

            // finished rendering
            _isRendering = false;

            // display elapsed time.
            stopwatch.Stop();
            statusLabel.Text = $"Rendered in: {stopwatch.Elapsed.TotalSeconds:0.#} seconds";
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


        /// <summary>
        /// Mouse dbl-click handler
        /// </summary>
        private void PictureBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // ZOOM In
            if(e.Button == MouseButtons.Left)
            {
                int pointX = e.X;
                int pointY = e.Y;
                aComplexNumber complexPoint = new aComplexNumber();
                TransformCoord(pointX, pointY, ref complexPoint);

                double windowWidthCentre = Math.Abs(_maxX - _minX) / 2.0;
                double windowHeightCentre = Math.Abs(_maxY - _minY) / 2.0;

                // Zoom in
                windowHeightCentre /= 2;
                windowWidthCentre /= 2;

                _minX = complexPoint.Real - windowWidthCentre;
                _maxX = complexPoint.Real + windowWidthCentre;
                _minY = complexPoint.Imaginary - windowHeightCentre;
                _maxY = complexPoint.Imaginary + windowHeightCentre;

                /****************************************************
                // can we copy this window and stretch
                Bitmap bitmap = new Bitmap(_Width, _Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                using (Graphics g = Graphics.FromImage((Image)bitmap))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(PictureBox.Image, 0, 0, size.Width, size.Height);
                }
                if (PictureBox.Image != null)
                    PictureBox.Image.Dispose();
                PictureBox.Image = bitmap;
                *****************************************************/

                Render();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="centrePoint"></param>
        /// <param name="zoomFactor"></param>
        private void Zoom(aComplexNumber centrePoint, float zoomFactor)
        {
            throw new NotImplementedException();
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

            if(_isZooming)
            {
                Point endZoomPoint = new Point(e.X, e.Y);

                Region r = new Region(PictureBox.DisplayRectangle);
                _ZoomWindow = new Rectangle(
                                            Math.Min(_startZoomPoint.X, endZoomPoint.X),
                                            Math.Min(_startZoomPoint.Y, endZoomPoint.Y),
                                            Math.Abs(_startZoomPoint.X - endZoomPoint.X),
                                            Math.Abs(_startZoomPoint.Y - endZoomPoint.Y));

                PictureBox.Invalidate();
                return;
            }

            aComplexNumber C = new aComplexNumber();
            TransformCoord(e.X, e.Y, ref C);

            // output the coordinates to the screen
            statusLabel.Text = $"x: {C.Real:0.###} y:{C.Imaginary:0.###}";
        }


        private void PictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Left)
            {
                _startZoomPoint.X = e.X;
                _startZoomPoint.Y = e.Y;
                _isZooming = true;
            }
        }

        private void PictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isZooming = false;
                Point endZoomPoint = new Point(e.X, e.Y);

                // Call zoom function
            }
        }


        private void PictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (_isZooming)
            {
                // Draw the rectangle...
                if (PictureBox.Image != null)
                {
                    if (_ZoomWindow != null && _ZoomWindow.Width > 0 && _ZoomWindow.Height > 0)
                    {
                        e.Graphics.FillRectangle(_ZoomSelectionBrush, _ZoomWindow);
                    }
                }
            }

        }
    }
}
