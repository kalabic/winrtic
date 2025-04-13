using Timer = System.Timers.Timer;

namespace MiniRTIC;

/// <summary>
/// A wrapper around <see cref="Console.Write"/> to make text on Windows command line look more
/// like a dialog between an user and an agent.
/// </summary>
public class MiniConsole
{
    private bool _sessionStarted = false;

    private bool _receivingItem = true;

    private Timer _timer = new();

    private string _bufferedText = "";

    private Func<long> _provideBufferedAudioMs;

    private CancellationToken _cancellationToken;

    public MiniConsole(Func<long> provideBufferedAudioMs, CancellationToken cancellationToken)
    {
        this._provideBufferedAudioMs = provideBufferedAudioMs;
        this._cancellationToken = cancellationToken;
        _timer = new();
        _timer.Interval = 500;
        _timer.Elapsed += OnTimedEvent;
        _timer.AutoReset = true;
        _timer.Enabled = true;
        _timer.Start();
    }

    private bool AssertNotCancelled()
    {
        if (_cancellationToken.IsCancellationRequested)
        {
            EndSession();
            return false;
        }
        return true;
    }

    public void UpdateTitleWithBufferedAudioLength(long playbackMs)
    {
        var agentText = GetShortTimeString(playbackMs);
        var titleText = "Audio buffered:[" + agentText + "]";
        Console.Title = titleText;
    }

    private string GetShortTimeString(long timeMs)
    {
        int playbackMin = (int)(timeMs / (60 * 1000));
        int playbackSec = (int)((timeMs / 1000) % 60);
        int playbackDec = (int)((timeMs % 1000) / 100);
        string mm = playbackMin.ToString("00");
        string ss = playbackSec.ToString("00");
        string d = playbackDec.ToString("0");
        return mm + ":" + ss + "." + d;
    }

    public void Write(string text)
    {
        if (AssertNotCancelled() && _sessionStarted)
        {
            if (_receivingItem)
            {
                Console.Write(text);
            }
            else
            {
                _bufferedText += text;
            }
        }
    }

    public void WriteTranscript(string text)
    {
        if (AssertNotCancelled() && _sessionStarted)
        {
            if (_receivingItem)
            {
                Console.Write(" [ Interruption ]");
                SetStateWaitingItem();
            }
            Console.WriteLine(MiniConsolePrompt.UserPrompt + text);
            SetStateReceivingItem();
        }
    }

    public void StartSession()
    {
        _sessionStarted = true;
        Console.WriteLine("");
        Console.WriteLine(" * Session started (Ctrl-C to finish)");
        SetStateWaitingItem();
    }

    public void EndSession()
    {
        if (_sessionStarted)
        {
            _sessionStarted = false;

            if (_receivingItem)
            {
                _receivingItem = false;
                _timer.Start();
                Console.WriteLine("");
                Console.WriteLine("");
            }
            else
            {
                _timer.Stop();
            }

            Console.WriteLine("SESSION ENDED");
        }
    }

    public void SetStateReceivingItem()
    {
        if (AssertNotCancelled() && !_receivingItem)
        {
            _receivingItem = true;
            // _timer.Stop();
            Console.Write(MiniConsolePrompt.AgentPrompt + _bufferedText);
            _bufferedText = "";
        }
    }

    public void SetStateWaitingItem()
    {
        if (AssertNotCancelled() && _receivingItem)
        {
            _receivingItem = false;
            _timer.Start();
            Console.WriteLine("");
            Console.WriteLine("");
        }
    }

    private void OnTimedEvent(Object? source, System.Timers.ElapsedEventArgs e)
    {
        if (AssertNotCancelled())
        {
            if (_sessionStarted)
            {
                if (!_receivingItem)
                {
                    Console.Write(MiniConsolePrompt.GetProgressPrompt());
                }
                UpdateTitleWithBufferedAudioLength(_provideBufferedAudioMs());
            }
            else
            {
                Console.Write(". ");
            }
        }
    }

    public class MiniConsolePrompt
    {
        static private int _step = 0;
        static private string[] _waiting = { "[ . ]", "[   ]" };

        static public string GetProgressPrompt()
        {
            _step++;
            var wl = _waiting[_step % 2];
            string result = "\r" + wl + "\r";
            return result;
        }

        public const string AgentPrompt = " AGENT: ";

        public const string UserPrompt = "  USER: ";
    }
}
