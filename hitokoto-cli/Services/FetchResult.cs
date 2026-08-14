namespace hitokoto_cli.Services;

/// <summary>Why a fetch failed. Carried by <see cref="FetchResult{T}"/> so the
/// caller can tell a timeout apart from an HTTP/network/parse failure instead
/// of receiving a bare null.</summary>
internal enum FetchFailureKind
{
    Timeout,
    Http,
    Network,
    Parse,
}

/// <summary>
/// Result of a fetch call: either a parsed value (<see cref="Ok"/>) or a
/// failure carrying its kind and a pre-rendered user-facing message. The
/// client returns results and never writes to stderr itself — reporting is
/// the caller's job (via Diagnostics).
/// </summary>
internal sealed record FetchResult<T>
{
    private FetchResult(T? value, FetchFailureKind? failure, string message)
    {
        Value = value;
        Failure = failure;
        Message = message;
    }

    public T? Value { get; }

    public FetchFailureKind? Failure { get; }

    /// <summary>User-facing failure line (empty on success).</summary>
    public string Message { get; }

    public bool IsSuccess => Failure is null;

    public static FetchResult<T> Ok(T value) => new(value, null, string.Empty);

    public static FetchResult<T> Fail(FetchFailureKind failure, string message)
        => new(default, failure, message);
}
