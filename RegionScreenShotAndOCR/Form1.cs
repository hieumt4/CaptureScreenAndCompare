using GI.Screenshot;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace RegionScreenShotAndOCR
{
    public partial class Form1 : Form
    {
        //set variable global
        Bitmap preBitMap = null;
        int width = 0;
        int height = 0;
        int stride = 0;
        int x = 0;
        int y = 0;
        public Form1()
        {
            InitializeComponent();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            btn_selectregion.Enabled = false;
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            if (!String.IsNullOrEmpty(txtSetTimeCapture.Text))
            {
                double timeLoop = Convert.ToDouble(txtSetTimeCapture.Text);
                _ = RunPeriodically(TimeSpan.FromMinutes(timeLoop), tokenSource.Token);
            }
            else
            {
                MessageBox.Show("Nhập thời gian định kỳ ! ");
                txtSetTimeCapture.Focus();
            }
        }

        /// <summary>
        /// Convert Image to bitmap
        /// </summary>
        /// <param name="bitmapSource"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="stride"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static Bitmap ConvertToBitmap(BitmapSource bitmapSource, int width, int height, int stride,int x, int y)
        {
            var memoryBlockPointer = Marshal.AllocHGlobal(height * stride);
            bitmapSource.CopyPixels(new Int32Rect(x, y, width, height), memoryBlockPointer, height * stride, stride);
            var bitmap = new Bitmap(width, height, stride, PixelFormat.Format32bppArgb, memoryBlockPointer);
            return bitmap;
        }


        /// <summary>
        /// RunPeriodically
        /// </summary>
        /// <param name="interval"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        async Task RunPeriodically( TimeSpan interval, CancellationToken token)
        {
            while (true)
            {
                this.WindowState = FormWindowState.Minimized;
                Thread.Sleep(500);
                captureScreenAndCompare();
                await Task.Delay(interval, token);
            }
        }

        /// <summary>
        /// Capture screen region and notice of difference
        /// </summary>
        public void captureScreenAndCompare()
        {
           
            if (width == 0 && height == 0 && stride == 0)
            {
                BitmapSource bitmapSourceRegion = Screenshot.CaptureRegion();
                width = bitmapSourceRegion.PixelWidth;
                height = bitmapSourceRegion.PixelHeight;
                x = ((CroppedBitmap)bitmapSourceRegion).SourceRect.X;
                y = ((CroppedBitmap)bitmapSourceRegion).SourceRect.Y;
                stride = width * ((bitmapSourceRegion.Format.BitsPerPixel + 7) / 8);
            }
            BitmapSource bitmapSource = Screenshot.CaptureAllScreens();
            Bitmap currentBitMap = ConvertToBitmap(bitmapSource, width, height, stride, x, y);

            pictureBox1.Image = (Image)currentBitMap;
            this.WindowState = FormWindowState.Normal;
            if (preBitMap != null)
            {
               Boolean isTheSame = CompareBitmapsFast(preBitMap, currentBitMap);
                if (!isTheSame)
                {
                    preBitMap = currentBitMap;
                    MessageBox.Show("Có sự khác biệt");
                }
            }
            else
            {
                preBitMap = currentBitMap;
            }

        }
        /// <summary>
        /// compare bitmap
        /// </summary>
        /// <param name="bmp1"></param>
        /// <param name="bmp2"></param>
        /// <returns></returns>
        public static bool CompareBitmapsFast(Bitmap bmp1, Bitmap bmp2)
        {
            if (bmp1 == null || bmp2 == null)
                return false;
            if (object.Equals(bmp1, bmp2))
                return true;
            if (!bmp1.Size.Equals(bmp2.Size) || !bmp1.PixelFormat.Equals(bmp2.PixelFormat))
                return false;

            int bytes = bmp1.Width * bmp1.Height * (Image.GetPixelFormatSize(bmp1.PixelFormat) / 8);

            bool result = true;
            byte[] b1bytes = new byte[bytes];
            byte[] b2bytes = new byte[bytes];

            BitmapData bitmapData1 = bmp1.LockBits(new Rectangle(0, 0, bmp1.Width, bmp1.Height), ImageLockMode.ReadOnly, bmp1.PixelFormat);
            BitmapData bitmapData2 = bmp2.LockBits(new Rectangle(0, 0, bmp2.Width, bmp2.Height), ImageLockMode.ReadOnly, bmp2.PixelFormat);

            Marshal.Copy(bitmapData1.Scan0, b1bytes, 0, bytes);
            Marshal.Copy(bitmapData2.Scan0, b2bytes, 0, bytes);

            for (int n = 0; n <= bytes - 1; n++)
            {
                if (b1bytes[n] != b2bytes[n])
                {
                    result = false;
                    break;
                }
            }

            bmp1.UnlockBits(bitmapData1);
            bmp2.UnlockBits(bitmapData2);

            return result;
        }
        /// <summary>
        /// event key press: not press character
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtSetTimeCapture_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.'))
            {
                e.Handled = true;
            }
            if ((e.KeyChar == '.') && ((sender as TextBox).Text.IndexOf('.') > -1))
            {
                e.Handled = true;
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
