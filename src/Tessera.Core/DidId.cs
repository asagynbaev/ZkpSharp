namespace Tessera.Core;

/// <summary>
/// A decentralized identifier. Opaque string of the form <c>did:tessera:&lt;identifier&gt;</c>.
/// </summary>
public readonly record struct DidId(string Value)
{
    public const string MethodPrefix = "did:tessera:";

    public bool IsWellFormed =>
        !string.IsNullOrEmpty(Value)
        && Value.StartsWith(MethodPrefix, StringComparison.Ordinal)
        && Value.Length > MethodPrefix.Length;

    public override string ToString() => Value;

    public static DidId Parse(string value)
    {
        var did = new DidId(value);
        if (!did.IsWellFormed)
            throw new FormatException($"Not a well-formed did:tessera identifier: '{value}'.");
        return did;
    }
}
