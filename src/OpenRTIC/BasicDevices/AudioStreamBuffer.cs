namespace OpenRTIC.BasicDevices;

public class AudioStreamBuffer : CircularBufferStreamBase
{
    // For simplicity, block until at least this much data is available.
    private const int WAIT_MIN_DATA_AVAILABLE = 100;

    private AudioStreamFormat _format;

    private int _minBufferSize;

    public AudioStreamBuffer(AudioStreamFormat audioFormat, int bufferSeconds, CancellationToken cancellation) 
        : base(audioFormat.BufferSizeFromSeconds(bufferSeconds), cancellation)
    {
        this._format = audioFormat;
        this._minBufferSize = _format.BufferSizeFromMiliseconds(WAIT_MIN_DATA_AVAILABLE);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int available = GetBytesAvailable(0);
        int minAsked = (_minBufferSize < count) ? _minBufferSize : count;
        if (available < minAsked)
        {
            if (!WaitDataAvailable(minAsked))
            {
                return 0;
            }
        }

        return base.Read(buffer, offset, count);
    }
}
