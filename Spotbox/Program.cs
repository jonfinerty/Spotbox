using System;
using System.Configuration;
using System.IO;
using System.Reflection;

using Microsoft.Owin.Hosting;

using Nancy.TinyIoc;

using Spotbox.Player;
using Spotbox.Player.Spotify;

using log4net;

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
            const string hostUri = "http://+:80/";

            using (WebApp.Start<Startup>(hostUri))
            {
                _logger.InfoFormat("Hosting Spotbox at: {0}", hostUri);

                var spotify = TinyIoCContainer.Current.Resolve<Spotify>();
                spotify.PlayLastPlayingPlaylist();

                Console.ReadLine();
            }
        }
    }
}
