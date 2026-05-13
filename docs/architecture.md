# Architecture

## What this is

Privacy-preserving identity and reputation infrastructure for our own product. Concretely:

- A DID layer where one human → one DID, multi-wallet, multi-channel.
- Generic attestation envelopes (issuer-signed, type-tagged, expiring).
- Selective-disclosure presentations with Merkle inclusion proofs.
- A minimal on-chain anchor — Merkle root + revocation epoch — Solana primary, Stellar secondary.
- A from-scratch Bulletproofs-on-secp256k1 library, used for selective disclosure
  over committed values.

## What this is not

- Not a generic ZKP playground.
- Not a zkVM, not a new chain, not a token, not a governance system.
- Not a payments / DeFi / prediction-market toolkit.
- Not a research-only cryptography experiment.

## Boundary: on-chain vs off-chain

Hard rule: the chain is for **audit**, not for **storage**.

```
+--------------------------------+      +------------------------------+
|         OFF-CHAIN              |      |          ON-CHAIN            |
|                                |      |                              |
|  DID documents (signed)        |      |  IdentityRegistry program    |
|  Wallet bindings (sig'd chal.) |  ->  |   - did_anchor accounts:     |
|  Attestations (issuer-signed)  |      |       attestation_root (32B) |
|  Merkle bundles + presentation |      |       revocation_epoch (u64) |
|  Reputation scores (computed)  |      |       owner pubkey           |
|  Bulletproof verification      |      |   - issuer accounts:         |
|  Issuer registry (auth source) |      |       schema_uri, active     |
|                                |      |                              |
+--------------------------------+      +------------------------------+
```

The chain answers: *was the holder's attestation root R at revocation_epoch E, and is
issuer X registered as active?* It does no cryptographic proof verification.

## Repository layout

```
ZkpSharp/
├── src/
│   ├── ZkpSharp.Core/                    DidId, Base58 — shared, dependency-free
│   ├── ZkpSharp.Did/                     DidDocument, DidService, IDidStore, wallet binding, revocation
│   ├── ZkpSharp.Did.Tests/
│   ├── ZkpSharp.Attestations/            Attestation, AttestationIssuer, MerkleTree, AttestationVerifier,
│   │                                     PresentationVerifier, IIssuerRegistry
│   ├── ZkpSharp.Attestations.Tests/
│   └── ZkpSharp.Chains.Abstractions/     IChainAnchor + state types — chain-agnostic
│
├── chains/
│   ├── solana/                           Anchor IdentityRegistry program (primary)
│   │   ├── Anchor.toml
│   │   ├── Cargo.toml
│   │   └── programs/identity-registry/
│   └── stellar/                          Existing Soroban work (secondary)
│       ├── Cargo.toml
│       └── contracts/attestation-verifier/
│
├── ZkpSharp/                             v2 monolith — published as NuGet 2.x
│   ├── Core/Zkp.cs                       HMAC equality, [Obsolete]
│   ├── Crypto/                           secp256k1 + Bulletproofs (kept; moves to Cryptography pkg at v3)
│   ├── Privacy/CredentialProof.cs        Real ZK selective disclosure (moves to Attestations at v3)
│   ├── Integration/Stellar/              Existing client (moves to Chains.Stellar at v3)
│   └── Interfaces/IBlockchain.cs         [Obsolete]
│
├── ZkpSharp.Tests/                       Tests for the v2 monolith
│
├── examples/
│   ├── PrivacyApps/                      ConfidentialTransfer, SealedBidAuction, PrivateVoting
│   └── PrivacyApps.Tests/
│
└── docs/
    └── architecture.md                   ← this file
```

## Packages and dependencies

```
                     ┌───────────────────────┐
                     │   ZkpSharp.Core       │  (DidId, Base58)
                     └────────┬──────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        │                     │                     │
┌───────▼─────────┐  ┌────────▼─────────┐  ┌────────▼──────────────┐
│  ZkpSharp.Did   │  │ ZkpSharp.        │  │ ZkpSharp.Chains.      │
│                 │  │ Attestations     │  │ Abstractions          │
└─────────────────┘  └──────────────────┘  └─────────┬─────────────┘
                                                     │
                                          ┌──────────┴──────────┐
                                          │                     │
                                ┌─────────▼────────┐  ┌─────────▼────────┐
                                │ ZkpSharp.Chains. │  │ ZkpSharp.Chains. │
                                │ Solana (TODO)    │  │ Stellar (TODO)   │
                                └──────────────────┘  └──────────────────┘
```

Hard invariants:
- `Core` has zero external dependencies and zero references to chains or crypto stacks.
- `Did` and `Attestations` reference only `Core`. They never know which chain backs anchoring.
- Chain adapter packages implement `IChainAnchor` from `Chains.Abstractions`.

## DID method

DIDs are derived from the controller key, not chosen by the holder:

```
did:zkp:<base58(sha256(pubkey || "v1"))>
```

`DidService.CreateAsync` enforces this. The caller cannot pick the identifier — this
prevents squatting and ties identity to provable control.

A DID document carries:
- The controller key (`Ed25519VerificationKey2020` by default).
- A list of bound wallets, each with a signature from the wallet itself over
  `{did, chain, address, nonce, expiry}`. `DidService.BindWalletAsync` enforces all five.
- A list of bound off-chain channels (Telegram, phone, email) as `blake3(handle || salt)`
  commitments — never plaintext, never salt-only.
- The current Merkle attestation root.

## Attestation flow

1. Issuer holds a signing key and is registered in the on-chain issuer registry
   (program: `chains/solana/programs/identity-registry`).
2. Issuer calls `AttestationIssuer.Issue(type, subject, payload)` to sign an envelope.
3. Holder collects attestations into an `AttestationBundle`. The bundle's `Root`
   is anchored on-chain via `IChainAnchor.AnchorRootAsync`.
4. Verifier asks for a presentation. Holder calls `bundle.DisclosureFor(index)` and
   wraps it in a `Presentation` bound to `{verifier, session_nonce, as_of_epoch, chain}`.
5. Verifier calls `PresentationVerifier.VerifyAsync(presentation, expectedRoot)` where
   `expectedRoot` is read from `IChainAnchor.GetAnchorAsync`.

The Merkle tree is domain-separated SHA-256 (leaf tag `0x00`, node tag `0x01`).

## Threat model summary

See `docs/threat-model.md` (TODO) for detail. Headline risks:

1. **Low-entropy channel commitments** (Telegram handle space ~10⁹). Mitigation:
   HKDF with KMS-held pepper.
2. **Wallet-binding spoofing.** Mitigation: challenge–response signed by the wallet itself,
   with all five fields required.
3. **Issuer key compromise.** Mitigation: per-issuer revocation epoch + short attestation expiries.
4. **Replay across verifiers / DIDs / chains.** Mitigation: every presentation is bound to
   `{verifier, session_nonce, as_of_revocation_epoch, chain}`.

## What still has to move (v3 cut)

- `ZkpSharp/Crypto/*` → `src/ZkpSharp.Cryptography/`.
- `ZkpSharp/Integration/Stellar/*` → `src/ZkpSharp.Chains.Stellar/` (implementing `IChainAnchor`).
- `ZkpSharp/Privacy/CredentialProof.cs` → `src/ZkpSharp.Attestations/` (as the predicate-proof primitive).
- `ZkpSharp/Core/Zkp.cs` + `ZkpSharp/Interfaces/IBlockchain.cs` → deleted.
- New: `src/ZkpSharp.Chains.Solana/` — C# adapter against the Anchor program.

The v3 cut is a breaking release. Until then, both layouts coexist and tests cover both.
