namespace Tessera.EntityFrameworkCore.Entities;

public sealed class VerificationMethodEntity
{
    public long DbId { get; set; }

    /// <summary>FK to <see cref="DidDocumentEntity.Id"/>.</summary>
    public string DidId { get; set; } = "";

    /// <summary>Verification method identifier, e.g. <c>did:tessera:abc#keys-1</c>.</summary>
    public string Id { get; set; } = "";

    /// <summary>Algorithm type, e.g. <c>Ed25519VerificationKey2020</c>.</summary>
    public string Type { get; set; } = "";

    public string PublicKeyMultibase { get; set; } = "";

    public DidDocumentEntity? Document { get; set; }
}
