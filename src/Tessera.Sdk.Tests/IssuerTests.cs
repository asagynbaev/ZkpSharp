using Tessera.Attestations;
using Tessera.Core;
using Tessera.Sdk;
using Tessera.Signing;

namespace Tessera.Sdk.Tests;

public class IssuerTests
{
    [Fact]
    public void Issue_ProducesAttestationBoundToIssuerAndSubject()
    {
        var (priv, _) = Ed25519.GenerateKeypair();
        using var signer = new Ed25519IssuerSigner(priv);
        var issuer = new Issuer(new DidId("did:tessera:my-issuer"), signer);

        var subject = new DidId("did:tessera:subject-1");
        var att = issuer.Issue(
            type: "phone_verified",
            subject: subject,
            payload: new AttestationPayload { Method = "test_provider" });

        Assert.Equal(new DidId("did:tessera:my-issuer"), att.Issuer);
        Assert.Equal(subject, att.Subject);
        Assert.Equal("phone_verified", att.Type);
        Assert.Equal("ed25519", att.Signature.Algorithm);
        Assert.Equal(Ed25519.SignatureSize, att.Signature.Value.Length);
    }

    [Fact]
    public void BuildRegistryRecord_ShapesIssuerRecord()
    {
        var (priv, pub) = Ed25519.GenerateKeypair();
        using var signer = new Ed25519IssuerSigner(priv);
        var issuer = new Issuer(new DidId("did:tessera:another-issuer"), signer);

        var record = issuer.BuildRegistryRecord(schemaUri: "https://example.com/schema/v3");

        Assert.Equal(issuer.Did, record.Did);
        Assert.Equal(pub, record.PublicKey);
        Assert.Equal("ed25519", record.Algorithm);
        Assert.Equal("https://example.com/schema/v3", record.SchemaUri);
        Assert.True(record.Active);
    }

    [Fact]
    public void BuildRegistryRecord_CanMarkInactive()
    {
        var (priv, _) = Ed25519.GenerateKeypair();
        using var signer = new Ed25519IssuerSigner(priv);
        var issuer = new Issuer(new DidId("did:tessera:revoked-issuer"), signer);

        var record = issuer.BuildRegistryRecord(schemaUri: "x", active: false);
        Assert.False(record.Active);
    }

    [Fact]
    public void Issue_OverrideSchema_UsesProvided()
    {
        var (priv, _) = Ed25519.GenerateKeypair();
        using var signer = new Ed25519IssuerSigner(priv);
        var issuer = new Issuer(new DidId("did:tessera:issuer"), signer);

        var att = issuer.Issue(
            type: "custom",
            subject: new DidId("did:tessera:s"),
            payload: new AttestationPayload { Method = "x" },
            schema: "https://custom.example/schema/v2");

        Assert.Equal("https://custom.example/schema/v2", att.Schema);
    }
}
