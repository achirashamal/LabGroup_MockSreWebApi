using System;
using System.Threading;

namespace MockSreWebApi
{
    public static class BackgroundWorker
    {
        private static Timer _timer;
        public static void Start()
        {
            _timer = new Timer(Execute, null, 0, 10000);
        }

        private static void Execute(object state)
        {
            try
            {
                Console.WriteLine($"Background worker executed at {DateTime.Now}");
                if (new Random().NextDouble() < 0.1)
                    throw new Exception("Simulated background failure.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Background worker error: {ex.Message}");
            }
        }
    }
}
