# ZkpSharp

Privacy-preserving identity and reputation infrastructure for .NET. DIDs, signed
attestations, selective disclosure via Merkle bundles, and Bulletproof-based
predicate proofs over committed values. Multi-chain anchoring with Solana as the
primary target and Stellar as a secondary backend.

[![NuGet](https://img.shields.io/nuget/v/ZkpSharp)](https://www.nuget.org/packages/ZkpSharp)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ZkpSharp)](https://www.nuget.org/packages/ZkpSharp)
[![Build](https://github.com/asagynbaev/ZkpSharp/actions/workflows/dotnet.yml/badge.svg)](https://github.com/asagynbaev/ZkpSharp/actions/workflows/dotnet.yml)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

## What this is for

- Binding humans to decentralized identifiers (`did:zkp:...`).
- Issuing and verifying generic attestations: humanity, phone, wallet control,
  region, reputation score, agent identity.
- Producing presentations a holder can hand to a verifier — Merkle inclusion plus
  selective predicate proofs over committed values.
- Anchoring attestation roots and revocation epochs on-chain without writing any
  identity data on-chain.

## What this is not for

- Not a zkVM, not a proving network.
- Not a token, governance, or DAO toolkit.
- Not a prediction-market or DeFi library.
- Not a research-only cryptography experiment.

## Repository layout

```
ZkpSharp/
├── src/
│   ├── ZkpSharp.Core/                 DidId, Base58 — cross-cutting types
│   ├── ZkpSharp.Did/                  DidDocument, DidService, IDidStore
│   ├── ZkpSharp.Attestations/         Issuer, MerkleTree, AttestationVerifier,
│   │                                  PresentationVerifier, IIssuerRegistry
│   └── ZkpSharp.Chains.Abstractions/  IChainAnchor — chain-agnostic interface
│
├── chains/
│   ├── solana/                        Anchor IdentityRegistry program (primary)
│   └── stellar/                       Soroban attestation-verifier (secondary)
│
├── ZkpSharp/                          v2 monolith (NuGet 2.x); v3 will split it
├── examples/PrivacyApps/              ConfidentialTransfer, SealedBidAuction, PrivateVoting
└── docs/architecture.md
```

See [docs/architecture.md](docs/architecture.md) for the full layout, package
dependency rules, and the on-chain / off-chain boundary.

## Quick start

### Create a DID

```csharp
using ZkpSharp.Did;

var store = new InMemoryDidStore();
var verifier = new Ed25519SignatureVerifier(YourEd25519VerifyImpl);
var service = new DidService(store, verifier);

// controllerPublicKey is the 32-byte Ed25519 key you control
var doc = await service.CreateAsync(controllerPublicKey);
// doc.Id == "did:zkp:<base58(sha256(pubkey||\"v1\"))>"
```

### Bind a wallet

```csharp
var challenge = DidService.BuildWalletChallenge(doc.Id, new WalletBindingRequest {
    Chain    = "solana",
    Address  = walletAddress,
    WalletPublicKey = walletPubkey,
    Nonce    = RandomNonce(),
    Expiry   = DateTimeOffset.UtcNow.AddMinutes(5),
    Signature = Array.Empty<byte>(),
});
var sig = SignWithWallet(challenge);                   // your wallet signs the canonical challenge
await service.BindWalletAsync(doc.Id, request with { Signature = sig });
```

### Issue and verify an attestation

```csharp
using ZkpSharp.Attestations;

var signer = new YourEd25519IssuerSigner(issuerPrivateKey);
var issuer = new AttestationIssuer(issuerDid, signer);

var att = issuer.Issue(
    AttestationTypes.HumanVerified,
    subject:  subjectDid,
    payload:  new AttestationPayload { Method = "humanity_check_v2" },
    validity: TimeSpan.FromDays(365));

// Verifier side
var registry = new InMemoryIssuerRegistry();           // or a chain-backed registry
registry.Register(new IssuerRecord {
    Did = issuerDid, PublicKey = issuerPubkey,
    Algorithm = "ed25519", SchemaUri = att.Schema, Active = true,
});
var attVerifier = new AttestationVerifier(registry, sigVerifier);
var result = await attVerifier.VerifyAsync(att);       // result.Valid == true
```

### Bundle into a Merkle tree and present selectively

```csharp
var bundle = new AttestationBundle(new[] { att1, att2, att3 });
// Anchor bundle.Root on chain via IChainAnchor

// Verifier later asks for just one attestation
var disclosure = bundle.DisclosureFor(0);
var presentation = new Presentation {
    Holder       = subjectDid,
    Disclosures  = new[] { disclosure },
    Binding      = new PresentationBinding { /* ... session, chain, signature ... */ },
};
var presentationVerifier = new PresentationVerifier(attVerifier);
var verified = await presentationVerifier.VerifyAsync(presentation, expectedRootFromChain);
```

### Predicate proof over a committed value

For attestations that carry a Pedersen commitment, the holder can prove a
predicate (e.g. `score ≥ 700`) without revealing the score. This uses the
existing Bulletproofs implementation:

```csharp
using ZkpSharp.Privacy;

var cp = new CredentialProof();
var bundle = cp.ProveMinimum(actualValue: 85_000, minimumRequired: 50_000, label: "annual_income");
bool valid = cp.Verify(bundle);
```

## Chains

- **Solana (primary)** — `chains/solana/programs/identity-registry/`. A minimal Anchor
  program: `register_did`, `update_root`, `bump_revocation`, `register_issuer`. No
  proof verification on-chain; verification stays in C#.
- **Stellar (secondary)** — `chains/stellar/contracts/attestation-verifier/`. The
  Soroban contract previously known as `proof-balance`. Kept as a working secondary
  anchor; will be ported to the same `IChainAnchor` shape in v3.

## Migration to v3

The package is being split. v2.x consumers continue to work; v3 is a breaking cut.

| v2 type | v3 replacement |
|---|---|
| `ZkpSharp.Core.Zkp` (HMAC equality) | Removed. Use `CredentialProof` for ZK predicates. |
| `ZkpSharp.Interfaces.IBlockchain` | `ZkpSharp.Chains.IChainAnchor`. |
| `ZkpSharp.Integration.Stellar.*` | `ZkpSharp.Chains.Stellar` (TODO). |
| `ZkpSharp.Crypto.*` | `ZkpSharp.Cryptography` (TODO). |
| `ZkpSharp.Privacy.CredentialProof` | `ZkpSharp.Attestations` (TODO). |

## License

MIT.
