using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ui_monitor
{
    /// <summary>
    /// Interaction logic for SelectionOverlay.xaml
    /// </summary>
    public partial class SelectionOverlay : Window
    {
        private Point startPoint;
        private Rectangle selectionRect;
        public Rect SelectedArea { get; private set; }
        public bool SelectionConfirmed { get; private set; }

        public SelectionOverlay()
        {
            InitializeComponent();
            selectionRect = new Rectangle
            {
                Stroke = Brushes.DeepSkyBlue,
                StrokeThickness = 2,
                RadiusX = 4,
                RadiusY = 4
            };
            OverlayCanvas.Children.Add(selectionRect);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(OverlayCanvas);
            Canvas.SetLeft(selectionRect, startPoint.X);
            Canvas.SetTop(selectionRect, startPoint.Y);
            selectionRect.Width = 0;
            selectionRect.Height = 0;
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point pos = e.GetPosition(OverlayCanvas);

                double x = Math.Min(pos.X, startPoint.X);
                double y = Math.Min(pos.Y, startPoint.Y);
                double w = Math.Abs(pos.X - startPoint.X);
                double h = Math.Abs(pos.Y - startPoint.Y);

                Canvas.SetLeft(selectionRect, x);
                Canvas.SetTop(selectionRect, y);
                selectionRect.Width = w;
                selectionRect.Height = h;
            }
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Point endPoint = e.GetPosition(OverlayCanvas);

            double x = Math.Min(startPoint.X, endPoint.X);
            double y = Math.Min(startPoint.Y, endPoint.Y);
            double width = Math.Abs(endPoint.X - startPoint.X);
            double height = Math.Abs(endPoint.Y - startPoint.Y);

            SelectedArea = new Rect(x, y, width, height);
            SelectionConfirmed = true;
            DialogResult = true;
            Close();
        }
    }
}