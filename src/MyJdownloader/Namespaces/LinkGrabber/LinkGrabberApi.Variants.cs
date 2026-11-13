namespace MyJdownloader.LinkGrabber;

using System.Text.Json;
using MyJdownloader.Facades;

public sealed partial class LinkGrabberApi
{
    /// <summary>Lists the variants of a link (<c>/linkgrabberv2/getVariants</c>).</summary>
    /// <param name="linkId">The identifier of the link.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>The variants of the link; empty when the device answers nothing.</returns>
    /// <remarks>Variant names and identifiers are untrusted device data.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="linkId"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out.</exception>
    /// <exception cref="HttpRequestException">A network error occurred.</exception>
    public Task<IReadOnlyList<LinkVariant>> GetVariantsAsync(long linkId, CancellationToken cancellationToken = default)
    {
        FacadeGuard.ThrowIfNegative(linkId, nameof(linkId));
        var args = new[] { JsonSerializer.SerializeToElement(linkId, FacadeJsonContext.Default.Int64) };
        return FacadeResults.ListAsync(_channel.CallAsync(Base + "getVariants", LinkGrabberJsonContext.Default.ListLinkVariant, args, cancellationToken));
    }

    /// <summary>Selects the variant of a link (<c>/linkgrabberv2/setVariant</c>).</summary>
    /// <param name="linkId">The identifier of the link.</param>
    /// <param name="variantId">The identifier of the variant, as returned by <see cref="GetVariantsAsync"/>.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <remarks>Applied without confirmation. Variant identifiers are device data.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="variantId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="variantId"/> is blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters or lone surrogates.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="linkId"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task SetVariantAsync(long linkId, string variantId, CancellationToken cancellationToken = default)
    {
        FacadeGuard.ThrowIfNegative(linkId, nameof(linkId));
        FacadeGuard.ValidateText(variantId, nameof(variantId), FacadeGuard.MaxIdentifierLength);
        var args = new[]
        {
            JsonSerializer.SerializeToElement(linkId, FacadeJsonContext.Default.Int64),
            JsonSerializer.SerializeToElement(variantId, FacadeJsonContext.Default.String),
        };
        return _channel.CallAsync(Base + "setVariant", args, cancellationToken);
    }

    /// <summary>Adds a copy of a link with another variant (<c>/linkgrabberv2/addVariantCopy</c>).</summary>
    /// <param name="linkId">The identifier of the link to copy.</param>
    /// <param name="afterLinkId">The identifier of the link after which the copy is placed.</param>
    /// <param name="destinationPackageId">The identifier of the destination package.</param>
    /// <param name="variantId">The identifier of the variant, as returned by <see cref="GetVariantsAsync"/>.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the device has answered.</returns>
    /// <remarks>Applied without confirmation. Variant identifiers are device data.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="variantId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="variantId"/> is blank, longer than its allowed length (see Argument validation in the error handling guide), or contains control characters or lone surrogates.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="linkId"/>, <paramref name="afterLinkId"/> or <paramref name="destinationPackageId"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The client is not connected.</exception>
    /// <exception cref="MyJdException">The relay or the device rejected the request, or the response is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
    /// <exception cref="TimeoutException">The request timed out; the action may have been executed.</exception>
    /// <exception cref="HttpRequestException">A network error occurred; the action may have been executed.</exception>
    public Task AddVariantCopyAsync(long linkId, long afterLinkId, long destinationPackageId, string variantId, CancellationToken cancellationToken = default)
    {
        FacadeGuard.ThrowIfNegative(linkId, nameof(linkId));
        FacadeGuard.ThrowIfNegative(afterLinkId, nameof(afterLinkId));
        FacadeGuard.ThrowIfNegative(destinationPackageId, nameof(destinationPackageId));
        FacadeGuard.ValidateText(variantId, nameof(variantId), FacadeGuard.MaxIdentifierLength);
        var args = new[]
        {
            JsonSerializer.SerializeToElement(linkId, FacadeJsonContext.Default.Int64),
            JsonSerializer.SerializeToElement(afterLinkId, FacadeJsonContext.Default.Int64),
            JsonSerializer.SerializeToElement(destinationPackageId, FacadeJsonContext.Default.Int64),
            JsonSerializer.SerializeToElement(variantId, FacadeJsonContext.Default.String),
        };
        return _channel.CallAsync(Base + "addVariantCopy", args, cancellationToken);
    }
}
