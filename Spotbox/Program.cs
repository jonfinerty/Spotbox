using System;
using System.Configuration;
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

                var doNotStream = ConfigurationManager.AppSettings["DoNotStream"];
                if (doNotStream == null || Boolean.Parse(doNotStream) == false )
                {
                    _logger.Info("Able to stream a spotify playlist, I'll find the last playlist played.");
                    SelectAndPlayBestPlayList();
                }
                else
                {
                    _logger.Info("'DoNotStream' is set true in App Settings, so I'm not actually going to stream the play list to audio, but all other fn should work.  Happy testing.");
                }

                Console.ReadLine();
            }
        }

        // TODO: check with JF, is this best place for this code?
        public static void SelectAndPlayBestPlayList()
        {
            var spotify = TinyIoCContainer.Current.Resolve<SpotSharp.SpotSharp>();

            Link bestPlayList = DefaultPlayList() ?? LastPlayingPlaylist();

            if (bestPlayList != null)
            {
                var playlistSet = spotify.SetCurrentPlaylist(bestPlayList);
                if (playlistSet)
                {
                    spotify.Play();
                    return;
                }
            }

            // Allow spotify to choose (likely based on first one with a track)
            spotify.PlayDefaultPlaylist();

        }


        // TODO: check with JF, is this best place for this code?
        private static Link DefaultPlayList()
        {
            var defaultPlayList = ConfigurationManager.AppSettings["DefaultPlayList"];
            
            if (defaultPlayList != null)
            {
                if (! defaultPlayList.StartsWith("spotify:"))
                {
                    var spotify = TinyIoCContainer.Current.Resolve<SpotSharp.SpotSharp>();
                    var playlists = spotify.GetAllPlaylists();

                    var possiblePlayList = playlists.Find(x => x.Name.Equals(defaultPlayList));
                    if (possiblePlayList != null)
                    {
                        _logger.InfoFormat("Found link of default play list: {0} , link: {1}", defaultPlayList, possiblePlayList.Link);
                        return possiblePlayList.Link;
                    }
                }
            }
            _logger.InfoFormat("Default play list: {0} ", defaultPlayList);
            
            return Link.Create( defaultPlayList );

        }

        // TODO: check with JF, is this best place for this code?
        private static Link LastPlayingPlaylist()
        {
            var lastPlaylistLink = Settings.Default.CurrentPlaylistLink;
            if (lastPlaylistLink != string.Empty)
            {
                var link = Link.Create(lastPlaylistLink);
                 
                if (link != null)
                {
                    _logger.InfoFormat("Found link of last playing play list: {0}", link);

                    return link;
                }
            }

            return null;
        }
    }
}
