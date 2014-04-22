using System;
using System.Reflection;
using System.Web.UI;
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
            var spotify = TinyIoCContainer.Current.Resolve<SpotSharp.SpotSharp>();

            var lastPlaylistLink = Settings.Default.CurrentPlaylistLink;
            if (lastPlaylistLink != string.Empty)
            {
                var link = new Link(lastPlaylistLink);
                var playlistSet = spotify.SetCurrentPlaylist(link);
                if (playlistSet)
                {
                    spotify.Play();
                    return;
                }
            }

            spotify.PlayDefaultPlaylist();
        }
    }
}
