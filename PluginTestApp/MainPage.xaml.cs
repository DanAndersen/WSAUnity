using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;

using Org.WebRtc;
using Windows.UI.Core;
using Windows.Media.Playback;
using Windows.Media.Core;
using WSAUnity;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PluginTestApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        StarWebrtcContext starWebrtcContext;
        MediaPlayer _mediaPlayer;

        public MainPage()
        {
            this.InitializeComponent();

            Debug.WriteLine("MainPage()");

            _mediaPlayer = new MediaPlayer();
            mediaPlayerElement.SetMediaPlayer(_mediaPlayer);

            starWebrtcContext = StarWebrtcContext.CreateTraineeContext();
            // right after creating the context (before starting the connections), we could edit some parameters such as the signalling server

            // comment these out if not needed
            //Messenger.AddListener<string>(SympleLog.LogTrace, OnLog);
            Messenger.AddListener<string>(SympleLog.LogDebug, OnLog);
            Messenger.AddListener<string>(SympleLog.LogInfo, OnLog);
            Messenger.AddListener<string>(SympleLog.LogError, OnLog);

            Messenger.AddListener<IMediaSource>(SympleLog.MediaSource, OnMediaSource);
        }

        private void OnMediaSource(IMediaSource source)
        {
            Messenger.Broadcast(SympleLog.LogDebug, "OnMediaSource");
            _mediaPlayer.Source = MediaSource.CreateFromIMediaSource(source);
            _mediaPlayer.Play();
        }

        private void OnLog(string msg)
        {
            Debug.WriteLine(msg);

            // http://stackoverflow.com/questions/19341591/the-application-called-an-interface-that-was-marshalled-for-a-different-thread
            Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                // Your UI update code goes here!
                textBox.Text += msg + "\n";
            }
            );
            
        }
        
        

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            button.IsEnabled = false;

            starWebrtcContext.initAndStartWebRTC();

            
            //p.basicTestVideo();
        }
    }
}
