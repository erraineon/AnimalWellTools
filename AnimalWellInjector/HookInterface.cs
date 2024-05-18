using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace AnimalWellInjector
{
    public class HookInterface : MarshalByRefObject
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Dictionary<string, DateTime> _recentMessages = new Dictionary<string, DateTime>();
        public bool IsStoppingRequested => _cts.Token.IsCancellationRequested;

        public void Ping()
        {
        }

        public void ReportMessage(string message)
        {
            var bufferTime = TimeSpan.FromSeconds(1);
            var now = DateTime.Now;
            // Prevent the same message from being spammed
            if (!_recentMessages.TryGetValue(message, out var lastSeenAt) || lastSeenAt + bufferTime < now)
            {
                _recentMessages[message] = now;
                Console.WriteLine(message);
                if (_recentMessages.Count > 16)
                    _recentMessages.Remove(_recentMessages.OrderBy(x => x.Value).First().Key);
            }
        }

        public void Stop()
        {
            _cts.Cancel();
        }
    }
}