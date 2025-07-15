using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ui_monitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static List<MonitorZone> activeMonitors = new();
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) =>
            {
                var screenWidth = SystemParameters.PrimaryScreenWidth;
                var windowWidth = this.Width;

                this.Left = (screenWidth - windowWidth) / 2;
                this.Top = 0;
            };
        }
        private void Close_Selection(object sender, RoutedEventArgs e)
        {
            var activemon = activeMonitors.FirstOrDefault(m => m.IsSelected == true);
            if (activemon != null)
            {
                activemon.CloseSelection();
            }
        }
        public static void RegisterMonitor(MonitorZone zone)
        {
            activeMonitors.Add(zone);
        }

        public static void DeselectAll()
        {
            foreach (var zone in activeMonitors)
            {
                zone.SetSelected(false);
            }
        }

        private void AddRect_Click(object sender, RoutedEventArgs e)
        {
            var overlay = new SelectionOverlay
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Topmost = true,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = 0,
                Top = 0,
                Width = SystemParameters.PrimaryScreenWidth,
                Height = SystemParameters.PrimaryScreenHeight
            };

            overlay.ShowDialog();

            if (overlay.SelectionConfirmed)
            {
                Rect area = overlay.SelectedArea;

                // Create and show the monitor window in selected area
                var monitor = new MonitorZone(area); // you'll need to implement this
                monitor.Left = area.X;
                monitor.Top = area.Y;
                monitor.Width = area.Width;
                monitor.Height = area.Height;
                monitor.Show();
            }
        }


        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Allow window dragging from anywhere
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}