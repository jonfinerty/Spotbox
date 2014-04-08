using System.Speech.Synthesis;
using Nancy;
using Nancy.ModelBinding;
using Spotbox.Api.Models;
using Spotbox.Player;

namespace Spotbox.Api
{
    public class Speak : NancyModule
    {
        public Speak()
        {
            Post["/speak"] = x =>
            {
                Audio.Pause();
                var speach = this.Bind<SimpleInput>();
                var synthesizer = new SpeechSynthesizer
                {
                    Volume = 100, 
                    Rate = -2
                };
                
                synthesizer.Speak(speach.Value);
                Audio.Play();
                return HttpStatusCode.OK;
            };
        }
    }
}
