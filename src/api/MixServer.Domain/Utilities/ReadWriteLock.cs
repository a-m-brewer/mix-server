namespace MixServer.Domain.Utilities;

/// <summary>
/// Uses a <see cref="ReaderWriterLockSlim"/> to invoke code.
/// </summary>
public interface IReadWriteLock : IDisposable
{
    /// <summary>
    /// Runs lockable code whose lock can be shared
    /// </summary>
    /// <typeparam name="T">the type of the return value</typeparam>
    /// <param name="action">the code to invoke</param>
    /// <returns>The result of the action</returns>
    void ForRead(Action action);
    
    /// <summary>
    /// Runs lockable code whose lock can be shared
    /// </summary>
    /// <typeparam name="T">the type of the return value</typeparam>
    /// <param name="action">the code to invoke</param>
    /// <returns>The result of the action</returns>
    T ForRead<T>(Func<T> action);

    /// <summary>
    /// Runs lockable code whose lock can initially be shared
    /// with the option to upgrade to an exclusive lock
    /// </summary>
    /// <typeparam name="T">the type of the return value</typeparam>
    /// <param name="action">the code to invoke</param>
    /// <returns>The result of the action</returns>
    T ForUpgradeableRead<T>(Func<T> action);

    /// <summary>
    /// Runs lockable code whose lock can initially be shared
    /// with the option to upgrade to an exclusive lock
    /// </summary>
    /// <param name="action">the code to invoke</param>
    /// <returns>The result of the action</returns>
    void ForUpgradeableRead(Action action);
    
    /// <summary>
    /// Runs lockable code whose lock must be exclusive
    /// </summary>
    /// <param name="action">the code to invoke</param>
    /// <returns>The result of the action</returns>
    void ForWrite(Action action);

    /// <summary>
    /// Runs lockable code whose lock must be exclusive
    /// </summary>
    /// <typeparam name="T">the type of the return value</typeparam>
    /// <param name="action">the code to invoke</param>
    /// <returns>The result of the action</returns>
    T ForWrite<T>(Func<T> action);
}

public class ReadWriteLock : IReadWriteLock
{
    private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

    public void ForRead(Action action)
    {
        _lock.EnterReadLock();

        try
        {
            action.Invoke();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public T ForRead<T>(Func<T> action)
    {
        _lock.EnterReadLock();

        try
        {
            return action.Invoke();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public T ForUpgradeableRead<T>(Func<T> action)
    {
        _lock.EnterUpgradeableReadLock();

        try
        {
            return action.Invoke();
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }
    }

    public void ForUpgradeableRead(Action action)
    {
        _lock.EnterUpgradeableReadLock();

        try
        {
            action.Invoke();
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }
    }

    public void ForWrite(Action action)
    {
        _lock.EnterWriteLock();

        try
        {
            action.Invoke();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public T ForWrite<T>(Func<T> action)
    {
        _lock.EnterWriteLock();

        try
        {
            return action.Invoke();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Dispose()
    {
        _lock.Dispose();
    }
}