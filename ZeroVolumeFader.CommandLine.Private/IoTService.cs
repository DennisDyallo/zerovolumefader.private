using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace ZeroVolumeFader.CommandLine.Private
{
    public static class IoTService
    {
        private const string ServerUrl = "http://192.168.0.15:8000/";
        public static async Task TurnOffSpeakersAsync()
        {
            using (var client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("toMainPage", "setOffACCF2399591C")
                });

                var result = await client.PostAsync(ServerUrl, content);
                var resultContent = await result.Content.ReadAsStringAsync();
                Console.WriteLine(resultContent);
            }
        }

        public static async Task TurnOffLightsAsync()
        {
            using (var client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("toMainPage", "setOffACCF2399582A")
                });

                var result = await client.PostAsync(ServerUrl, content);
                var resultContent = await result.Content.ReadAsStringAsync();
                Console.WriteLine(resultContent);
            }
        }
    }
}
