using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace RateLimiterTest
{
    /// <summary>
    ///     Ограничение запросов в единицу времени. Поддерживает несколько ограничений
    ///     Пример - не более 10 в секунду и не более 100 в минуту.
    ///     Точность - миллисекунды
    /// </summary>
    public class RateLimiter
    {
        private readonly RateLimiterOnce[] _limiters;
        private long ElapsedMilliseconds { get; set; }
        private readonly Stopwatch _stopwatch;
        private readonly object _waitLock = new object();

        public RateLimiter(params (TimeSpan interval, int count)[] restrictions)
        {
            ValidateArguments(restrictions);

            _stopwatch = Stopwatch.StartNew();
            _limiters = restrictions
                .Select(r => new RateLimiterOnce(r.interval, r.count, this))
                .ToArray();
        }

        public Task WaitAsync()
        {
            Task waitTask;
            lock (_waitLock)
            {
                ElapsedMilliseconds = _stopwatch.ElapsedMilliseconds;
                waitTask = Task.WhenAll(_limiters.Select(l => l.WaitAsync()));
            }

            return waitTask;
        }

        private static void ValidateArguments(IReadOnlyCollection<(TimeSpan interval, int count)> restrictions)
        {
            if (restrictions.Count == 0)
            {
                throw new ArgumentException("No restrictions added");
            }

            foreach (var (interval, count) in restrictions)
            {
                if (count < 0)
                {
                    throw new ArgumentException($"Count for interval {interval} is negative: {count}");
                }
            }
        }

        /// <summary>
        ///     Ограничитель зарпросов для 1 интервала
        /// </summary>
        private class RateLimiterOnce
        {
            private readonly long[] _lastRequests;
            private readonly TimeSpan _interval;
            private readonly RateLimiter _parent;
            private readonly int _count;
            private int _lastIndex;

            public RateLimiterOnce(TimeSpan interval, int count, RateLimiter parent)
            {
                _interval = interval;
                _parent = parent;
                _lastRequests = new long[count];
                for (var i = 0; i < count; ++i)
                {
                    _lastRequests[i] = long.MinValue;
                }
                _count = count;
            }

            public Task WaitAsync()
            {
                var elapsed = _parent.ElapsedMilliseconds;
                _lastRequests[_lastIndex] = elapsed;
                _lastIndex = (_lastIndex + 1) % _count;
                var delta = elapsed - _lastRequests[_lastIndex];
                var waitTimeMilliseconds = (int)(_interval.TotalMilliseconds - delta);
                if (waitTimeMilliseconds <= 0)
                {
                    return Task.CompletedTask;
                }

                return Task.Delay(waitTimeMilliseconds);
            }
        }
    }
}