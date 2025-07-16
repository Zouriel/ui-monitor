using System;
using System.IO;
using System.Media;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Animation;

namespace ui_monitor
{
    /// <summary>
    /// Represents a pulsing red alarm overlay with sound.
    /// </summary>
    public partial class AlarmOverlay : Window
    {
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
            var glowAnim = new DoubleAnimation
            {
                From = 10,
                To = 30,
                Duration = TimeSpan.FromSeconds(0.5),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };

            GlowEffect.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.BlurRadiusProperty, glowAnim);
            StartAudio();
        }

        private void StartAudio()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                // Debug check — run this once to see correct name in Output window
                foreach (var name in assembly.GetManifestResourceNames())
                    Console.WriteLine("[Resource] " + name);

                var resourceName = "ui_monitor.alarm.wav"; // Update this after checking!

                var resource = assembly.GetManifestResourceStream(resourceName);
                if (resource == null)
                {
                    Console.WriteLine($"[AlarmOverlay] Resource '{resourceName}' not found.");
                    return;
                }

                _soundStream = new MemoryStream();
                resource.CopyTo(_soundStream);
                _soundStream.Position = 0;

                _player = new SoundPlayer(_soundStream);
                _player.PlayLooping();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AlarmOverlay] Failed to play alarm sound: {ex.Message}");
            }
        }

        public void StopGlow()
        {
            GlowEffect?.BeginAnimation(System.Windows.Media.Effects.DropShadowEffect.BlurRadiusProperty, null);

            _player?.Stop();
            _player?.Dispose();
            _player = null;

            _soundStream?.Dispose();
            _soundStream = null;
        }
    }
}
