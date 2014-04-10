using System;
using System.Reflection;

using Microsoft.Owin.Hosting;

using Nancy.TinyIoc;
using log4net;
using SpotSharp;

namespace Spotbox
{
    class Program
    {
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        static void Main()
        {
            StartServer();
        }

        private static void StartServer()
        {
            const string hostUri = "http://+:4050/";

            using (WebApp.Start<Startup>(hostUri))
            {
                _logger.InfoFormat("Hosting Spotbox at: {0}", hostUri);

                PlayLastPlayingPlaylist();

                Console.ReadLine();
            }
        }


        public static void PlayLastPlayingPlaylist()
        {
            var spotify = TinyIoCContainer.Current.Resolve<Spotify>();

            var lastPlaylistName = Settings.Default.CurrentPlaylistName;
            if (lastPlaylistName != string.Empty)
            {
                var foundPlaylistInfo = spotify.GetPlaylistInfo(lastPlaylistName);
                if (foundPlaylistInfo != null)
                {
                    var lastPosition = Settings.Default.CurrentPlaylistPosition;

                    var playlist = foundPlaylistInfo.GetPlaylist();
                    spotify.SetCurrentPlaylist(playlist);
                    spotify.SetCurrentPlaylistPosition(lastPosition);
                    spotify.Play();
                    return;
                }
            }

            spotify.PlayDefaultPlaylist();
        }
    }
}
