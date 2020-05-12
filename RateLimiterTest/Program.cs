using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RateLimiterTest
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            /*
             * Тест - 100 раз делаем запрос с правилом "не более 2 раз в секунду и не более 5 раз в 5 секунд"
             */
            var limiter = new RateLimiter((TimeSpan.FromSeconds(1), 2), (TimeSpan.FromSeconds(5), 5));

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < 100; ++i)
            {
                Console.WriteLine(sw.ElapsedMilliseconds);
                await limiter.WaitAsync();
            }
        }
    }
}
