using System.Speech.Synthesis;
using Nancy;
using Nancy.ModelBinding;
using Spotbox.Api.Models;
using Spotbox.Player.Spotify;

namespace Spotbox.Api
{
    public class Speak : NancyModule
    {
        public Speak(Spotify spotify)
        {
            Post["/speak"] = x =>
            {
                spotify.GetCurrentPlaylist().Pause();
                var speach = this.Bind<SimpleInput>();
                var synthesizer = new SpeechSynthesizer
                {
                    Volume = 100, 
                    Rate = -2
                };
                
                synthesizer.Speak(speach.Value);
                spotify.GetCurrentPlaylist().Play();
                return HttpStatusCode.OK;
            };
        }
    }
}
