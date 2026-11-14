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
    private int _written;

    /// <summary>Gets the bytes written so far.</summary>
    public ReadOnlySpan<byte> WrittenSpan => _buffer.AsSpan(0, _written);

    /// <summary>Gets the bytes written so far, as memory.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public ReadOnlyMemory<byte> WrittenMemory => _buffer.AsMemory(0, _written);

    public void Advance(int count) => _written += count;

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        Ensure(sizeHint);
        return _buffer.AsMemory(_written);
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        Ensure(sizeHint);
        return _buffer.AsSpan(_written);
    }

    public void Dispose()
    {
        CryptographicOperations.ZeroMemory(_buffer);
        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = [];
    }

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
