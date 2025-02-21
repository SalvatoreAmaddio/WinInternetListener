using System.Runtime.InteropServices;

namespace WinInternetListener
{
    public class InternetListener : IDisposable
    {
        [DllImport("wininet.dll")]
        private static extern bool InternetGetConnectedState(out int description, int reservedValue);
        private static readonly Lazy<InternetListener> _instance = new(() => new());
        public static InternetListener Instance => _instance.Value;

        private volatile bool _isConnected = true;
        public bool IsConnected => _isConnected;
        public int CheckEvery { get; set; } = 2000;
        public event EventHandler? ConnectionLost;
        public event EventHandler? ConnectionRestored;
        private CancellationTokenSource? _cts;

        private InternetListener()
        {

        }

        public void Listen()
        {
            if (Interlocked.CompareExchange(ref _cts, new CancellationTokenSource(), null) != null)
                return;

            _cts = new CancellationTokenSource();
            Task.Run(() => ListenAsync(_cts.Token), _cts.Token);
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts = null;
        }

        private async Task ListenAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    bool temp = InternetGetConnectedState(out _, 0);

                    if (temp != IsConnected)
                    {
                        _isConnected = temp;

                        if (_isConnected)
{                            ConnectionRestored?.Invoke(this, EventArgs.Empty);
}                        else
                            ConnectionLost?.Invoke(this, EventArgs.Empty);
                    }

                    await Task.Delay(CheckEvery, token);
                }
            }
            catch (TaskCanceledException)
            {
                // Task was canceled gracefully
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error in ListenAsync: {ex.Message}");
            }
        }
    
        public void Dispose()
        {
            Stop();
            ConnectionLost = null;
            ConnectionRestored = null;
            GC.SuppressFinalize(this);
        }
    }
}