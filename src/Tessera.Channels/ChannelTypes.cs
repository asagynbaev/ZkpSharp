namespace Tessera.Channels;

/// <summary>
/// Canonical channel type identifiers. Channel types act as a domain separator inside
/// the HKDF context — picking a different identifier for the same logical channel
/// would break compatibility with previously stored commitments, so treat these as
/// part of the on-disk format.
/// </summary>
public static class ChannelTypes
{
    public const string Phone = "phone";
    public const string Email = "email";
    public const string Telegram = "telegram";
}
