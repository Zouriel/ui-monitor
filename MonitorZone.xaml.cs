using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;




namespace ui_monitor
{
    /// <summary>
    /// Interaction logic for MonitorZone.xaml
    /// </summary>
    public partial class MonitorZone : Window
    {
        public bool IsSelected { get; private set; }
        public BitmapSource? Snapshot { get; private set; }
        public BitmapSource? Snapshot2 { get; private set; } 
        private Window? _snapshotPreviewWindow;
        private AlarmOverlay? _alarmOverlay;
        private DispatcherTimer? _monitorTimer;
        private byte[]? _lastSnapshotData;

        // Event or callback for significant changes
        public event Action? MonitorZoneChanged;
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        public MonitorZone(Rect area)
        {
            InitializeComponent();
            Left = area.X;
            Top = area.Y;
            Width = area.Width;
            Height = area.Height;

            // Transparent window with soft border
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            WindowStyle = WindowStyle.None;
            //Topmost = true;

            // Optional: place a border inside
            var border = new Border
            {
                BorderBrush = Brushes.DarkGray,
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)) // subtle dark overlay
            };

            Content = border;
        }
        private bool IsSignificantlyDifferent(byte[] current, byte[] previous, int threshold = 50)
        {
            if (current.Length != previous.Length) return true;

            int diff = 0;
            for (int i = 0; i < current.Length; i++)
            {
                if (Math.Abs(current[i] - previous[i]) > 16) // Ignore tiny color jitters
                    diff++;

                if (diff > threshold)
                    return true;
            }
            return false;
        }
        public void WarningSetter()
        {

        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            MainWindow.RegisterMonitor(this); 
            this.CaptureSnapshot("this");

            StartMonitoringZoneChanges();
            MonitorZoneChanged += () =>
            {
                if (_alarmOverlay == null)
                {
                    var offset = 4; // 4px outside the monitor zone
                    var bounds = new Rect(
                        this.Left - offset,
                        this.Top - offset,
                        this.Width + offset * 2,
                        this.Height + offset * 2
                    );
                    _alarmOverlay = new AlarmOverlay(bounds);
                    _alarmOverlay.Closed += (_, __) => _alarmOverlay = null;
                    _alarmOverlay.Show();
                }
            };

        }

        private void CaptureSnapshot(string from)
        {
            int x = (int)this.Left;
            int y = (int)this.Top;
            int width = (int)this.Width;
            int height = (int)this.Height;

            if (width <= 0 || height <= 0)
                return;

            // Hide self so we don’t capture our glow or overlay
            this.Visibility = Visibility.Hidden;
            if(_snapshotPreviewWindow != null) this._snapshotPreviewWindow.Hide();
            System.Threading.Thread.Sleep(100); // wait 1 frame

            using var bmp = new System.Drawing.Bitmap(width, height);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(x, y, 0, 0, bmp.Size);
            }

            IntPtr hBitmap = bmp.GetHbitmap();
            try
            {
                var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                bitmapSource.Freeze();
                if (from == "monitor") Snapshot2 = bitmapSource;
                if (from == "this") Snapshot = bitmapSource;
            }
            finally
            {
                DeleteObject(hBitmap);
                this.Visibility = Visibility.Visible; // bring it back
                if(_snapshotPreviewWindow != null) this._snapshotPreviewWindow.Hide();

            }
        }

        private void boarderGlower()
        {

        }


        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsSelected)
            {
                StartMonitoringZoneChanges();
                MainWindow.DeselectAll();
            }
            else{
                MainWindow.DeselectAll(); // deselect others first
                SetSelected(true);
                
            }
            
        }


        public void SetSelected(bool selected)
        {
            IsSelected = selected;

            if (selected)
            {
                
                var accentColor = GetWindowsAccentColor();
                GlassBorder.Background = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)); // semi-dark
                GlassBorder.BorderBrush = new SolidColorBrush(accentColor);
                StopMonitoringZoneChanges();
            }
            else
            {
                GlassBorder.Background = Brushes.Transparent;
                GlassBorder.BorderBrush = Brushes.Transparent;

            }
        }
        private Color GetWindowsAccentColor()
        {
            try
            {
                var color = SystemParameters.WindowGlassColor;
                return Color.FromArgb(255, color.R, color.G, color.B);
            }
            catch
            {
                return Colors.DeepSkyBlue; // fallback
            }
        }

        public void CloseSelection()
        {
            this.StopMonitoringZoneChanges();
            if (_alarmOverlay != null)
            {
                _alarmOverlay.Close();
                _alarmOverlay = null;
            }
            this.Close();
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            var accentColor = GetWindowsAccentColor();
            GlassBorder.Background = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)); // semi-dark
            GlassBorder.BorderBrush = new SolidColorBrush(accentColor);

            if (Snapshot != null && _snapshotPreviewWindow == null)
            {
                _snapshotPreviewWindow = new Window
                {
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = Brushes.Transparent,
                    IsHitTestVisible = false,
                    ShowInTaskbar = false,
                    Topmost = true,
                    Width = Snapshot.PixelWidth,
                    Height = Snapshot.PixelHeight,
                    Content = new Image
                    {
                        Source = Snapshot,
                        Stretch = Stretch.None
                    }
                };
                _snapshotPreviewWindow.Show();
            }

            // Position the preview window at the mouse location
            UpdateSnapshotPreviewPosition(e);
            this.MouseMove += Window_MouseMove_ShowSnapshot;
            this.MouseLeave += Window_MouseLeave_HideSnapshot;
            StopMonitoringZoneChanges();
        }
        private void Window_MouseMove_ShowSnapshot(object sender, MouseEventArgs e)
        {
            UpdateSnapshotPreviewPosition(e);
        }
        private void Window_MouseLeave_HideSnapshot(object sender, MouseEventArgs e)
        {
            if (_snapshotPreviewWindow != null)
            {
                _snapshotPreviewWindow.Close();
                if(!IsSelected)StartMonitoringZoneChanges();
                _snapshotPreviewWindow = null;
            }
            this.MouseMove -= Window_MouseMove_ShowSnapshot;
            this.MouseLeave -= Window_MouseLeave_HideSnapshot;
            

        }

        private void UpdateSnapshotPreviewPosition(MouseEventArgs e)
        {
            if (_snapshotPreviewWindow != null)
            {
                var mousePos = e.GetPosition(null);
                var screenPos = PointToScreen(mousePos);
                _snapshotPreviewWindow.Left = screenPos.X + 16; // offset from cursor
                _snapshotPreviewWindow.Top = screenPos.Y + 16;
            }
        }
        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            var Selected = IsSelected;
            if (Selected)
            {
                var accentColor = GetWindowsAccentColor();
                GlassBorder.Background = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)); // semi-dark
                GlassBorder.BorderBrush = new SolidColorBrush(accentColor);
            }
            else
            {
                GlassBorder.Background = Brushes.Transparent;
                GlassBorder.BorderBrush = Brushes.Transparent;
            }
        }

        public void StartMonitoringZoneChanges(int intervalMs = 1000)
        {
            _monitorTimer ??= new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(intervalMs)
            };
            _monitorTimer.Tick += MonitorTimer_Tick;
            _monitorTimer.Start();
        }

        public void StopMonitoringZoneChanges()
        {
            if (_monitorTimer != null)
            {
                _monitorTimer.Stop();
                _monitorTimer.Tick -= MonitorTimer_Tick;
                _monitorTimer = null;
            }
        }

        private void MonitorTimer_Tick(object? sender, EventArgs e)
        {
            CaptureSnapshot("monitor");
            if (Snapshot == null)
                return;

            // Downscale for performance and noise reduction
            var scaled = new TransformedBitmap(Snapshot2, new ScaleTransform(0.1, 0.1));
            int stride = (scaled.PixelWidth * scaled.Format.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[scaled.PixelHeight * stride];
            scaled.CopyPixels(pixels, stride, 0);

            // Quantize colors to ignore minor changes (e.g., round to nearest 16)
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = (byte)((pixels[i] / 16) * 16);

            if (_lastSnapshotData != null && IsSignificantlyDifferent(pixels, _lastSnapshotData))
            {
                OnMonitorZoneChanged();
            }

            _lastSnapshotData = pixels;
        }

        protected virtual void OnMonitorZoneChanged()
        {
            MonitorZoneChanged?.Invoke();
        }

        private async void GlassBorder_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.StopMonitoringZoneChanges();

            if (_alarmOverlay != null)
            {
                _alarmOverlay.StopGlow();
                _alarmOverlay.Close();
                _alarmOverlay = null;
            }

            this.Snapshot = null;

            // Wait a short time to allow any overlays or visual artifacts to disappear from screen
            await Task.Delay(300);

            this.CaptureSnapshot("this");

            StartMonitoringZoneChanges();
        }
        public void TriggerAlarm()
        {
            if (_alarmOverlay != null) return; // already showing

            var rect = new Rect(this.Left, this.Top, this.Width, this.Height);
            _alarmOverlay = new AlarmOverlay(rect);
            _alarmOverlay.Owner = this;
            _alarmOverlay.Show();
        }

        public void StopAlarm()
        {
            _alarmOverlay?.StopGlow();
            _alarmOverlay = null;
        }
    }
}
