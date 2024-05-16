using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NewPaintPlugIn;

namespace Shuffle
{
    [Version(1, 0)]
    public class Shuffle : IFilterPlugin
    {
        public string Name => "ShuffleImage";
        public string Author => "mishelevine";

        public void Transform(ref InkCanvas inkCanvas)
        {
            // Преобразование InkCanvas в Bitmap
            Bitmap bitmap = InkCanvasToBitmap(inkCanvas);

            // Разбиение изображения на 3x3 части
            List<Bitmap> imageParts = SplitImage(bitmap, 3, 3);

            // Перемешивание частей изображения
            imageParts = ShuffleImageParts(imageParts);

            // Создание нового Bitmap с перемешанными частями изображения
            Bitmap shuffledBitmap = CombineImageParts(imageParts, bitmap.Width, bitmap.Height);

            // Создание нового InkCanvas с перемешанными изображениями
            inkCanvas = BitmapToInkCanvas(shuffledBitmap, inkCanvas.ActualWidth, inkCanvas.ActualHeight);
        }
        private Bitmap InkCanvasToBitmap(InkCanvas inkCanvas)
        {
            // Определение размеров InkCanvas
            int width = (int)inkCanvas.ActualWidth;
            int height = (int)inkCanvas.ActualHeight;

            // Создание нового Bitmap с заданными размерами
            Bitmap bitmap = new Bitmap(width, height);

            // Создание Graphics для рисования на Bitmap
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                // Создание прозрачного фона
                graphics.Clear(System.Drawing.Color.Transparent);

                // Создание нового Visual для отрисовки InkCanvas
                var visual = new DrawingVisual();

                // Рендеринг InkCanvas на Visual
                var drawingContext = visual.RenderOpen();
                drawingContext.DrawRectangle(new System.Windows.Media.VisualBrush(inkCanvas), null, new System.Windows.Rect(0, 0, width, height));
                drawingContext.Close();

                // Рендеринг Visual на Graphics
                RenderTargetBitmap rtb = new RenderTargetBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                rtb.Render(visual);

                // Конвертация RenderTargetBitmap в Bitmap
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

        private List<Bitmap> SplitImage(Bitmap image, int rows, int cols)
        {
            List<Bitmap> imageParts = new List<Bitmap>();

            int partWidth = image.Width / cols;
            int partHeight = image.Height / rows;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    Rectangle rect = new Rectangle(x * partWidth, y * partHeight, partWidth, partHeight);
                    Bitmap part = image.Clone(rect, image.PixelFormat);
                    imageParts.Add(part);
                }
            }

            return imageParts;
        }

        private List<Bitmap> ShuffleImageParts(List<Bitmap> imageParts)
        {
            Random rng = new Random();
            int n = imageParts.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                Bitmap value = imageParts[k];
                imageParts[k] = imageParts[n];
                imageParts[n] = value;
            }
            return imageParts;
        }

        private Bitmap CombineImageParts(List<Bitmap> imageParts, int width, int height)
        {
            Bitmap combinedBitmap = new Bitmap(width, height);

            using (Graphics graphics = Graphics.FromImage(combinedBitmap))
            {
                int index = 0;
                foreach (Bitmap part in imageParts)
                {
                    int x = (index % 3) * (width / 3);
                    int y = (index / 3) * (height / 3);
                    graphics.DrawImage(part, x, y);
                    index++;
                }
            }

            return combinedBitmap;
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