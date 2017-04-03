using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using ZeroVolumeFader.CommandLine.Private;

namespace ZeroVolumeFader.CommandLine
{
    class Program
    {
        private static readonly ZeroVolumeFader ZeroVolumeFader = new ZeroVolumeFader();

        //static void Main(string[] args)
        //{
        //    //Main2(new[] {"-w", "0.1", "-f", "0.5", "-z", "sleep"});
        //    Main2(Console.ReadLine().Split(' '));
        //}

        static void Main(string[] args)
        {
            if (args.Length == 1 && args[0] == "stop")
                ZeroVolumeFader.Stop();

            var options = GetOptions(args);
            ZeroVolumeFader.OnVolumeFaderWaitFinished += async (sender, @event) => await IoTService.TurnOffLightsAsync();
            ZeroVolumeFader.OnVolumeFaderEnded += async (sender, @event) =>
            {
                if (!@event.Completed) return;

                await IoTService.TurnOffSpeakersAsync();

                switch (options.ZeroVolumeAction)
                {
                    case "sleep":
                        SleepComputer();
                        break;
                    case "shutdown":
                        ShutdownComputer();
                        break;
                }
            };

            ZeroVolumeFader.Run(options.WaitTimeMinutes, options.FadeTimeMinutes);
        }


        private static Options GetOptions(string[] args)
        {
            var argsList = args.ToList();
            var options = new Options
            {
                WaitTimeMinutes = GetOption<double>("-w", argsList),
                FadeTimeMinutes = GetOption<double>("-f", argsList),
                TurnOffLightsAfterWait = GetOption<bool>("-l", argsList)
            };

            try
            {
                var zeroVolumeAction = GetOption<string>("-z", argsList);
                options.ZeroVolumeAction = zeroVolumeAction;
            }
            catch
            {
                // ignored
            }

            return options;
        }

        private static T GetOption<T>(string option, List<string> args)
        {
            try
            {
                var key = args.IndexOf(option);
                if (key == -1)
                {
                    throw new IndexOutOfRangeException();
                }
                var value = args[key + 1];

                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [DllImport("Powrprof.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern bool SetSuspendState(bool hiberate, bool forceCritical, bool disableWakeEvent);
        private static void SleepComputer() => SetSuspendState(false, true, false);
        private static void ShutdownComputer() => System.Diagnostics.Process.Start("shutdown", "/s /t 0");
        private struct Options
        {
            public double WaitTimeMinutes;
            public double FadeTimeMinutes;
            public string ZeroVolumeAction;
            public bool TurnOffLightsAfterWait;
        }
    }
}
