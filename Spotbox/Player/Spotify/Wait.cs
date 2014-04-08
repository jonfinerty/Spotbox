using System;
using System.Threading;

namespace Spotbox.Player.Spotify
{
    public class Wait
    {        
        public static bool For(Func<bool> isFinishedTest)
        {
            const int DefaultTimeoutInSeconds = 10;

            var start = DateTime.Now;

            while (DateTime.Now.Subtract(start).Seconds < DefaultTimeoutInSeconds)
            {
                if (isFinishedTest.Invoke())
                {
                    return true;
                }

                Thread.Sleep(250);
            }

            return false;
        }
    }
}
