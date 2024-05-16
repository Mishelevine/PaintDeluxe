using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PluginInterface;

namespace Prewitt
{
    [Version(1, 0)]
    public class PrewittFilter : IFilterPlugin
    {
        public string Name => "PrewittFilter";
        public string Author => "mishelevine";

        public void Transform(ref InkCanvas inkCanvas)
        {
            Bitmap bitmap = InkCanvasToBitmap(inkCanvas);

            Bitmap filteredBitmap = ApplyPrewittFilter(bitmap);

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

        private Bitmap ApplyPrewittFilter(Bitmap image)
        {
            Bitmap result = new Bitmap(image.Width, image.Height);

            int[,] horizontalFilter = { { -1, 0, 1 }, { -1, 0, 1 }, { -1, 0, 1 } };

            int[,] verticalFilter = { { -1, -1, -1 }, { 0, 0, 0 }, { 1, 1, 1 } };

            for (int y = 1; y < image.Height - 1; y++)
            {
                for (int x = 1; x < image.Width - 1; x++)
                {
                    int horizontalGradient = 0;
                    int verticalGradient = 0;

                    for (int j = -1; j <= 1; j++)
                    {
                        for (int i = -1; i <= 1; i++)
                        {
                            System.Drawing.Color pixel = image.GetPixel(x + i, y + j);
                            int gray = (pixel.R + pixel.G + pixel.B) / 3;
                            horizontalGradient += gray * horizontalFilter[j + 1, i + 1];
                            verticalGradient += gray * verticalFilter[j + 1, i + 1];
                        }
                    }

                    int totalGradient = (int)Math.Sqrt(Math.Pow(horizontalGradient, 2) + Math.Pow(verticalGradient, 2));
                    totalGradient = Math.Min(255, Math.Max(0, totalGradient));
                    result.SetPixel(x, y, System.Drawing.Color.FromArgb(totalGradient, totalGradient, totalGradient));
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