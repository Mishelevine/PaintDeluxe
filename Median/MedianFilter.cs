using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PluginInterface;

namespace MedianFilter
{
    [Version(1, 0)]
    public class MedianFilter : IFilterPlugin
    {
        public string Name => "MedianFilter";
        public string Author => "mishelevine";

        public void Transform(ref InkCanvas inkCanvas)
        {
            Bitmap bitmap = InkCanvasToBitmap(inkCanvas);

            Bitmap filteredBitmap = ApplyMedianFilter(bitmap);

            inkCanvas = BitmapToInkCanvas(filteredBitmap, inkCanvas.ActualWidth, inkCanvas.ActualHeight);
        }

        private Bitmap InkCanvasToBitmap(InkCanvas inkCanvas)
        {
            int width = (int)inkCanvas.ActualWidth;
            int height = (int)inkCanvas.ActualHeight;

            Bitmap bitmap = new Bitmap(width, height);

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(System.Drawing.Color.Transparent);

                var visual = new DrawingVisual();
                var drawingContext = visual.RenderOpen();
                drawingContext.DrawRectangle(new System.Windows.Media.VisualBrush(inkCanvas), null, new System.Windows.Rect(0, 0, width, height));
                drawingContext.Close();

                RenderTargetBitmap rtb = new RenderTargetBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtb.Render(visual);

                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(rtb));

                using (var stream = new System.IO.MemoryStream())
                {
                    encoder.Save(stream);
                    stream.Seek(0, System.IO.SeekOrigin.Begin);
                    bitmap = (Bitmap)System.Drawing.Image.FromStream(stream);
                }
            }

            return bitmap;
        }

        private Bitmap ApplyMedianFilter(Bitmap image)
        {
            Bitmap result = new Bitmap(image.Width, image.Height);

            int kernelSize = 5;

            for (int y = kernelSize / 2; y < image.Height - kernelSize / 2; y++)
            {
                for (int x = kernelSize / 2; x < image.Width - kernelSize / 2; x++)
                {
                    List<int> neighborR = new List<int>();
                    List<int> neighborG = new List<int>();
                    List<int> neighborB = new List<int>();

                    for (int j = -kernelSize / 2; j <= kernelSize / 2; j++)
                    {
                        for (int i = -kernelSize / 2; i <= kernelSize / 2; i++)
                        {
                            System.Drawing.Color pixel = image.GetPixel(x + i, y + j);
                            neighborR.Add(pixel.R);
                            neighborG.Add(pixel.G);
                            neighborB.Add(pixel.B);
                        }
                    }

                    neighborR.Sort();
                    neighborG.Sort();
                    neighborB.Sort();

                    int medianR = neighborR[neighborR.Count / 2];
                    int medianG = neighborG[neighborG.Count / 2];
                    int medianB = neighborB[neighborB.Count / 2];

                    result.SetPixel(x, y, System.Drawing.Color.FromArgb(medianR, medianG, medianB));
                }
            }

            return result;
        }

        private InkCanvas BitmapToInkCanvas(Bitmap bitmap, double width, double height)
        {
            InkCanvas inkCanvas = new InkCanvas();

            System.Windows.Controls.Image image = new System.Windows.Controls.Image();
            image.Source = ToBitmapImage(bitmap);
            image.Width = width;
            image.Height = height;

            inkCanvas.Children.Add(image);

            return inkCanvas;
        }

        private BitmapImage ToBitmapImage(Bitmap bitmap)
        {
            using (System.IO.MemoryStream memory = new System.IO.MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;
                System.Windows.Media.Imaging.BitmapImage bitmapImage = new System.Windows.Media.Imaging.BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }
    }
}