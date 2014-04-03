using System;
using System.Linq;
using NAudio.Wave;

namespace Spotbox
{
    public class HaltableBufferedWaveProvider : IWaveProvider
    {
        private readonly BufferedWaveProvider _bufferedWaveProvider;
        private bool _bufferFinished=false;

        public HaltableBufferedWaveProvider(WaveFormat waveFormat)
        {
            _bufferedWaveProvider = new BufferedWaveProvider(waveFormat);
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            var byteCount = _bufferedWaveProvider.Read(buffer, offset, count);
            
            if (!_bufferFinished) return byteCount;
            
            if (buffer.All(b => b.Equals(0)))
            {
                byteCount = 0;
            }

            return byteCount;
        }

        public WaveFormat WaveFormat
        {
            get
            {
                return _bufferedWaveProvider.WaveFormat;
            }
        }

        public TimeSpan BufferDuration {
            get
            {
                return _bufferedWaveProvider.BufferDuration;
            } 
            set
            {
                _bufferedWaveProvider.BufferDuration = value;
            }
        }

        public bool DiscardOnBufferOverflow
        {
            get
            {
                return _bufferedWaveProvider.DiscardOnBufferOverflow;
            }
            set
            {
                _bufferedWaveProvider.DiscardOnBufferOverflow = value;
            }
        }

        public void AddSamples(byte[] buffer, int i, int length)
        {
            _bufferedWaveProvider.AddSamples(buffer, i, length);
        }

        public void SetBufferFinished()
        {
            _bufferFinished = true;
        }
    }
}
