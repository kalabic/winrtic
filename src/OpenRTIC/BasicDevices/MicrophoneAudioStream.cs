using NAudio.Wave;

namespace OpenRTIC.BasicDevices;

public class MicrophoneAudioStream : CircularBufferStreamBase
{
    // For simplicity, this is configured to use a static 5-second ring buffer.
    public const int BUFFER_SECONDS = 5;

    private WaveInEvent _waveInEvent;

    EventHandler<WaveInEventArgs> handleDataAvailable;

    public MicrophoneAudioStream(AudioStreamFormat audioFormat, CancellationToken microphoneToken)
        : base(audioFormat.BufferSizeFromSeconds(5), microphoneToken)
    {
        _waveInEvent = new()
        {
            WaveFormat = new WaveFormat(audioFormat.SamplesPerSecond, audioFormat.BitsPerSample, audioFormat.ChannelCount)
        };
        handleDataAvailable = (_, e) =>
        {
            Write(e.Buffer, 0, e.BytesRecorded);
        };
        _waveInEvent.DataAvailable += handleDataAvailable;
        _waveInEvent.StartRecording();
    }

    protected override void Dispose(bool disposing)
    {
        // Release managed resources.
        if (disposing && (_waveInEvent is not null))
        {
            _waveInEvent.DataAvailable -= handleDataAvailable;
        }

        // Release unmanaged resources.
        _waveInEvent?.Dispose();
        base.Dispose(disposing);
    }

    public override void Close()
    {
        _waveInEvent.StopRecording();
        _waveInEvent.DataAvailable -= handleDataAvailable;
        base.Close();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        // For simplicity, block until all requested data is available and do not perform partial reads.
        if (!WaitDataAvailable(count))
        {
            return 0;
        }

        return base.Read(buffer, offset, count);
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;
}
