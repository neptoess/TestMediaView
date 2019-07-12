using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Vlc.DotNet.Core;
using Vlc.DotNet.Core.Interops.Signatures;
using Vlc.DotNet.Forms;

namespace TestMediaView
{
    /// <summary>
    /// Interaction logic for VideoViewer.xaml
    /// </summary>
    public partial class VideoViewer : UserControl
    {
        private static readonly SynchronizationContext UiContext = SynchronizationContext.Current;
        private static readonly DirectoryInfo VlcLibDirectory =
            new DirectoryInfo(
                Path.Combine(
                    new FileInfo(Assembly.GetEntryAssembly().Location).DirectoryName,
                    "libvlc",
                    IntPtr.Size == 4 ? "win-x86" : "win-x64"));
        private static readonly DependencyProperty FilePathProperty =
            DependencyProperty.Register(
                "FilePath",
                typeof(Uri),
                typeof(VideoViewer),
                new PropertyMetadata(OnFilePathChanged));
        private static readonly DependencyProperty PlaybackRateProperty =
            DependencyProperty.Register(
                "PlaybackRate",
                typeof(float),
                typeof(VideoViewer),
                new PropertyMetadata(OnPlaybackRateChanged));
        private static readonly DependencyProperty PausePlaybackProperty =
            DependencyProperty.Register(
                "PausePlayback",
                typeof(bool),
                typeof(VideoViewer),
                new PropertyMetadata(OnPausePlaybackChanged));

        private readonly VlcControl vlcControl = new VlcControl();

        public VideoViewer()
        {
            this.InitializeComponent();

            this.vlcControl.BeginInit();
            this.vlcControl.VlcLibDirectory = VlcLibDirectory;
            this.vlcControl.VlcMediaplayerOptions = new string[] { };
            this.vlcControl.EndInit();

            this.vlcControl.EndReached += this.VlcControl_EndReached;
            this.winFormsHost.Child = this.vlcControl;
        }

        public Uri FilePath
        {
            get => (Uri)GetValue(FilePathProperty);
            set => SetValue(FilePathProperty, value);
        }

        public float PlaybackRate
        {
            get => (float)GetValue(PlaybackRateProperty);
            set => SetValue(PlaybackRateProperty, value);
        }

        public bool PausePlayback
        {
            get => (bool)GetValue(PausePlaybackProperty);
            set => SetValue(PausePlaybackProperty, value);
        }

        private static void InvokeIfRequired(Action action)
        {
            if (SynchronizationContext.Current != UiContext)
            {
                UiContext.Post((_) => action(), null);
            }
            else
            {
                action();
            }
        }

        private static void OnFilePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var t = d as VideoViewer;
            if (string.IsNullOrWhiteSpace(e.NewValue?.ToString()))
            {
                InvokeIfRequired(() => t.vlcControl.Stop());
            }
            else
            {
                InvokeIfRequired(() => t.vlcControl.Play(e.NewValue as Uri));
            }
        }

        private static void OnPlaybackRateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var t = d as VideoViewer;
            InvokeIfRequired(() => t.vlcControl.Rate = (float)e.NewValue);
        }

        private static void OnPausePlaybackChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var t = d as VideoViewer;
            var pause = (bool)e.NewValue;
            InvokeIfRequired(() =>
                {
                    if (pause && t.vlcControl.IsPlaying)
                    {
                        t.vlcControl.Pause();
                    }
                    else if (!pause && t.vlcControl.State == MediaStates.Paused)
                    {
                        t.vlcControl.Play();
                    }
                });
        }

        private void VlcControl_EndReached(object sender, VlcMediaPlayerEndReachedEventArgs e)
        {
            // TODO: Load next file if available.
            InvokeIfRequired(() => this.FilePath = null);
        }
    }
}
