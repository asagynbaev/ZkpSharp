# ZkpSharp

Privacy-preserving identity and reputation infrastructure for .NET. DIDs, signed
attestations, selective disclosure via Merkle bundles, Bulletproof-based predicate
proofs over committed values, and multi-chain anchoring (Solana primary, Stellar
secondary).

[![NuGet](https://img.shields.io/nuget/v/ZkpSharp)](https://www.nuget.org/packages/ZkpSharp)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ZkpSharp)](https://www.nuget.org/packages/ZkpSharp)
[![Build](https://github.com/asagynbaev/ZkpSharp/actions/workflows/dotnet.yml/badge.svg)](https://github.com/asagynbaev/ZkpSharp/actions/workflows/dotnet.yml)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

## What this is for

- Binding humans to decentralized identifiers (`did:zkp:...`).
- Issuing and verifying generic attestations — humanity, phone, wallet control,
  region, reputation score, agent identity.
- Producing presentations a holder can hand to a verifier: Merkle inclusion plus
  selective predicate proofs over committed values.
- Anchoring attestation roots and revocation epochs on-chain without writing any
  identity data on-chain.

## What this is not for

- Not a zkVM, not a proving network.
- Not a token, governance, or DAO toolkit.
- Not a prediction-market or DeFi library.
- Not a research-only cryptography experiment.

## Packages

| Package | Purpose |
|---|---|
| `ZkpSharp.Sdk` | **Entry point for most consumers.** High-level `ZkpHolder`, `ZkpIssuer`, `ZkpVerifier` facades. |
| `ZkpSharp.Core` | `DidId`, `Base58`. Zero external dependencies. |
| `ZkpSharp.Did` | `DidDocument`, `DidService`, `IDidStore`, wallet/channel binding, revocation. |
| `ZkpSharp.Attestations` | `Attestation`, `AttestationIssuer`, `MerkleTree`, `AttestationVerifier`, `PresentationVerifier`, `IIssuerRegistry`, `CredentialProof`. |
| `ZkpSharp.Cryptography` | Pure-C# secp256k1, Pedersen commitments, Bulletproofs (no external deps). |
| `ZkpSharp.Signing` | Production Ed25519 (NSec / libsodium). Drop-in `Ed25519Verifier` and `Ed25519IssuerSigner`. |
| `ZkpSharp.EntityFrameworkCore` | EF Core `IDidStore` and `IIssuerRegistry` over any relational provider (Postgres, SQL Server, SQLite). |
| `ZkpSharp.Chains.Abstractions` | `IChainAnchor` — chain-agnostic anchor interface. |
| `ZkpSharp.Chains.Solana` | Solana adapter targeting the `identity-registry` Anchor program. |
| `ZkpSharp.Chains.Stellar` | Stellar adapter scaffold targeting a Soroban anchor contract. |

## Repository layout

```
ZkpSharp/
├── src/
│   ├── ZkpSharp.Core/                    DidId, Base58
│   ├── ZkpSharp.Did/                     DID model + service
│   ├── ZkpSharp.Attestations/            Attestations + Merkle + CredentialProof
│   ├── ZkpSharp.Cryptography/            secp256k1 + Bulletproofs
│   ├── ZkpSharp.Signing/                 Ed25519 (NSec)
│   ├── ZkpSharp.EntityFrameworkCore/     Postgres/SQL Server/SQLite stores
│   ├── ZkpSharp.Chains.Abstractions/     IChainAnchor
│   ├── ZkpSharp.Chains.Solana/           Solana adapter (Solnet)
│   ├── ZkpSharp.Chains.Stellar/          Stellar adapter scaffold
│   └── ZkpSharp.Sdk/                     ZkpHolder, ZkpIssuer, ZkpVerifier
│
├── chains/
│   ├── solana/programs/identity-registry/   Anchor program (primary)
│   └── stellar/contracts/attestation-verifier/  Soroban contract (secondary)
│
├── ZkpSharp/                             v2.x monolith — kept for backward compat
├── examples/PrivacyApps/                 ConfidentialTransfer, SealedBidAuction, PrivateVoting
└── docs/architecture.md
```

See [docs/architecture.md](docs/architecture.md) for the on-chain/off-chain
boundary and the package dependency rules.

## Quick start

The SDK is the entry point. The three facades cover the three roles in any
attestation flow: holder, issuer, verifier.

### Install

```bash
dotnet add package ZkpSharp.Sdk
dotnet add package ZkpSharp.Signing
# pick the chain adapter you need:
dotnet add package ZkpSharp.Chains.Solana
# pick a store (or use the in-memory one for tests):
dotnet add package ZkpSharp.EntityFrameworkCore
```

### Holder side — create a DID, accept an attestation, present it

```csharp
using ZkpSharp.Sdk;
using ZkpSharp.Signing;
using ZkpSharp.Did;

// One-time keypair for the human/agent who controls this DID.
var (controllerPriv, controllerPub) = Ed25519.GenerateKeypair();

var holder = await ZkpHolder.CreateAsync(controllerPub, new ZkpHolderOptions
{
    Store              = new InMemoryDidStore(),     // or EfCoreDidStore for Postgres
    SignatureVerifier  = new Ed25519Verifier(),
    ChainAnchor        = solanaAnchor,                // optional; null = offline mode
});

// `holder.Did` is "did:zkp:<base58(sha256(pubkey||"v1"))>" — deterministic, not chosen.

// Later: accept an issuer-signed attestation, anchor the new root on-chain.
holder.AcceptAttestation(attestationFromIssuer);
await holder.AnchorRootAsync();

// Build a presentation for a relying app, disclosing only what it needs.
var presentation = holder.BuildPresentation(
    verifier:             new DidId("did:zkp:my-relying-app"),
    attestationTypes:     new[] { "phone_verified" },
    sessionNonce:         RandomBytes(16),
    asOfRevocationEpoch:  0,
    chain:                "solana",
    holderSignature:      walletSignatureOverBinding);
```

### Issuer side — sign attestations, publish your key

```csharp
using var signer = new Ed25519IssuerSigner(issuerPrivateKey);
var issuer = new ZkpIssuer(new DidId("did:zkp:my-issuer-service"), signer);

var attestation = issuer.Issue(
    type:     AttestationTypes.PhoneVerified,
    subject:  subjectDid,
    payload:  new AttestationPayload { Method = "twilio_v2" },
    validity: TimeSpan.FromDays(365));

// Register yourself once so verifiers can find you:
await issuerRegistry.RegisterAsync(issuer.BuildRegistryRecord(
    schemaUri: "https://schemas.zkp/attestation/v1"));
```

### Verifier side — check a presentation against a policy

```csharp
var zkp = new ZkpVerifier(new ZkpVerifierOptions
{
    IssuerRegistry     = issuerRegistry,
    SignatureVerifier  = new Ed25519Verifier(),
    ChainAnchor        = solanaAnchor,
});

var result = await zkp.VerifyPresentationAsync(presentation, new VerificationPolicy
{
    ExpectedVerifier              = new DidId("did:zkp:my-relying-app"),
    ExpectedSessionNonce          = nonceIssuedAtSessionStart,
    RequireCurrentRevocationEpoch = true,
});

if (!result.Valid)
    return Unauthorized(result.Reason);   // e.g. "verifier_mismatch", "revocation_stale"
```

### Predicate proof over a committed attestation value

For attestations carrying a Pedersen commitment, the holder proves a predicate
(e.g. `score ≥ 700`) without revealing the score. Bulletproofs on secp256k1,
implemented from scratch:

```csharp
using ZkpSharp.Attestations;

var cp = new CredentialProof();
var bundle = cp.ProveMinimum(actualValue: 85_000, minimumRequired: 50_000, label: "annual_income");
bool valid = cp.Verify(bundle);  // verifier learns only "income >= 50,000"
```

## Storage

`IDidStore` and `IIssuerRegistry` are pluggable. Two implementations ship:

- `InMemoryDidStore` / `InMemoryIssuerRegistry` — for tests and offline dev.
- `EfCoreDidStore` / `EfCoreIssuerRegistry` — EF Core 8, provider-agnostic.

Postgres example:

```csharp
services.AddDbContext<ZkpSharpDbContext>(opts =>
    opts.UseNpgsql(connectionString));

services.AddScoped<IDidStore, EfCoreDidStore>();
services.AddScoped<IIssuerRegistry, EfCoreIssuerRegistry>();
services.AddSingleton<ISignatureVerifier, Ed25519Verifier>();
```

Generate migrations against your chosen provider:
```bash
dotnet ef migrations add InitialZkpSharp --project ZkpSharp.EntityFrameworkCore
```

## Chains

The on-chain layer stores **only** Merkle attestation roots and revocation epochs.
DID documents, attestations, and proofs are never written on-chain.

| Chain | Status | Code |
|---|---|---|
| **Solana** | Adapter complete; program needs deployment | [`chains/solana/programs/identity-registry/`](chains/solana/programs/identity-registry/) |
| **Stellar** | Adapter scaffold; anchor contract pending | [`chains/stellar/contracts/attestation-verifier/`](chains/stellar/contracts/attestation-verifier/) |

The Solana adapter speaks to a minimal Anchor program with four instructions:
`register_did`, `update_root`, `bump_revocation`, `register_issuer`. Off-chain
verification stays in C#.

## v2 → v3

v3 is a breaking cut from the v2.x monolith. v2.x consumers keep working until
they upgrade.

| v2 type | v3 replacement |
|---|---|
| `ZkpSharp.Core.Zkp` (HMAC equality) | Removed. Use `CredentialProof` for ZK predicates. |
| `ZkpSharp.Interfaces.IBlockchain` | `ZkpSharp.Chains.IChainAnchor`. |
| `ZkpSharp.Integration.Stellar.*` | `ZkpSharp.Chains.Stellar`. |
| `ZkpSharp.Crypto.*` | `ZkpSharp.Cryptography`. |
| `ZkpSharp.Privacy.CredentialProof` | `ZkpSharp.Attestations.CredentialProof`. |

## License

MIT.
