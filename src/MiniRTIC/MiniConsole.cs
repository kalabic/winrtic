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

    public MiniConsole()
    {
        _timer = new();
        _timer.Interval = 500;
        _timer.Elapsed += OnTimedEvent;
        _timer.AutoReset = true;
        _timer.Enabled = true;
        _timer.Start();
    }

    public void Write(string text)
    {
        if (_sessionStarted)
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
        if (_sessionStarted)
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
        if (!_receivingItem)
        {
            _receivingItem = true;
            _timer.Stop();
            Console.Write(MiniConsolePrompt.AgentPrompt + _bufferedText);
            _bufferedText = "";
        }
    }

    public void SetStateWaitingItem()
    {
        if (_receivingItem)
        {
            _receivingItem = false;
            _timer.Start();
            Console.WriteLine("");
            Console.WriteLine("");
        }
    }

    private void OnTimedEvent(Object? source, System.Timers.ElapsedEventArgs e)
    {
        if (_sessionStarted)
        {
            if (!_receivingItem)
            {
                Console.Write(MiniConsolePrompt.GetProgressPrompt());
            }
        }
        else
        {
            Console.Write(". ");
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
