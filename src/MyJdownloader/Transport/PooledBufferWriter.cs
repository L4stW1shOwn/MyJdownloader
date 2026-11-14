namespace MyJdownloader.Transport;

using System.Buffers;
using System.Diagnostics;
using System.Security.Cryptography;

/// <summary>Growable pooled buffer that clears every array it held when it grows or is disposed.</summary>
internal sealed class PooledBufferWriter : IBufferWriter<byte>, IDisposable
{
    // Holds the plaintext request, which may carry passwords passed as call arguments.
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private byte[] _buffer = ArrayPool<byte>.Shared.Rent(512);
    /// <summary>The number of bytes written so far.</summary>
    private int _written;

    /// <summary>Gets the bytes written so far.</summary>
    public ReadOnlySpan<byte> WrittenSpan => _buffer.AsSpan(0, _written);

    /// <summary>Gets the bytes written so far, as memory.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ReadOnlyMemory<byte> WrittenMemory => _buffer.AsMemory(0, _written);

    /// <inheritdoc/>
    public void Advance(int count) => _written += count;

    /// <inheritdoc/>
    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        Ensure(sizeHint);
        return _buffer.AsMemory(_written);
    }

    /// <inheritdoc/>
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        Ensure(sizeHint);
        return _buffer.AsSpan(_written);
    }

    /// <summary>Clears the buffer and returns it to the pool; the written bytes must not be used afterwards.</summary>
    public void Dispose()
    {
        CryptographicOperations.ZeroMemory(_buffer);
        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = [];
    }

    /// <summary>Grows the buffer when fewer than <paramref name="sizeHint"/> bytes are free, clearing the array it replaces.</summary>
    /// <param name="sizeHint">The minimum number of free bytes requested; at least one byte is guaranteed.</param>
    private void Ensure(int sizeHint)
    {
        var needed = Math.Max(sizeHint, 1);
        if (_buffer.Length - _written >= needed)
        {
            return;
        }

        var bigger = ArrayPool<byte>.Shared.Rent(Math.Max(_buffer.Length * 2, _written + needed));
        _buffer.AsSpan(0, _written).CopyTo(bigger);
        CryptographicOperations.ZeroMemory(_buffer);
        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = bigger;
    }
}
