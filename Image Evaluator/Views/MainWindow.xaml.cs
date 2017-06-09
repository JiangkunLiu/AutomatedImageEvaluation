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

namespace CBCTQC.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ImageInformation ImageInfo;
        private DisplayParameters DisplayControl;
        //// image position and scale
        private Point OldMousePosition;
        private bool IsLeftMouseButtonDown;
        //// Size of the image containing sub grid        
        private double OldCanvasWidth;
        private double OldCanvasHeight;      
        private readonly BackgroundWorker backgroundWorker;
        MeasurementResult MeasurementResults;
        DetectionResult DetectionResults;      

        public MainWindow()
        {
                InitializeComponent();
                ImageInfo = new ImageInformation();
                DisplayControl = new DisplayParameters();
                IsLeftMouseButtonDown = false;
                ButtonImageManipulation.IsChecked = false;
                DetectionResults = new DetectionResult();
                MeasurementResults = new MeasurementResult();
                DataGridMeasurementResult.ItemsSource = MeasurementResults.Result;
                DataGridDetectionResult.ItemsSource = DetectionResults.Result;
                backgroundWorker = new BackgroundWorker()
                {
                    WorkerSupportsCancellation = true,
                    WorkerReportsProgress = true
                };
                backgroundWorker.DoWork += new DoWorkEventHandler(backgroundWorker_DoWork);
                backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker_RunWorkerCompleted);
                backgroundWorker.ProgressChanged += new ProgressChangedEventHandler(backgroundWorker_ProgressChanged);
        }

        private void ResetImageViewer()
        {
            DisplayControl.DisplayCenterOnCanvas.X = Canvas.ActualWidth / 2;
            DisplayControl.DisplayCenterOnCanvas.Y = Canvas.ActualHeight / 2;
            DisplayControl.DisplayRatio = Math.Min(Canvas.ActualHeight / DisplayControl.Height, Canvas.ActualWidth / DisplayControl.Width);
            DisplayControl.ResetWindowLevel();
            //// DisplayControl.Width and Height remain the same
            DisplayImage();
        }

        private void ButtonSelectImage_Click(object sender, RoutedEventArgs e)
        {
            ///////// Select image with a open file dialog
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "All DICOM Files(*.dcm)|*.dcm";
            ofd.Title = "Select Dicom Image";
            ofd.InitialDirectory = @"D:\PhantomAcceptance\115_Phantom\E2398\4_49_50_8\1.2.276.0.45.44.2.41.3.160600737534.20160926.154948001";

            //System.Windows.MessageBox.Show(short.MaxValue.ToString());

            if (ofd.ShowDialog() == true)
            {
                ImageInfo.inputPath = Directory.GetParent(ofd.FileName).ToString();
                ImageInfo.fileNames = Directory.GetFiles(ImageInfo.inputPath, "*.dcm", SearchOption.TopDirectoryOnly);
                ImageInfo.ImageNumber = ImageInfo.fileNames.Length;
                if (ImageInfo.ImageNumber < 1)
                {
                    MessageBox.Show("No DICOM file found, please check folder path.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    // Upon path selected, display an image, and get image info: pixel size, dimension, etc.

                    ImageInfo.IsImageFound = true;
                    DisplayControl.ReadDicomData(ImageInfo.fileNames[ImageInfo.ImageNumber * 7 / 8]);

                    MeasurementResults.SetAllNotMeasured();
                    DataGridMeasurementResult.ItemsSource = null;
                    DataGridMeasurementResult.ItemsSource = MeasurementResults.Result;
                    DetectionResults.SetAllNull();
                    DataGridDetectionResult.ItemsSource = null;
                    DataGridDetectionResult.ItemsSource = DetectionResults.Result;
                    ImageInfo.resetParameter();

                    // Calculate displaycenteroncanvas and display ratio before calling displayimage
                    DisplayControl.DisplayCenterOnCanvas.X = Canvas.ActualWidth / 2;
                    DisplayControl.DisplayCenterOnCanvas.Y = Canvas.ActualHeight / 2;
                    DisplayControl.DisplayRatio = Math.Min(Canvas.ActualHeight / ImageInfo.Height, Canvas.ActualWidth / ImageInfo.Width);
                    DisplayImage();

                    ButtonImageManipulation.IsChecked = false;
                }
            }
        }

        private void DisplayImage()
        {
            DisplayControl.BuildBitmapImage(false);
            ImagePanel.Source = DisplayControl.ImageSource;
            DisplayControl.IsImageDisplayed = true;          
            OldCanvasWidth = Canvas.ActualWidth;
            OldCanvasHeight = Canvas.ActualHeight;
            SetImagePositionAndScale();
            ////Display window level
            int winMax = Convert.ToInt32(DisplayControl.WindowCentre + 0.5 * DisplayControl.WindowWidth);
            int winMin = Convert.ToInt32(winMax - DisplayControl.WindowWidth);
            WindowLevel.Content = "[ " + winMin.ToString() + ", " + winMax.ToString() + " ]";
        }

        private void SetImagePositionAndScale()
        {
            //// ImagePanel's and Cavnas' ActualHeight and ActualWidth are not set until the control is measured and arranged,
            //// It gives 0's when first time checked, so use imageSource to get the image size instead of imagePanel.ActualWidth
            //// LayoutTransform does not change ImagePanel.Height and Width        
            // If the image is put inside a Grid, its position is solely controlled by the Margin property
            // In it is in a canvas, the position can be controlled by canvas.setleft and settop
            // Grid automatically resize image in some direction, canvas does not.
            ImagePanel.LayoutTransform = new ScaleTransform(DisplayControl.DisplayRatio, DisplayControl.DisplayRatio);
            Canvas.SetLeft(ImagePanel, DisplayControl.DisplayCenterOnCanvas.X - DisplayControl.Width * DisplayControl.DisplayRatio / 2);
            Canvas.SetTop(ImagePanel, DisplayControl.DisplayCenterOnCanvas.Y - DisplayControl.Height * DisplayControl.DisplayRatio / 2);
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!DisplayControl.IsImageDisplayed)
                return;
            if (backgroundWorker.IsBusy)
                return;

            //// Display pixel position and value
            Point CursorPosition = e.GetPosition(ImagePanel);
            int Pixel_X = (int)CursorPosition.X;
            int Pixel_Y = (int)CursorPosition.Y;
            if (Pixel_X < DisplayControl.Width && Pixel_Y < DisplayControl.Height && Pixel_X >= 0 && Pixel_Y >= 0)
            {
                this.PixelPosition.Content = "( " + Pixel_X.ToString() + ", " + Pixel_Y.ToString() + " )";
                int index = Pixel_X + Pixel_Y * DisplayControl.Width;
                this.CTNumber.Content = DisplayControl.Image[Pixel_X + Pixel_Y * DisplayControl.Width] + short.MinValue;
            }

            if (ButtonImageManipulation.IsChecked == false)
                return;

            Point newMousePosition = e.GetPosition(Canvas);
            double diffX = newMousePosition.X - OldMousePosition.X;
            double diffY = newMousePosition.Y - OldMousePosition.Y;
            //// Adjust window level
            if (e.RightButton == MouseButtonState.Pressed)
            {

                int widthValueChangeRate = 2; // mouse sensitivity
                int centerValueChangeRate = 2;
                int deltaX = (int)(diffX * widthValueChangeRate);
                int deltaY = (int)(diffY * centerValueChangeRate);
                DisplayControl.WindowCentre -= deltaY;
                DisplayControl.WindowWidth -= deltaX;
                if (DisplayControl.WindowWidth < 0)
                    DisplayControl.WindowWidth = 0;
                DisplayImage();              
            }
            //// Pan image
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                //// Doubl clicking file in OpenFile window (or the window top to maximize or normalize window size) triggers mouse move event, without trigging mouse down event
                //// therefore, the image is moved unexpectedly
                if (!IsLeftMouseButtonDown)
                    return;

                double CanvasCenterChangeRate = 1; // mouse sensitivity
                DisplayControl.DisplayCenterOnCanvas.X += (diffX / CanvasCenterChangeRate);
                DisplayControl.DisplayCenterOnCanvas.Y += (diffY / CanvasCenterChangeRate);
                //// Limit the position of display center
                if (DisplayControl.DisplayCenterOnCanvas.X < -DisplayControl.Width * DisplayControl.DisplayRatio / 2 * 0.9)
                    DisplayControl.DisplayCenterOnCanvas.X = -DisplayControl.Width * DisplayControl.DisplayRatio / 2 * 0.9;
                if (DisplayControl.DisplayCenterOnCanvas.X > Canvas.ActualWidth + DisplayControl.Width * DisplayControl.DisplayRatio / 2 * 0.9)
                    DisplayControl.DisplayCenterOnCanvas.X = Canvas.ActualWidth + DisplayControl.Width * DisplayControl.DisplayRatio / 2 * 0.9;
                if (DisplayControl.DisplayCenterOnCanvas.Y < -DisplayControl.Height * DisplayControl.DisplayRatio / 2 * 0.9)
                    DisplayControl.DisplayCenterOnCanvas.Y = -DisplayControl.Height * DisplayControl.DisplayRatio / 2 * 0.9;
                if (DisplayControl.DisplayCenterOnCanvas.Y > Canvas.ActualHeight + DisplayControl.Height * DisplayControl.DisplayRatio / 2 * 0.9)
                    DisplayControl.DisplayCenterOnCanvas.Y = Canvas.ActualHeight + DisplayControl.Height * DisplayControl.DisplayRatio / 2 * 0.9;
                SetImagePositionAndScale();
            }
            
            OldMousePosition = newMousePosition;
        }

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!DisplayControl.IsImageDisplayed)
                return;
            //// Capture canvas size change when window is maximized, normalized, dragged
            //// And automatically scale, move image inside
            if (Canvas.ActualHeight < 8 || Canvas.ActualWidth < 8)
                return;

            DisplayControl.DisplayCenterOnCanvas.X *= (Canvas.ActualWidth / OldCanvasWidth);
            DisplayControl.DisplayCenterOnCanvas.Y *= (Canvas.ActualHeight / OldCanvasHeight);
            //// Check if image width height ratio 
            double CanvasWidthHeightRatio = Canvas.ActualWidth / Canvas.ActualHeight;
            double ImageWidthHeightRatio = ImagePanel.ActualWidth / ImagePanel.ActualHeight;
            double OldCanvasWidthHeightRatio = OldCanvasWidth / OldCanvasHeight;
            ////Switching display ratio calculation methods according to width to height ratio
            if (CanvasWidthHeightRatio > ImageWidthHeightRatio)
            {
                //// If a different dimension was used for last calculation
                //// Corrent the old canvas height by using the height when canvaswidthheightratio = imagewidthheightratio
                if (OldCanvasWidthHeightRatio <= ImageWidthHeightRatio)
                {
                    OldCanvasHeight = OldCanvasWidth * ImagePanel.ActualHeight / ImagePanel.ActualWidth;
                }
                DisplayControl.DisplayRatio *= (Canvas.ActualHeight / OldCanvasHeight);
            }
            else
            {
                if (OldCanvasWidthHeightRatio > ImageWidthHeightRatio)
                {
                    OldCanvasWidth = OldCanvasHeight * ImagePanel.ActualWidth / ImagePanel.ActualHeight;
                }
                DisplayControl.DisplayRatio *= (Canvas.ActualWidth / OldCanvasWidth);
            }

            if (DisplayControl.IsImageDisplayed)
            {
                SetImagePositionAndScale();
            }


            OldCanvasWidth = Canvas.ActualWidth;
            OldCanvasHeight = Canvas.ActualHeight;
        }

        private void Canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DisplayControl.IsImageDisplayed && ButtonImageManipulation.IsChecked == true)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    OldMousePosition = e.GetPosition(Canvas);
                }

                Canvas.Cursor = System.Windows.Input.Cursors.Hand;
            }
        }

        private void Canvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            Canvas.Cursor = System.Windows.Input.Cursors.Arrow;
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DisplayControl.IsImageDisplayed && ButtonImageManipulation.IsChecked == true)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    IsLeftMouseButtonDown = true;
                    OldMousePosition = e.GetPosition(Canvas);
                }

                Canvas.Cursor = System.Windows.Input.Cursors.SizeAll;
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Canvas.Cursor = System.Windows.Input.Cursors.Arrow;
            IsLeftMouseButtonDown = false;
        }

        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!DisplayControl.IsImageDisplayed)
                return;
            if (ButtonImageManipulation.IsChecked == false)
                return;
            double RatioChangeRate = 888;
            int diff = e.Delta;
            DisplayControl.DisplayRatio += diff / RatioChangeRate;
            if (DisplayControl.DisplayRatio < 0.1)
                DisplayControl.DisplayRatio = 0.1;
            if (DisplayControl.DisplayRatio > 10.0)
                DisplayControl.DisplayRatio = 10.0;

            //// If displayCenter is outside the canvas, zooming out may cause image panel to be completele outside canvas
            //// To prevent this, update display center if neccessary while zooming.
            if (DisplayControl.DisplayCenterOnCanvas.X < -DisplayControl.Width * DisplayControl.DisplayRatio / 2 * 0.9)
                DisplayControl.DisplayCenterOnCanvas.X = -DisplayControl.Width * DisplayControl.DisplayRatio / 2 * 0.9;
            if (DisplayControl.DisplayCenterOnCanvas.X > Canvas.ActualWidth + DisplayControl.Width * DisplayControl.DisplayRatio / 2 * 0.9)
                DisplayControl.DisplayCenterOnCanvas.X = Canvas.ActualWidth + DisplayControl.Width * DisplayControl.DisplayRatio / 2 * 0.9;
            if (DisplayControl.DisplayCenterOnCanvas.Y < -DisplayControl.Height * DisplayControl.DisplayRatio / 2 * 0.9)
                DisplayControl.DisplayCenterOnCanvas.Y = -DisplayControl.Height * DisplayControl.DisplayRatio / 2 * 0.9;
            if (DisplayControl.DisplayCenterOnCanvas.Y > Canvas.ActualHeight + DisplayControl.Height * DisplayControl.DisplayRatio / 2 * 0.9)
                DisplayControl.DisplayCenterOnCanvas.Y = Canvas.ActualHeight + DisplayControl.Height * DisplayControl.DisplayRatio / 2 * 0.9;
            SetImagePositionAndScale();
        }

        private void ButtonMeasure_Click(object sender, RoutedEventArgs e)
        {

            if (ImageInfo.IsImageFound == false)
            {
                MessageBox.Show("Please select image first.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // handle imcompletely reconstructed volume 
            if (ImageInfo.ImageNumber - ImageInfo.phStart < 50.0 / ImageInfo.pixelSize)
            {
                MessageBox.Show("Reconstructoin volume seems not complete, measurement is not performed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (backgroundWorker.IsBusy != true)
            {
                // create a folder to save pictures
                ImageInfo.measurementTime = DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss tt");
                ImageInfo.outputPath = AppDomain.CurrentDomain.BaseDirectory + @"\QCFiles\" + ImageInfo.measurementTime;
                DirectoryInfo di = Directory.CreateDirectory(ImageInfo.outputPath);

                MeasurementResults.SetAllMeasuring();
                DataGridMeasurementResult.ItemsSource = null;
                DataGridMeasurementResult.ItemsSource = MeasurementResults.Result;

                DetectionResults.SetAllNull();
                DataGridDetectionResult.ItemsSource = null;
                DataGridDetectionResult.ItemsSource = DetectionResults.Result;

                measurementProgressBar.Minimum = 0;
                //// Other calculation is worth 60 of progress
                measurementProgressBar.Maximum = ImageInfo.ImageNumber + 60;
                measurementProgressBar.Value = 0;

                ImageInfo.resetParameter();

                ButtonImageManipulation.IsChecked = false;
                // Start the asynchronous operation.
                backgroundWorker.RunWorkerAsync();
            }
            else
            {
                MessageBox.Show("Measurement is already in progress.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            int i = 0;
            short[] img = new short[ImageInfo.Height * ImageInfo.Width];
            short[] img2 = new short[ImageInfo.Width * ImageInfo.ImageNumber];
            while (!ImageInfo.phFlag && i < ImageInfo.ImageNumber)
            {
                Console.WriteLine("\n" + "Checking: " + Path.GetFileName(ImageInfo.fileNames[i]));
                ImgEvaluation.readDicomData(ImageInfo.fileNames[i], img, ImageInfo.Height * ImageInfo.Width);
                ImgEvaluation.findObject(img, ImageInfo);
                worker.ReportProgress(i + 1);

                if (ImageInfo.phFlag)
                    ImageInfo.phStart = i;
                else
                    i += (int)((1.6) / ImageInfo.pixelSize);

                // handle cancellation request
                // cancel work if image found not to be phantom or cancel button pressed
                if (!ImageInfo.isPhantom)
                    backgroundWorker.CancelAsync();
                if (worker.CancellationPending == true)
                {
                    e.Cancel = true;
                    return;
                }
            }

            while (!ImageInfo.hcFlag && i < ImageInfo.ImageNumber)
            {
                Console.WriteLine("\n" + "Checking: " + Path.GetFileName(ImageInfo.fileNames[i]));
                ImgEvaluation.readDicomData(ImageInfo.fileNames[i], img, ImageInfo.Height * ImageInfo.Width);
                ImgEvaluation.findObject(img, ImageInfo);

                // update worker.ProgressPercentage. It's used as slice number here, not percentage
                worker.ReportProgress(i + 1);
                if (ImageInfo.hcFlag)
                    ImageInfo.hcStart = i;
                else
                    i += 3;

                // handle cancellation request
                // cancel work if image found not to be phantom or cancel button pressed
                if (!ImageInfo.isPhantom)
                    backgroundWorker.CancelAsync();
                if (worker.CancellationPending == true)
                {
                    e.Cancel = true;
                    return;
                }
            }

            while (!ImageInfo.clFlag && i < ImageInfo.ImageNumber)
            {
                Console.WriteLine("\n" + "Checking: " + Path.GetFileName(ImageInfo.fileNames[i]));
                ImgEvaluation.readDicomData(ImageInfo.fileNames[i], img, ImageInfo.Height * ImageInfo.Width);
                ImgEvaluation.findCalcification(img, ImageInfo);

                // update worker.ProgressPercentage. It's used as slice number here, not percentage
                worker.ReportProgress(i + 1);

                if (ImageInfo.clFlag)
                    ImageInfo.clIndex = i;
                else
                    i++;

                // handle cancellation request
                // cancel work if image found not to be phantom or cancel button pressed
                if (!ImageInfo.isPhantom)
                    backgroundWorker.CancelAsync();
                if (worker.CancellationPending == true)
                {
                    e.Cancel = true;
                    return;
                }
            }

            i = ImageInfo.ImageNumber;
            worker.ReportProgress(i);
            
            if (!ImageInfo.clFlag)
            {
                Console.WriteLine("No calcification detected.");
            }
            else
            {
                Console.WriteLine("\nCalcifications are on: " + Path.GetFileName(ImageInfo.fileNames[ImageInfo.clIndex]));
                ImgEvaluation.maxShortImg(ImageInfo.fileNames, ImageInfo, img);
                ImgEvaluation.writeShortData(ImageInfo.outputPath + "\\CalcificationImg_" + ImageInfo.Width + "x" + ImageInfo.Height + ".img", img, 0, ImageInfo.Height * ImageInfo.Width);

                // handle cancellation request
                // cancel work if image found not to be phantom or cancel button pressed
                if (!ImageInfo.isPhantom)
                    backgroundWorker.CancelAsync();
                if (worker.CancellationPending == true)
                {
                    e.Cancel = true;
                    return;
                }

                i += 20;
                worker.ReportProgress(i);
            }

            if (!ImageInfo.hcFlag)
            {
                Console.WriteLine("No contrast structure detected.");
                return;
            }
            else
            {

                Console.WriteLine("Phantom starts on: " + Path.GetFileName(ImageInfo.fileNames[ImageInfo.phStart]) + "\nContrast structures start on: " + Path.GetFileName(ImageInfo.fileNames[ImageInfo.hcStart]));
                //ImgEvaluation.avgShortNoiseImg(fileNames, ImageInfo, img);
                //ImgEvaluation.writeShortData(ImageInfo.outputPath + "\\noiseImg_" + ImageInfo.Width + "x" + ImageInfo.Height + ".img", img, 0, ImageInfo.Height * ImageInfo.Width);
                // handle cancellation request
                // cancel work if image found not to be phantom or cancel button pressed
                if (!ImageInfo.isPhantom)
                    backgroundWorker.CancelAsync();
                if (worker.CancellationPending == true)
                {
                    e.Cancel = true;
                    return;
                }
                ImgEvaluation.avgShortContrastImg(ImageInfo.fileNames, ImageInfo, img);
                ImgEvaluation.writeShortData(ImageInfo.outputPath + "\\ContrastImg_" + ImageInfo.Width + "x" + ImageInfo.Height + ".img", img, 0, ImageInfo.Height * ImageInfo.Width);
                i += 20;
                worker.ReportProgress(i);

                ImgEvaluation.measureImgQuality(ImageInfo, ImageInfo.fileNames);
                // handle cancellation request
                // cancel work if image found not to be phantom or cancel button pressed
                if (!ImageInfo.isPhantom)
                    backgroundWorker.CancelAsync();
                if (worker.CancellationPending == true)
                {
                    e.Cancel = true;
                    return;
                }

                i += 20;
                worker.ReportProgress(i);
            }

            img = null;
            img2 = null;
            Console.WriteLine("\nApplication is finished.");
            
        }

        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //// UI control and background worker are different threads, in background worker, UI control is not allowed
            //// But it is allowed here, it belongs to the UI control thread
            measurementProgressBar.Value = e.ProgressPercentage;
            if (ImageInfo.phFlag)
                DetectionResults.SetDetected("Phantom");
            if (ImageInfo.hcFlag)
                DetectionResults.SetDetected("Tumor");
            if (ImageInfo.wtFlag)
                DetectionResults.SetDetected("Water");
            if (ImageInfo.clFlag)
                DetectionResults.SetDetected("Calcification");

            DataGridDetectionResult.ItemsSource = null;
            DataGridDetectionResult.ItemsSource = DetectionResults.Result;
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {   // This event is necessary because int he doword event, many things cannot be done, such as button enabling
            // because they must be performed by a different thread.

            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message);
            }
            else if (e.Cancelled)
            {
                // Next, handle the case where the user canceled 
                // the operation.
                // Note that due to a race condition in 
                // the DoWork event handler, the Cancelled
                // flag may not have been set, even though
                // CancelAsync was called.
                measurementProgressBar.Value = 0;

                MeasurementResults.SetAllCancelled();
                DataGridMeasurementResult.ItemsSource = null;
                DataGridMeasurementResult.ItemsSource = MeasurementResults.Result;

                DetectionResults.SetAllNotDetected();
                DataGridDetectionResult.ItemsSource = null;
                DataGridDetectionResult.ItemsSource = DetectionResults.Result;

                string log = "*******************************\r\n";
                log += ImageInfo.measurementTime;
                log += "\r\nMeasurement Cancelled.";
                string path = AppDomain.CurrentDomain.BaseDirectory + "\\QCLog.koning";
                using (StreamWriter sw = File.AppendText(path))
                {
                    sw.WriteLine(log);
                    sw.WriteLine("\n");
                }

            }
            else
            {
                string log = "*******************************\r\n";
                log += ImageInfo.measurementTime;
                log += "\r\nImage Directory: \r\n" + ImageInfo.inputPath + "\r\n";
                
                // output results
                if (ImageInfo.phFlag)
                {
                    log += ("Phantom detected, starting on " + Path.GetFileName(ImageInfo.fileNames[ImageInfo.phStart]) + "\r\n");
                    DetectionResults.SetDetected("Phantom");
                }
                else
                {
                    log += "Phantom not detected\r\n";
                    DetectionResults.SetNotDetected("Phantom");

                }
                if (ImageInfo.hcFlag)
                {
                    log += ("Contrast structures detected, starting on " + Path.GetFileName(ImageInfo.fileNames[ImageInfo.hcStart]) + "\r\n");
                    DetectionResults.SetDetected("Tumor");
                }
                else
                {
                    log += "Contrast structures not detected\r\n";
                    DetectionResults.SetNotDetected("Tumor");
                }
                if (ImageInfo.wtFlag)
                {
                    log += ("Water detected\r\n");
                    DetectionResults.SetDetected("Water");
                }
                else
                {
                    log += "Phantom not detected\r\n";
                    DetectionResults.SetNotDetected("Water");
                }
                if (ImageInfo.clFlag)
                {
                    log += ("Calcifications detected, on " + Path.GetFileName(ImageInfo.fileNames[ImageInfo.clIndex]) + "\r\n");
                    DetectionResults.SetDetected("Calcification");
                }
                else
                {
                    log += "Calcifications not detected";
                    DetectionResults.SetNotDetected("Calcification");
                }
                

                DataGridDetectionResult.ItemsSource = null;
                DataGridDetectionResult.ItemsSource = DetectionResults.Result;

                // only show measurements if high contrast is detected
                if (ImageInfo.hcFlag)
                {
                    string measurementResult = "";
                    measurementResult += string.Format("\r\nCT Number Accuracy:\r\n     Tumor: {0:F2}\r\n     Water: {1:F2}\r\nField Uniformity: {2:F2}", ImageInfo.hcCTValue, ImageInfo.wtCTValue, ImageInfo.imgUniformity);
                    measurementResult += string.Format("\r\nNoise: {0:F2}\r\nContrast-to-Noise Ratio: {1:F2}", ImageInfo.imgNoise, ImageInfo.lcContrast / ImageInfo.imgNoise);
                    log += measurementResult;

                    MeasurementResults.SetValue("Image Noise:", string.Format("{0:F2}", ImageInfo.imgNoise));
                    MeasurementResults.SetValue("Tumor CT Number:", string.Format("{0:F2}", ImageInfo.hcCTValue));
                    MeasurementResults.SetValue("Water CT Number:", string.Format("{0:F2}", ImageInfo.wtCTValue));
                    MeasurementResults.SetValue("Field Uniformity:", string.Format("{0:F2}", ImageInfo.imgUniformity));
                    MeasurementResults.SetValue("Contrast to Noise Ratio:", string.Format("{0:F2}", ImageInfo.lcContrast / ImageInfo.imgNoise));

                    //// Display the contrastlabel image upon measurement finishing
                    short[] img = new short[ImageInfo.Height * ImageInfo.Width];
                    if (ImgEvaluation.readShortData(ImageInfo.outputPath + "\\ContrastLabel_" + ImageInfo.Width + "x" + ImageInfo.Height + ".img", img, 0, ImageInfo.Height * ImageInfo.Width))
                    {
                        DisplayShortImage(img, ImageInfo.Height, ImageInfo.Width);
                    }
                    img = null;
                    
                    DisplayImage();
                }
                else
                {
                    log += "\r\nMeasurements not performed as high contrast structure not detected.";
                    MeasurementResults.SetAllFailed();
                }

                DataGridMeasurementResult.ItemsSource = null;
                DataGridMeasurementResult.ItemsSource = MeasurementResults.Result;

                string path = AppDomain.CurrentDomain.BaseDirectory + "\\QCLog.koning";

                using (StreamWriter sw = File.AppendText(path))
                {
                    sw.WriteLine(log);
                    sw.WriteLine("\n");
                }
            }
            measurementProgressBar.Value = 0;
        }

        private void ButtonCancelMeasurement_Click(object sender, RoutedEventArgs e)
        {
            if (backgroundWorker.WorkerSupportsCancellation == true)
            {
                // Cancel the asynchronous operation.
                backgroundWorker.CancelAsync();
            }
            if (backgroundWorker.IsBusy != true)
            {
                // MessageBox.Show("No measurement is in progress.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ButtonImageMamipulation_Click(object sender, RoutedEventArgs e)
        {
            if (backgroundWorker.IsBusy)
            {
                ButtonImageManipulation.IsChecked = false;
                return;
            }
            if (!DisplayControl.IsImageDisplayed)
            {
                ButtonImageManipulation.IsChecked = false;
                return;
            }
            if (ButtonImageManipulation.IsChecked == false)
            {
                ResetImageViewer();           
            }
        }

        private void ButtonShowCoronalNoiseImage_Click(object sender, RoutedEventArgs e)
        {
            if (!ImageInfo.hcFlag)
            {
                //MessageBox.Show("Cannot show image as tumor is not detected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            short[] img = new short[ImageInfo.Height * ImageInfo.Width];
            if (ImgEvaluation.readShortData(ImageInfo.outputPath + "\\NoiseLabel_Coronal_" + ImageInfo.Width + "x" + ImageInfo.Height + ".img", img, 0, ImageInfo.Height * ImageInfo.Width))
            {
                DisplayShortImage(img, ImageInfo.Height, ImageInfo.Width);
            }
            img = null;
        }

        private void ButtonShowContrastImage_Click(object sender, RoutedEventArgs e)
        {
            if (!ImageInfo.hcFlag)
            {
                //MessageBox.Show("Cannot show image as tumor is not detected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            short[] img = new short[ImageInfo.Height * ImageInfo.Width];
            if (ImgEvaluation.readShortData(ImageInfo.outputPath + "\\ContrastLabel_" + ImageInfo.Width + "x" + ImageInfo.Height + ".img", img, 0, ImageInfo.Height * ImageInfo.Width))
            {
                DisplayShortImage(img, ImageInfo.Height, ImageInfo.Width);
            }
            img = null;
        }

        private void ButtonShowCalcificationImage_Click(object sender, RoutedEventArgs e)
        {
            if (!ImageInfo.hcFlag)
            {
                //MessageBox.Show("Cannot show image as tumor is not detected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            short[] img = new short[ImageInfo.Height * ImageInfo.Width];
            if (ImgEvaluation.readShortData(ImageInfo.outputPath + "\\CalcificationImg_" + ImageInfo.Width + "x" + ImageInfo.Height + ".img", img, 0, ImageInfo.Height * ImageInfo.Width))
            {
                DisplayShortImage(img, ImageInfo.Height, ImageInfo.Width);
            }
            img = null;
        }

        private void ButtonShowSagittalNoiseImage_Click(object sender, RoutedEventArgs e)
        {
            if (!ImageInfo.hcFlag)
            {
                //MessageBox.Show("Cannot show image as tumor is not detected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            short[] img = new short[ImageInfo.Height * ImageInfo.Width];
            if (ImgEvaluation.readShortData(ImageInfo.outputPath + "\\NoiseLabel_Sagittal_" + ImageInfo.Width + "x" + ImageInfo.ImageNumber + ".img", img, 0, ImageInfo.ImageNumber * ImageInfo.Width))
            {
                DisplayShortImage(img, ImageInfo.ImageNumber, ImageInfo.Width);
            }
            img = null;
        }

        private void ButtonSaveImage_Click(object sender, RoutedEventArgs e)
        {
            if (!DisplayControl.IsImageDisplayed)
                return;

            string fileName;
            SaveFileDialog SFD = new SaveFileDialog();
            SFD.Title = "Select Where to Save";
            SFD.Filter = "All Files(*.png)|*.*";
            //OFD.InitialDirectory = ImageInfo.outputPath;
            SFD.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory + @"QCFiles\" + ImageInfo.measurementTime;

            if (SFD.ShowDialog() == true)
            {
                fileName = SFD.FileName;

                // A list needes to be cleared before given new values
                // otherwise, previous data may stay in the list
                using (var fileStream = new FileStream(fileName, FileMode.Create))
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(DisplayControl.ImageSource));
                    encoder.Save(fileStream);
                }
            }
        }

        private void DisplayShortImage(short[] Img, int Height, int Width)
        {
            DisplayControl.IsSignedImage = true;
            DisplayControl.Width = Width;
            DisplayControl.Height = Height;
            for (int i = 0; i < Width * Height; i++)
            {
                DisplayControl.Image[i] = (ushort)(Img[i] - short.MinValue);
            }
            ResetImageViewer();
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
