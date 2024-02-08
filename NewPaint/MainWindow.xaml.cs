using Fluent;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NewPaint
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private double currentScale = 1.0;
        private bool isDrawing = false;

        private int filesCreated = 0;
        private List<int> filesSavedIdexes = new List<int>();
        private List<string> filesSavedPaths = new List<string>();

        private GalleryItem selectedItem { get; set; }
        private string drawingOption = "";
        private Point startPoint;
        private Point endPoint;
        private Line line;
        private Ellipse ellipse;
        private Polyline star = new Polyline();
        bool isDelete = false;
        private int vertex = 5;

        public MainWindow()
        {
            this.InitializeComponent();
        }

        #region Добавление и закрытие окон
        private void AddTabButton_Click(object sender, RoutedEventArgs e = null)
        {
            filesCreated++;

            TabItem tab = new TabItem();
            tab.Header = "tab" + filesCreated.ToString();
            InkCanvas canvas = new InkCanvas();
            canvas.Name = "canvas" + filesCreated.ToString();
            tab.Header = "Tab" + filesCreated.ToString();
            canvas.MouseLeftButtonDown += InkCanvas_PreviewMouseLeftButtonDown;
            canvas.MouseLeftButtonUp += InkCanvas_PreviewMouseLeftButtonUp;
            canvas.MouseMove += InkCanvas_PreviewMouseMove;
            tab.Content = canvas;
            tabs.Items.Add(tab);
            tabs.SelectedIndex = filesCreated - 1;
        }

        private void newFile_MouseDown(object sender, MouseButtonEventArgs e)
        {
            AddTabButton_Click(sender);
        }

        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            SaveCloseWindow saveCloseWindow = new SaveCloseWindow();

            if(saveCloseWindow.ShowDialog()== true)
            {
                Save(sender);
            }

            int fileIndex = -1;

            for (int i = 1; i <= filesSavedIdexes.Count; i++)
            {
                if (filesSavedIdexes[i - 1] == tabs.SelectedIndex)
                {
                    fileIndex = i - 1;
                }
            }

            if(fileIndex != -1)
            {
                filesSavedIdexes.RemoveAt(tabs.SelectedIndex);
                filesSavedPaths.RemoveAt(tabs.SelectedIndex);
            }

            tabs.Items.RemoveAt(tabs.SelectedIndex);
            filesCreated--;
        }

        private void tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            lineItem.IsSelected = false;
            ellipseItem.IsSelected = false;
            starItem.IsSelected = false;
            if(((InkCanvas)tabs.SelectedContent) != null)
            {
                ((InkCanvas)tabs.SelectedContent).EditingMode = InkCanvasEditingMode.Ink;
            }
        }

        #endregion

        #region Виды пера
        private void pencilButton_Click(object sender, RoutedEventArgs e)
        {
            if (drawingOption != "")
            {
                drawingOption = "";
                selectedItem.IsSelected = false;
            }
            ((InkCanvas)tabs.SelectedContent).EditingMode = InkCanvasEditingMode.Ink;
        }

        private void eraserButton_Click(object sender, RoutedEventArgs e)
        {
            if (drawingOption != "")
            {
                drawingOption = "";
                selectedItem.IsSelected = false;
            }
            ((InkCanvas)tabs.SelectedContent).EditingMode = InkCanvasEditingMode.EraseByPoint;
        }

        private void lineEraserButton_Click(object sender, RoutedEventArgs e)
        {
            if (drawingOption != "")
            {
                drawingOption = "";
                selectedItem.IsSelected = false;
            }
            ((InkCanvas)tabs.SelectedContent).EditingMode = InkCanvasEditingMode.EraseByStroke;
        }
        #endregion

        #region Сохранение и загрузка файлов
        private void Save(object sender, MouseButtonEventArgs e = null)
        {
            int fileIndex = -1;

            for(int i = 1; i<=filesSavedIdexes.Count; i++)
            {
                if (filesSavedIdexes[i-1] == tabs.SelectedIndex)
                {
                    fileIndex = i-1;
                }
            }

            if(fileIndex == -1)
            {
                SaveAsCanvas(sender, e);
            }
            else
            {
                string filePath = filesSavedPaths[fileIndex];
                SaveCanvas(sender, e, filePath);
            }
        }

        private void SaveAsCanvas(object sender, MouseButtonEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp";
            saveFileDialog.Title = "Save File";
            saveFileDialog.FileName = "New file " + (tabs.SelectedIndex + 1).ToString();

            if (saveFileDialog.ShowDialog() == true)
            {
                RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(
                    (int)((InkCanvas)tabs.SelectedContent).ActualWidth,
                    (int)((InkCanvas)tabs.SelectedContent).ActualHeight,
                    96, 96,
                    PixelFormats.Pbgra32);

                renderTargetBitmap.Render(((InkCanvas)tabs.SelectedContent));

                PngBitmapEncoder pngEncoder = new PngBitmapEncoder();
                pngEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

                try
                {
                    using (FileStream fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                    {
                        pngEncoder.Save(fileStream);
                    }
                }
                catch(System.IO.IOException)
                {
                    int index = saveFileDialog.FileName.IndexOf('.');
                    saveFileDialog.FileName = saveFileDialog.FileName.Substring(0, index) + " New" + saveFileDialog.FileName.Substring(index);
                    using (FileStream fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                    {
                        pngEncoder.Save(fileStream);
                    }
                }

                filesSavedIdexes.Add(tabs.SelectedIndex);
                filesSavedPaths.Add(saveFileDialog.FileName);
            }
        }

        public void SaveCanvas(object sender, MouseButtonEventArgs e, string filePath)
        {
            if (tabs.SelectedContent != null)
            {
                RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(
                    (int)((InkCanvas)tabs.SelectedContent).ActualWidth,
                    (int)((InkCanvas)tabs.SelectedContent).ActualHeight,
                    96, 96,
                    PixelFormats.Pbgra32);

                renderTargetBitmap.Render(((InkCanvas)tabs.SelectedContent));

                PngBitmapEncoder pngEncoder = new PngBitmapEncoder();
                pngEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

                try
                {
                    using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        pngEncoder.Save(fileStream);
                    }
                }
                catch (System.IO.IOException)
                {
                    int index = filePath.IndexOf('.');
                    filePath = filePath.Substring(0, index) + " New" + filePath.Substring(index);
                    using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        pngEncoder.Save(fileStream);
                    }
                }
            }
        }

        private void LoadCanvas(object sender, MouseButtonEventArgs e)
        {
            newFile_MouseDown(sender, e);

            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp";
            openFileDialog.Title = "Load Image";

            if (openFileDialog.ShowDialog() == true)
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(openFileDialog.FileName);
                bitmapImage.EndInit();

                Image image = new Image();
                image.Source = bitmapImage;

                ((InkCanvas)tabs.SelectedContent).Strokes.Clear();
                ((InkCanvas)tabs.SelectedContent).Children.Clear();

                ((InkCanvas)tabs.SelectedContent).Children.Add(image);
            }

        }

        #endregion

        #region Цвет и толщина

        private void colorGallery_SelectedColorChanged(object sender, RoutedEventArgs e)
        {
            ColorGallery color = sender as ColorGallery;
            if (color.SelectedColor == null) ((InkCanvas)tabs.SelectedContent).DefaultDrawingAttributes.Color = Colors.Black; 
            else ((InkCanvas)tabs.SelectedContent).DefaultDrawingAttributes.Color = (Color)color.SelectedColor;
        }

        private void Button_Click1pt(object sender, RoutedEventArgs e)
        {
            ((InkCanvas)tabs.SelectedContent).DefaultDrawingAttributes.Width = 1;
            ((InkCanvas)tabs.SelectedContent).DefaultDrawingAttributes.Height = 1;
        }
        private void Button_Click3pt(object sender, RoutedEventArgs e)
        {
            ((InkCanvas)tabs.SelectedContent).DefaultDrawingAttributes.Width = 3;
            ((InkCanvas)tabs.SelectedContent).DefaultDrawingAttributes.Height = 3;
        }
        private void Button_Click5pt(object sender, RoutedEventArgs e)
        {
            ((InkCanvas)tabs.SelectedContent).DefaultDrawingAttributes.Width = 5;
            ((InkCanvas)tabs.SelectedContent).DefaultDrawingAttributes.Height = 5;
        }
        private void Button_Click8pt(object sender, RoutedEventArgs e)
        {
            ((InkCanvas)tabs.SelectedContent).DefaultDrawingAttributes.Width = 8;
            ((InkCanvas)tabs.SelectedContent).DefaultDrawingAttributes.Height = 8;
        }

        #endregion

        #region Масштабирование
        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            ZoomIn();
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            ZoomOut();
        }

        private void ZoomIn()
        {
            currentScale *= 1.2;
            ApplyScaleTransform();
        }

        private void ZoomOut()
        {
            currentScale *= 0.8;
            ApplyScaleTransform();
        }

        private void ApplyScaleTransform()
        {
            ((InkCanvas)tabs.SelectedContent).LayoutTransform = new ScaleTransform(currentScale, currentScale);
        }

        #endregion

        #region Окно о программе

        private void OpenAbout(object sender, MouseButtonEventArgs e)
        {
            var About = new About();
            About.Show();
        }

        #endregion

        #region Работа с фигурами

        private void line_MouseDown(object sender, MouseButtonEventArgs e)
        {
            selectedItem = (GalleryItem)sender;

            if (line != null)
            {
                ((InkCanvas)tabs.SelectedContent).Children.Remove(line);
                line = null;
            }

            drawingOption = "line";

            ((InkCanvas)tabs.SelectedContent).EditingMode = InkCanvasEditingMode.None;
        }

        private void ellipse_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            selectedItem = (GalleryItem)sender;

            if (ellipse != null)
            {
                ((InkCanvas)tabs.SelectedContent).Children.Remove(ellipse);
                ellipse = null;
            }

            drawingOption = "ellipse";

            ((InkCanvas)tabs.SelectedContent).EditingMode = InkCanvasEditingMode.None;
        }

        private void star_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            selectedItem = (GalleryItem)sender;

            drawingOption = "star";
            ((InkCanvas)tabs.SelectedContent).EditingMode = InkCanvasEditingMode.None;
        }

        private void InkCanvas_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (drawingOption == "") return;

            startPoint = e.GetPosition(((InkCanvas)tabs.SelectedContent));
            isDrawing = true;

            switch (drawingOption)
            {
                case "line":

                    line = new Line
                    {
                        Stroke = new SolidColorBrush(((InkCanvas)tabs.SelectedContent).DefaultDrawingAttributes.Color),
                        X1 = startPoint.X,
                        Y1 = startPoint.Y,
                        X2 = startPoint.X,
                        Y2 = startPoint.Y,
                        StrokeThickness = ((InkCanvas)tabs.SelectedContent).DefaultDrawingAttributes.Width
                    };

                    ((InkCanvas)tabs.SelectedContent).Children.Add(line);
                    break;
                case "ellipse":

                    ellipse = new Ellipse
                    {
                        Width = 0,
                        Height = 0,
                        Stroke = new SolidColorBrush(((InkCanvas)tabs.SelectedContent).DefaultDrawingAttributes.Color),
                        StrokeThickness = ((InkCanvas)tabs.SelectedContent).DefaultDrawingAttributes.Width
                    };

                    InkCanvas.SetLeft(ellipse, startPoint.X);
                    InkCanvas.SetTop(ellipse, startPoint.Y);


                    ((InkCanvas)tabs.SelectedContent).Children.Add(ellipse);
                    break;
                case "star":
                    isDelete = false;

                    star = new Polyline
                    {
                        Stroke = new SolidColorBrush(((InkCanvas)tabs.SelectedContent).DefaultDrawingAttributes.Color),
                        StrokeThickness = ((InkCanvas)tabs.SelectedContent).DefaultDrawingAttributes.Width
                    };

                    InkCanvas.SetLeft(star, startPoint.X);
                    InkCanvas.SetTop(star, startPoint.Y);

                    break;
            }
        }

        private void InkCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            endPoint = e.GetPosition(((InkCanvas)tabs.SelectedContent));


            if (!isDrawing) return;
            switch (drawingOption)
            {
                case ("line"):
                    line.X2 = endPoint.X;
                    line.Y2 = endPoint.Y;
                    break;

                case ("ellipse"):

                    InkCanvas.SetLeft(ellipse, Math.Min(startPoint.X, endPoint.X));
                    InkCanvas.SetTop(ellipse, Math.Min(startPoint.Y, endPoint.Y));

                    ellipse.Width = Math.Abs(startPoint.X - endPoint.X);
                    ellipse.Height = Math.Abs(startPoint.Y - endPoint.Y);

                    break;
                case ("star"):
                        DrawStar(sender, e);
                    break;
            }
        }

        private void InkCanvas_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!isDrawing) return;
            switch (drawingOption)
            {
                case ("line"):
                    ConvertLinesToStrokes();
                    line = null;
                    break;

                case ("ellipse"):
                    ConvertEllipseToStrokes();
                    ellipse = null;
                    break;
                case ("star"):
                    ConvertStarToStroke();
                    star = null;
                    break;
            }

            isDrawing = false;
        }

        private void ConvertLinesToStrokes()
        {
            StylusPointCollection points = new StylusPointCollection();
            points.Add(new StylusPoint(line.X1, line.Y1));
            points.Add(new StylusPoint(line.X2, line.Y2));

            Stroke stroke = new Stroke(points);

            stroke.DrawingAttributes.Color = ((InkCanvas)tabs.SelectedContent).DefaultDrawingAttributes.Color;
            stroke.DrawingAttributes.Width = ((InkCanvas)tabs.SelectedContent).DefaultDrawingAttributes.Width;
            stroke.DrawingAttributes.Height = ((InkCanvas)tabs.SelectedContent).DefaultDrawingAttributes.Height;
            ((InkCanvas)tabs.SelectedContent).Strokes.Add(stroke);
            ((InkCanvas)tabs.SelectedContent).Children.Remove(line);
        }

        private void ConvertEllipseToStrokes()
        {
            double centerX = (endPoint.X + startPoint.X) / 2;
            double centerY = (endPoint.Y + startPoint.Y) / 2;

            StylusPointCollection points = new StylusPointCollection();
            for (double angle = 0; angle <= 360; angle += 1)
            {
                double x = centerX + ellipse.Width / 2 * Math.Cos(angle * Math.PI / 180);
                double y = centerY + ellipse.Height / 2 * Math.Sin(angle * Math.PI / 180);
                points.Add(new StylusPoint(x, y));
            }

            Stroke stroke = new Stroke(points);

            stroke.DrawingAttributes.Color = ((InkCanvas)tabs.SelectedContent).DefaultDrawingAttributes.Color;
            stroke.DrawingAttributes.Width = ((InkCanvas)tabs.SelectedContent).DefaultDrawingAttributes.Width;
            stroke.DrawingAttributes.Height = ((InkCanvas)tabs.SelectedContent).DefaultDrawingAttributes.Height;

            ((InkCanvas)tabs.SelectedContent).Strokes.Add(stroke);

            ((InkCanvas)tabs.SelectedContent).Children.Remove(ellipse);
        }

        private void ConvertStarToStroke()
        {
            if (star == null)
                return;

            PointCollection starPoints = star.Points;

            StylusPointCollection points = new StylusPointCollection();

            double left = InkCanvas.GetLeft(star);
            double top = InkCanvas.GetTop(star);

            foreach (Point point in starPoints)
            {
                points.Add(new StylusPoint(point.X + left, point.Y + top));
            }

            if(points.Count == 0) return;

            Stroke stroke = new Stroke(points);

            stroke.DrawingAttributes.Color = ((InkCanvas)tabs.SelectedContent).DefaultDrawingAttributes.Color;
            stroke.DrawingAttributes.Width = ((InkCanvas)tabs.SelectedContent).DefaultDrawingAttributes.Width;
            stroke.DrawingAttributes.Height = ((InkCanvas)tabs.SelectedContent).DefaultDrawingAttributes.Height;

            ((InkCanvas)tabs.SelectedContent).Strokes.Add(stroke);

            ((InkCanvas)tabs.SelectedContent).Children.Remove(star);

            star = null;
        }

        private void DrawStar(object sender, MouseEventArgs e)
        {
            endPoint = e.GetPosition(((InkCanvas)tabs.SelectedContent));

            double width = Math.Abs(startPoint.X - endPoint.X);
            double height = Math.Abs(startPoint.Y - endPoint.Y);

            double x0 = width / 2;
            double y0 = height / 2;

            int n = vertex;

            double ratio = 2;

            double R = Math.Min(width, height) / 2;
            double r = R / ratio;


            PointCollection points= new PointCollection();

            double rotateValue = Math.PI / n;

            double angle = 0;

            if (n % 4 == 0)
                angle = 0;
            else if (n % 4 == 1)
                angle = 1.5 * rotateValue;
            else if (n % 4 == 2)
                angle = rotateValue;
            else
                angle = 1.5 * rotateValue - Math.PI;

            double currRadius;
            for (int k = 0; k < 2 * n + 1; k++)
            {
                if (k % 2 == 0)
                    currRadius = R;
                else
                    currRadius = r;
                points.Add(new Point((float)(x0 + currRadius * Math.Cos(angle)), (float)(y0 + currRadius * Math.Sin(angle))));
                angle += rotateValue;
            }


            star.Points = points;

            Debug.WriteLine($"{points}");

            InkCanvas.SetTop(star, Math.Min(endPoint.Y, startPoint.Y));
            InkCanvas.SetLeft(star, Math.Min(endPoint.X, startPoint.X));

            if (isDelete)
            {
                ((InkCanvas)tabs.SelectedContent).Children.RemoveAt(((InkCanvas)tabs.SelectedContent).Children.Count - 1);
            }

            ((InkCanvas)tabs.SelectedContent).Children.Add(star);
            isDelete = true;
        }

        private void vertexCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            vertex = (int)vertexCount.Value;
        }

        #endregion

    }
}