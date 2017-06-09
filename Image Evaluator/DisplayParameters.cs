using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
using System.Windows.Threading;
using Microsoft.Win32;
using System.ComponentModel;
using System.Globalization;
using Dicom;
using Dicom.Imaging.Codec;
using Dicom.Media;

namespace CBCTQC
{
    class DisplayParameters
    {
        private DicomFile dicomFile;

        public ushort[] Image { get; set; }
        public byte[] BitmapImage { get; set; }
        public BitmapSource ImageSource;
        public int Width;
        public int Height;
        public bool IsImageDisplayed;
        public bool IsSignedImage;
        //// window level
        public double WindowWidth;
        public double WindowCentre;
        public double DisplayRatio;
        public Point DisplayCenterOnCanvas;

        public DisplayParameters()
        {
            IsImageDisplayed = false;
            IsSignedImage = true;
            DisplayRatio = 1.0;
        }

        public bool ReadDicomData(string fileName)
        {
            try
            {
                ImageInformation imageInfo = ((Views.MainWindow)System.Windows.Application.Current.MainWindow).ImageInfo;
                dicomFile = DicomFile.Open(fileName);
                IsSignedImage = true;
                Height = dicomFile.Dataset.Get<ushort>(DicomTag.Rows);
                Width = dicomFile.Dataset.Get<ushort>(DicomTag.Columns);

                WindowCentre = Convert.ToDouble(dicomFile.Dataset.Get<string>(DicomTag.WindowCenter));
                WindowWidth = Convert.ToDouble(dicomFile.Dataset.Get<string>(DicomTag.WindowWidth));

                imageInfo.Width = Width;
                imageInfo.Height = Height;
                imageInfo.pixelSize = dicomFile.Dataset.Get<double>(DicomTag.PixelSpacing);
                string seriesDescripton = dicomFile.Dataset.Get<string>(DicomTag.SeriesDescription);

                if (seriesDescripton.IndexOf("Standard") > 0)
                    imageInfo.filterType = "MSL";
                else if (seriesDescripton.IndexOf("Calcifications") > 0)
                    imageInfo.filterType = "RL";
                else
                {
                    imageInfo.filterType = "NA"; // not identified
                    MessageBox.Show("Cannot identify filter type.",
                        "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                if (seriesDescripton.IndexOf("cupping correction") < 0)
                    MessageBox.Show("Cupping correction was not used during reconstruction, measurement will not be accurate.",
                        "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);

                if (Image == null || Image.Length < Width * Height)
                {
                    Image = new ushort[Width * Height];
                }
                Image = dicomFile.Dataset.Get<ushort[]>(DicomTag.PixelData);

                if (IsSignedImage)
                {
                    //// Convert short to ushort
                    short[] tmp = new short[Width * Height];
                    Buffer.BlockCopy(Image, 0, tmp, 0, Width * Height * sizeof(ushort));
                    for (int i = 0; i < Width * Height; i++)
                    {
                        Image[i] = (ushort)(tmp[i] - short.MinValue);
                    }
                    tmp = null;
                }
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Cannot read image: {0}", fileName), ex);
            }
        }

        public void BuildBitmapImage(bool InvertImage)
        {
            if (BitmapImage == null || BitmapImage.Length < Width * Height)
            {
                BitmapImage = new byte[Width * Height];
            }

            int winMax = (int)(WindowCentre + WindowWidth / 2);
            if (IsSignedImage)
                winMax -= short.MinValue;
            int winMin = (int)(winMax - WindowWidth);

            if (InvertImage)
            {
                for (int i = 0; i < Width * Height; i++)
                {
                    if (Image[i] <= winMin)
                    {
                        BitmapImage[i] = 255;
                    }
                    else if (Image[i] >= winMax)
                    {
                        BitmapImage[i] = 0;
                    }
                    else
                    {
                        BitmapImage[i] = (byte)(255 - (Image[i] - winMin) * 255 / WindowWidth);
                    }
                }
            }
            else
            {
                for (int i = 0; i < Width * Height; i++)
                {
                    if (Image[i] <= winMin)
                    {
                        BitmapImage[i] = 0;
                    }
                    else if (Image[i] >= winMax)
                    {
                        BitmapImage[i] = 255;
                    }
                    else
                    {
                        BitmapImage[i] = (byte)((Image[i] - winMin) * 255 / WindowWidth);
                    }
                }
            }
            ImageSource = BitmapSource.Create(Width, Height, 96d, 96d, PixelFormats.Gray8, null, BitmapImage, Width);
            //// In order to write text, create a new bitmapSource with text
            var Visual = new DrawingVisual();
            using (DrawingContext drawingContext = Visual.RenderOpen())
            {
                drawingContext.DrawImage(ImageSource, new Rect(0, 0, Width, Height));
                ////Display window level
                FormattedText WindowLevel = new FormattedText("Window Level: " + "[ " + (WindowCentre - WindowWidth / 2).ToString() + ", " + (WindowCentre + WindowWidth / 2).ToString() + " ]", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        new Typeface("Arial"), 18, Brushes.White);
                drawingContext.DrawText(WindowLevel, new Point(Width / 2 - WindowLevel.Width / 2, Height - WindowLevel.Height - 18));
            }
            ImageSource = ToBitmapSource(new DrawingImage(Visual.Drawing));
        }

        private BitmapSource ToBitmapSource(DrawingImage source)
        {
            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            drawingContext.DrawImage(source, new Rect(new Point(0, 0), new Size(source.Width, source.Height)));
            drawingContext.Close();

            RenderTargetBitmap bmp = new RenderTargetBitmap((int)source.Width, (int)source.Height, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(drawingVisual);
            return bmp;
        }

        public void ResetWindowLevel()
        {
            WindowCentre = Convert.ToDouble(dicomFile.Dataset.Get<string>(DicomTag.WindowCenter));
            WindowWidth = Convert.ToDouble(dicomFile.Dataset.Get<string>(DicomTag.WindowWidth));
        }
    }
}
