using System;
using System.Diagnostics;
using System.Threading;
using NetSharedMemory;
using ZeroVolumeFader.CommandLine;

namespace ZeroVolumeFader
{
    public class ZeroVolumeFader
    {
        static readonly SharedMemory<State> Memory = new SharedMemory<State>("FADETOZERO_MEMORY", 2);

        public event EventHandler<VolumeFaderEndedEvent> OnVolumeFaderEnded;
        public event EventHandler<VolumeFaderEndedEvent> OnVolumeFaderWaitFinished;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private bool IsFading {
            get { return State.IsFading; }
            set { State = new State {IsFading = value}; }
        }
        private bool IsStopFadeRequested => State.StopRequested;
        private State State
        {
            get
            {
                Memory.Open();
                var data = Memory.Data;
                Memory.Close();
                return data;
            }
            set
            {
                Memory.Open();
                Memory.Data = value;
                Memory.Close();
            }
        }
        private void CallVolumeFaderEndedCallbacks(VolumeFaderEndedEvent @event)
            => OnVolumeFaderEnded?.Invoke(this, @event);
        private void CallVolumeFaderWaitFinishedCallbacks(VolumeFaderEndedEvent @event = null) => 
            OnVolumeFaderWaitFinished?.Invoke(this, @event ?? new VolumeFaderEndedEvent());

        public void Run(double waitTimeMinutes, double fadeTimeMinutes) => 
            FadeVolumeLoop(waitTimeMinutes, fadeTimeMinutes);
        public void Stop()
        {
            State = new State { StopRequested = true };
            PlayCancellingBeep();
        }
        private void PlayStartBeep()
        {
            Console.Beep(2000, 200);
            Console.Beep(1000, 200);
        }
        private void PlayCancellingBeep()
        {
            Console.Beep(2000, 200);
            Console.Beep(2000, 200);
        }
        private void PlayWasCanceledBeep()
        {
            Console.Beep(1000, 200);
            Console.Beep(1000, 200);
        }
        private void FadeVolumeLoop(double waitTimeBeforeFadeMinutes, double fadeTimeMinutes)
        {
            if (IsFading) return;

            PlayStartBeep();

            _stopwatch.Start();

            IsFading = true;

            //Setup
            var offsetTickWaitMs = 200;
            var fadeTimeMs = fadeTimeMinutes * 60 * 1000;
            var volumeDistanceToFade = Audio.Volume + 0.005; 
            var fadeRate = offsetTickWaitMs * (volumeDistanceToFade / fadeTimeMs);

            WaitBeforeFade(waitTimeBeforeFadeMinutes);

            CallVolumeFaderWaitFinishedCallbacks();

            Debug.WriteLine($"Starting fade-effect by {fadeRate} over {fadeTimeMs} ms");
            while (true)
            {

                if (IsStopFadeRequested)
                {
                    PlayWasCanceledBeep();
                    CallVolumeFaderEndedCallbacks(new VolumeFaderEndedEvent {Canceled = true, Took = (int)_stopwatch.ElapsedMilliseconds});
                    return;
                }

                if (Audio.Volume < fadeRate)
                {
                    CallVolumeFaderEndedCallbacks(new VolumeFaderEndedEvent {Completed = true, Took =  (int)_stopwatch.ElapsedMilliseconds});
                    return;
                }

                Audio.Volume = (float)(Audio.Volume - fadeRate);
                var ticksLeft = Audio.Volume / fadeRate;
                var sLeft = fadeRate * ticksLeft;

                Debug.WriteLine($"Decreasing volume by {fadeRate}");
                Debug.WriteLine($"Volume left: {sLeft}. Ticks left: {ticksLeft}");

                var tLeft = ticksLeft * offsetTickWaitMs / 1000;
                Debug.WriteLine($"Time left: {tLeft} s");

                Debug.WriteLine($"Waiting {offsetTickWaitMs} ms before next tick.");
                Thread.Sleep(offsetTickWaitMs);
            }
        }
        private void WaitBeforeFade(double minutes)
        {
            Console.WriteLine($"Waiting {minutes} minutes before starting Fade-effect");
            var waitMs = (int)(minutes * 1000 * 60);

            //Sleep
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;
            do
            {
                if (State.StopRequested) return;

                var taskWasCanceled = token.WaitHandle.WaitOne(waitMs);

                if (taskWasCanceled)
                    return;

                tokenSource.Cancel();

            } while (token.IsCancellationRequested == false);
        }
    }

    public class VolumeFaderEvent {}
    public class VolumeFaderEndedEvent : VolumeFaderEvent
    {
        public bool Completed { get; set; }
        public bool Canceled { get; set; }
        public int Took { get; set; }
    }

    public struct State
    {
        public bool IsFading;
        public bool StopRequested;
    }
}
