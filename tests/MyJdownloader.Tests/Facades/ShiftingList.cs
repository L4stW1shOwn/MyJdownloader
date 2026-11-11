namespace MyJdownloader.Tests.Facades;

using System.Collections;

/// <summary>A one-element list whose first enumeration yields 1 and every later one yields -1, to prove a facade sends the copy it checked.</summary>
internal sealed class ShiftingList : IReadOnlyList<long>
{
    private int _enumerations;

    public int Count => 1;

    public long this[int index] => index == 0 ? this.Current() : throw new ArgumentOutOfRangeException(nameof(index));

    public IEnumerator<long> GetEnumerator()
    {
        yield return this.Current();
    }

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    private long Current() => this._enumerations++ == 0 ? 1 : -1;
}
