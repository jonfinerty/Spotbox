using System.Speech.Synthesis;
using Nancy;
using Nancy.ModelBinding;
using Spotbox.Api.Models;
using SpotSharp;

namespace Spotbox.Api
{
    public class Speak : NancyModule
    {
        public Speak(Spotify spotify)
        {
            Post["/speak"] = x =>
            {
                spotify.Pause();
                var speach = this.Bind<LinkModel>();
                var synthesizer = new SpeechSynthesizer
                {
                    Volume = 100, 
                    Rate = -2
                };
                
                synthesizer.Speak(speach.Link);
                spotify.Play();
                return HttpStatusCode.OK;
            };
        }
    }
}
