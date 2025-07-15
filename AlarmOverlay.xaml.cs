using System.IO;
using System.Media;
using System.Media;
using System.Windows;
using System.Windows.Media.Animation;
namespace ui_monitor
{
    /// <summary>
    /// Interaction logic for AlarmOverlay.xaml
    /// </summary>
    public partial class AlarmOverlay : Window
    {
        private Storyboard? _glowStoryboard;
        private SoundPlayer? _player;
        private MemoryStream? _soundStream;

        public AlarmOverlay(Rect bounds)
        {
            InitializeComponent();

            Left = bounds.X;
            Top = bounds.Y;
            Width = bounds.Width;
            Height = bounds.Height;

            StartGlowPulse();
        }

        private void StartGlowPulse()
        {
            var glowAnim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 10,
                To = 30,
                Duration = TimeSpan.FromSeconds(0.5),
                AutoReverse = true,
                RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever,
                EasingFunction = new System.Windows.Media.Animation.SineEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseInOut }
            };

            GlowEffect.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.BlurRadiusProperty, glowAnim);
            StartAudio();
        }
        private void StartAudio()
        {
            try
            {
                byte[] wavBytes = File.ReadAllBytes("alarm.wav");
                _soundStream = new MemoryStream(wavBytes);
                _player = new SoundPlayer(_soundStream);
                _player.PlayLooping();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Alarm audio error: {ex.Message}");
            }
        }

        public void StopGlow()
        {
            GlowEffect?.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.BlurRadiusProperty, null);

            if (_player != null)
            {
                _player.Stop();
                _player.Dispose();
                _player = null;
            }

            _soundStream?.Dispose();
            _soundStream = null;
        }
    }

}
