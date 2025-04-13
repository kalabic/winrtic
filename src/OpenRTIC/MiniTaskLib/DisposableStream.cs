namespace OpenRTIC.MiniTaskLib;

/// <summary>
/// This class is a literal copy/paste of IDisaposable implementation from Stream class.
/// </summary>
public abstract class DisposableStream : IDisposable
{
    public void Dispose() => Close();

    public virtual void Close()
    {
        // When initially designed, Stream required that all cleanup logic went into Close(),
        // but this was thought up before IDisposable was added and never revisited. All subclasses
        // should put their cleanup now in Dispose(bool).
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        // Note: Never change this to call other virtual methods on Stream
        // like Write, since the state on subclasses has already been
        // torn down.  This is the last code to run on cleanup for a stream.
    }

    public virtual ValueTask DisposeAsync()
    {
        try
        {
            Dispose();
            return default;
        }
        catch (Exception exc)
        {
            return ValueTask.FromException(exc);
        }
    }

    public abstract void Flush();

    public Task FlushAsync() => FlushAsync(CancellationToken.None);

    public virtual Task FlushAsync(CancellationToken cancellationToken) =>
        Task.Factory.StartNew(
            static state => ((Stream)state!).Flush(), this,
            cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
}
