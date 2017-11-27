using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MandelbrotBrowser
{
    public enum PaletteType
    {
        Dark = 0,
        Hues = 1
    }
    
    public partial class Form1 : Form
    {
        private MandelbrotPlane _complexWindow = new MandelbrotPlane(); // Mandelbrot complex window
        private Bitmap _canvas;                                         // bitmap that we will be drawing our images

        private bool _isRendering;      // indicates current rendering
        private bool _isResizing;       //

        // mouse drag/move
        private Graphics _panninggrapics;
        private bool _isPanning = false;
        private bool _isZooming = false;
        private Point _startPoint = Point.Empty;
        private Rectangle _ZoomWindow;
        private Pen _ZoomSelectionPen = new Pen(Color.Gold);

        private int _palettesize = 256;
        private List<Color> _colourtable;


        /**************************************************************************
        * Main Form Class 
        ***************************************************************************/
        public Form1()
        {
            InitializeComponent();

            // set the form title
            Text += " - " + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;

            //
            _canvas = new Bitmap(myPictureBox.Width, myPictureBox.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

            _isRendering = false;
            _isResizing = false;
            _colourtable = new List<Color>(_palettesize);
            toolStripProgressBar.Visible = false;

            // colour palettes
            comboBoxPalette.Items.Add("Dark");
            comboBoxPalette.Items.Add("Hues");

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

        // Button Handler: Render image
        private void buttonRender_Click(object sender, EventArgs e)
        {
            Render();
        }

        // Button Handler: Reset to default
        private void buttonReset_Click(object sender, EventArgs e)
        {
            SetDefaultValues();
            Render();
        }

        private void SetDefaultValues()
        {
            _complexWindow = new MandelbrotPlane();
            comboBoxPalette.SelectedIndex = 0;
            textBoxThreshold.Text = _complexWindow.MaxThreshold.ToString();
        }

        private void comboBoxPalette_SelectedIndexChanged(object sender, EventArgs e)
        {
            statusLabel.Text = $"Colour palette changed to {comboBoxPalette.GetItemText(comboBoxPalette.SelectedItem)}";
            BuildColourPalette();
        }

        // Build a look table of our colour palette. We will use the iteration count to 
        // find the colour that we will use to set the screen pixel.
        private void BuildColourPalette()
        {
            _colourtable.Clear();
            int whichPalette = comboBoxPalette.SelectedIndex;

            switch(whichPalette)
            {
                /*
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
                    break;*/
                case (int)PaletteType.Dark:
                    {
                        for (int i = 0; i < _palettesize; i++)
                            _colourtable.Add(Color.FromArgb(i, i, i));
                    }
                    break;
                case (int)PaletteType.Hues:
                default:
                    {
                        double r;
                        double g;
                        double b;
                        double h;
                        double s = 1.0;
                        double v = 1.0;
                        Color clr = new Color();

                        for (int i = 0; i < _palettesize; i++)
                        {
                            h = ((double)i / _palettesize);
                            clr.HSVToRGB(h, s, v, out r, out g, out b);
                            _colourtable.Add(Color.FromArgb((int)(r*255), (int)(g*255), (int)(b*255)));
                        }
                    }
                    break;
            }

            Debug.Assert(_colourtable.Count == _palettesize);
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            _isResizing = true;
        }

        /**************************************************************************
        * Render the image, if window resizing is finished
        ***************************************************************************/
        private void Form1_ResizeEnd(object sender, EventArgs e)
        {
            if (_isResizing)
            {
                _isResizing = false;

                // stretch zoom
                Bitmap bitmap = new Bitmap(myPictureBox.Width, myPictureBox.Height, PixelFormat.Format32bppPArgb);
                using (Graphics g = Graphics.FromImage((Image)bitmap))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Default;
                    g.DrawImage(_canvas, myPictureBox.ClientRectangle, 0, 0, _canvas.Width, _canvas.Height, GraphicsUnit.Pixel);

                    if (_canvas != null)
                        _canvas.Dispose();

                    _canvas = (Bitmap)bitmap.Clone();
                }

                myPictureBox.Invalidate();
                Render();
            }
        }



        /**************************************************************************
        * Main Rendering code 
        ***************************************************************************/
        private void Render()
        {
            // Get the current threshold value in the edit box.
            // We need to validate this and alert the user if wrong.
            int maxthreshold;
            if( !Int32.TryParse( textBoxThreshold.Text, out maxthreshold ) )
            {
                MessageBox.Show("Please enter a valid numeric threshold", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            _complexWindow.MaxThreshold = maxthreshold;

            // Prevents other updates from happening while we are drawing to the screen
            _isRendering = true;

            // start progress
            Stopwatch stopwatch = new Stopwatch();
            toolStripProgressBar.Value = 0;
            toolStripProgressBar.Visible = true;
            stopwatch.Start();  // start timer

            // Here we are using an progress update pattern that works for async/await models.
            // see: https://blog.stephencleary.com/2012/02/reporting-progress-from-async-tasks.html
            //
            var calcprogress = new Progress<int>((p) => toolStripProgressBar.Value = p);

            bool isSymmetric = Math.Abs(_complexWindow.MinY) == Math.Abs(_complexWindow.MaxY);

            for (int y = 0; y < myPictureBox.Height; y++)
            {
                for(int x =0; x < myPictureBox.Width; x++)
                {
                    int iterationTaken = TestForMandelbrotSetAsync(x, y);

                    if (iterationTaken == 0)
                    {
                        // point is in the mandelbrot set.
                        // Color to the first item in our color table.
                        _canvas.SetPixel(x, y, Color.Black);

                        if(isSymmetric)
                            _canvas.SetPixel(x, myPictureBox.Height - y - 1, Color.Black);

                    }
                    else
                    {
                        // Retrieve the colour that will represent this pixel.
                        // This will be an index into our colour table, based on the value of our cycle value.
                        int colourindex = (iterationTaken * (_palettesize - 1)) / _complexWindow.MaxThreshold;
                        Debug.Assert(colourindex >= 0);
                        Debug.Assert(colourindex < _palettesize);

                        if (colourindex > _palettesize)
                            colourindex = _palettesize - 1;

                        _canvas.SetPixel(x, y, _colourtable[colourindex]);

                        if (isSymmetric)
                            _canvas.SetPixel(x, myPictureBox.Height - y - 1, _colourtable[colourindex]);
                    }

                } // for(int x =0; x < myPictureBox.Width; x++)

                // Progress report
                int perc = ((myPictureBox.Width * y) * 100) / (myPictureBox.Height * myPictureBox.Width);
                ((IProgress<int>)calcprogress).Report(perc);

                myPictureBox.Invalidate();
                Application.DoEvents();

                // Stop if we are doing a mirror rendering
                if( isSymmetric )
                {
                    if (y >= myPictureBox.Height / 2)
                        break;
                }

            } // for (int y = 0; y < myPictureBox.Height; y++)

            // end progress update
            toolStripProgressBar.Value = 100;
            toolStripProgressBar.Visible = false;

            // finished rendering
            _isRendering = false;

            // display elapsed time.
            stopwatch.Stop();
            statusLabel.Text = $"Rendered in: {stopwatch.Elapsed.TotalSeconds:0.#} seconds";
            myPictureBox.Invalidate();
        }


        /**************************************************************************
        *  Test the given coordinate to see if it is inside the Mandelbrot set
        *  
        *  Returns 0 if it is part of the set.
        *  If not, returns the number of tries reached.
        *  
        ***************************************************************************/
        private int TestForMandelbrotSetAsync(int x, int y)
        {
            int iterationsTaken = 0;
            aComplexNumber Z = new aComplexNumber();
            aComplexNumber C = new aComplexNumber();

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

            for (iterationsTaken = 0; iterationsTaken < _complexWindow.MaxThreshold; iterationsTaken++)
            {
                /***********************************
                aComplexNumber Zn = Z.Squared() + C;
                if (Zn.IsDivergent())
                    break;
                Z = Zn;
                ***********************************/

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

            if (iterationsTaken >= _complexWindow.MaxThreshold)
                return 0;

            // Not inside the Mandelbrot set
            return iterationsTaken;
        }

        /**************************************************************************
        * Transform the given screen coordinate to the represented complex number
        ***************************************************************************/
        private void TransformCoord(Point screenPoint, ref aComplexNumber complexPoint)
        {
            TransformCoord(screenPoint.X, screenPoint.Y, ref complexPoint);
        }


        /**************************************************************************
        * Maps the given screen position to the complex plane being rendered 
        ***************************************************************************/
        private void TransformCoord(int x, int y, ref aComplexNumber complexPoint)
        {
            complexPoint.Real = ((x * (_complexWindow.MaxX - _complexWindow.MinX)) / myPictureBox.Width) + _complexWindow.MinX;
            complexPoint.Imaginary = (((myPictureBox.Height - y) * (_complexWindow.MaxY - _complexWindow.MinY)) / myPictureBox.Height) + _complexWindow.MinY;
        }


        /**************************************************************************
        * Mouse dbl-click handler 
        ***************************************************************************/
        private void PictureBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
        }


        /**************************************************************************
        *  
        ***************************************************************************/
        private void Zoom(Rectangle window)
        {
            // don't bother zooming if the size is too small.
            if (window.Height < 10 || window.Width < 10)
                return;

            // stretch zoom
            using (Bitmap bitmap = new Bitmap(myPictureBox.Width, myPictureBox.Height, PixelFormat.Format32bppPArgb))
            using (Graphics g = Graphics.FromImage((Image)bitmap))
            {
                g.DrawImage(_canvas, myPictureBox.ClientRectangle, window, GraphicsUnit.Pixel);
            }
            myPictureBox.Invalidate(true);

            // HORRIBLE !!
            // Need to use async/await
            Application.DoEvents();

            // calculate new window and render
            aComplexNumber tlPoint = new aComplexNumber();
            aComplexNumber brPoint = new aComplexNumber();

            TransformCoord(window.X, window.Y, ref tlPoint);
            TransformCoord(window.Right, window.Bottom, ref brPoint);

            _complexWindow.MinX = tlPoint.Real;
            _complexWindow.MaxX = brPoint.Real;
            _complexWindow.MinY = brPoint.Imaginary;
            _complexWindow.MaxY = tlPoint.Imaginary;

            Render();      
        }

        /**************************************************************************
        * Handles the mouse movement event 
        * 
        * This handler has a few tasks.
        * . Handles image movement, if we are panning
        * . Updates zoom coordinates, if we are zooming in.
        * 
        ***************************************************************************/
        private void PictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            // If we are still rendering our image, then do no continue
            if (_isRendering)
                return;

            if(_isZooming)
            {
                Point endZoomPoint = new Point(e.X, e.Y);

                _ZoomWindow = new Rectangle(
                                            Math.Min(_startPoint.X, endZoomPoint.X),
                                            Math.Min(_startPoint.Y, endZoomPoint.Y),
                                            Math.Abs(_startPoint.X - endZoomPoint.X),
                                            Math.Abs(_startPoint.Y - endZoomPoint.Y));

                myPictureBox.Invalidate();
                return;
            }

            // we are panning our image, so move to the new position
            if(_isPanning)
            {
                Point endPoint = new Point(e.X, e.Y);

                _panninggrapics.FillRectangle(new SolidBrush(Color.DimGray), myPictureBox.ClientRectangle);
                _panninggrapics.DrawImage(_canvas, (endPoint.X - _startPoint.X), (endPoint.Y - _startPoint.Y));
                return;
            }

            aComplexNumber C = new aComplexNumber();
            TransformCoord(e.X, e.Y, ref C);

            // output the coordinates to the screen
            statusLabel.Text = $"x: {C.Real:0.####} y:{C.Imaginary:0.####}";
        }


        /**************************************************************************
        * Handles the mouse button down event 
        * 
        * We use this handler to start a panning or zoom action.
        ***************************************************************************/
        private void PictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Left)
            {
                // set our starting coordinate
                _startPoint.X = e.X;
                _startPoint.Y = e.Y;

                if(Control.ModifierKeys.HasFlag(Keys.Control))
                {
                    _isZooming = true;
                }
                else
                {
                    // We are panning
                    // create a new graphics object of our PictureBox control. We will use this
                    // to continuously reposition our existing canvas bitmap.
                    _panninggrapics?.Dispose();
                    _panninggrapics = myPictureBox.CreateGraphics();
                    _isPanning = true;
                }
            }
        }


        /**************************************************************************
        * Handler for the mouse button up event 
        * 
        * Use this event to update our image if we have been zooming or panning.
        ***************************************************************************/
        private void PictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (_isZooming)
                {
                    _isZooming = false;
                    Point endZoomPoint = new Point(e.X, e.Y);

                    // Call zoom function
                    Rectangle window = new Rectangle(
                                        Math.Min(_startPoint.X, endZoomPoint.X),
                                        Math.Min(_startPoint.Y, endZoomPoint.Y),
                                        Math.Abs(_startPoint.X - endZoomPoint.X),
                                        Math.Abs(_startPoint.Y - endZoomPoint.Y));

                    Zoom(window);
                }
                else if (_isPanning)
                {
                    _isPanning = false;
                    Point endPoint = new Point(e.X, e.Y);

                    _panninggrapics?.Dispose();
                    _panninggrapics = null;

                    // calculate distance moved on each axis.
                    aComplexNumber start = new aComplexNumber();
                    aComplexNumber end = new aComplexNumber();

                    TransformCoord(_startPoint.X, _startPoint.Y, ref start);
                    TransformCoord(endPoint.X, endPoint.Y, ref end);

                    double deltaX = end.Real - start.Real;
                    double deltaY = end.Imaginary - start.Imaginary;

                    _complexWindow.MinX -= deltaX;
                    _complexWindow.MaxX -= deltaX;
                    _complexWindow.MinY -= deltaY;
                    _complexWindow.MaxY -= deltaY;

                    using (Bitmap tempbitmap = (Bitmap)_canvas.Clone())
                    using (Graphics g = Graphics.FromImage(_canvas))
                    {
                        GraphicsUnit gu = GraphicsUnit.Pixel;
                        g.FillRectangle(new SolidBrush(Color.SeaGreen), _canvas.GetBounds(ref gu));
                        g.DrawImage(tempbitmap, (endPoint.X - _startPoint.X), (endPoint.Y - _startPoint.Y));
                    }

                    Render();
                }

            }
        }


        /**************************************************************************
        * Draws the current complex coordinates to the given graphics object
        ***************************************************************************/
        private void OverlayCoordinates(ref Graphics g)
        {
            string tlLabel = $"({_complexWindow.MinX:0.####}, {_complexWindow.MaxY:0.####})";
            string brLabel = $"({_complexWindow.MaxX:0.####}, {_complexWindow.MinY:0.####})";

            using (Brush brush = new SolidBrush(Color.White))
            using (Font font = new Font("Tahoma", 12f, FontStyle.Regular, GraphicsUnit.Pixel))
            {
                SizeF rightlabelsize = g.MeasureString(brLabel, font);

                g.DrawString(tlLabel, font, brush, 0, 0);
                g.DrawString(brLabel, font, brush,
                                    myPictureBox.Width - rightlabelsize.Width,
                                    myPictureBox.Height - rightlabelsize.Height);
            }
        }


        /**************************************************************************
        * Paint Handler 
        ***************************************************************************/
        private void PictureBox_Paint(object sender, PaintEventArgs e)
        {
            // Draws the bitmap canvas to the picturebox.
            e.Graphics.DrawImage(_canvas, 0, 0);

            // If we are rendering, show a progress line.
            // We can get the progress from the statusbar progress control.
            if (_isRendering)
            {
                int perc = toolStripProgressBar.Value;
                int Yline = (myPictureBox.Height * perc) / 100;
                if (Yline >= myPictureBox.Height)
                    Yline = myPictureBox.Height - 1;

                e.Graphics.DrawLine(new Pen(Color.OrangeRed), 0, Yline, myPictureBox.Width - 1, Yline);
            }

            if (_isZooming)
            {
                // Draw the selection rectangle...
                if (_ZoomWindow != null && _ZoomWindow.Width > 0 && _ZoomWindow.Height > 0)
                {
                    e.Graphics.DrawRectangle(_ZoomSelectionPen, _ZoomWindow);
                }
            }

            // Overlay the current Mandelbrot coordinates
            Graphics g = e.Graphics;
            OverlayCoordinates(ref g);
        }
    }

    // Stores the rectangular plane that defines our Mandelbrot
    // drawing plane.
    public class MandelbrotPlane
    {
        public MandelbrotPlane()
        {
            MinX = -2.4;
            MaxX = 1.0;
            MinY = -1.8;
            MaxY = 1.8;
            MaxThreshold = 50;
        }

        public double MinX { get; set; }
        public double MaxX { get; set; }
        public double MinY { get; set; }
        public double MaxY { get; set; }

        // max iterations to test for inclusion in Mandelbrot set
        public int MaxThreshold { get; set; }
    }

}
