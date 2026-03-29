# ZkpSharp

A .NET library for Zero-Knowledge Proofs with Stellar Soroban blockchain integration. Prove facts about private data (age, balance, membership, range, time conditions) without revealing the data itself.

[![NuGet](https://img.shields.io/nuget/v/ZkpSharp)](https://www.nuget.org/packages/ZkpSharp)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ZkpSharp)](https://www.nuget.org/packages/ZkpSharp)
[![Build](https://github.com/asagynbaev/ZkpSharp/actions/workflows/dotnet.yml/badge.svg)](https://github.com/asagynbaev/ZkpSharp/actions/workflows/dotnet.yml)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

## Features

**Privacy proofs** (HMAC-based commitment schemes) -- fast, lightweight:

- Proof of Age, Balance, Membership, Range, Time Condition
- HMAC-SHA256 with cryptographic salt

**True zero-knowledge proofs** (Bulletproofs on secp256k1) -- mathematically sound:

- Real Pedersen commitments: `C = v*G + r*H` on the secp256k1 curve
- Inner product argument for O(log n) proof size (~690 bytes for 64-bit range)
- Fiat-Shamir transcript for non-interactive proofs
- Implemented from scratch in pure C# -- zero external crypto dependencies

**Privacy SDK** -- ready-to-use solutions for real-world scenarios:

- Confidential transfers (hide amounts, prove solvency)
- Sealed-bid auctions (commit-reveal with range proof)
- Private voting (anonymous ballots with verifiable tally)
- Credential verification (prove income, credit score, age meets threshold without revealing value)

**Stellar blockchain integration**:

- Soroban smart contract: full HMAC verification on-chain, structural validation for Bulletproofs
- `SorobanTransactionBuilder` for XDR construction
- `StellarBlockchain` high-level API for both HMAC and ZK on-chain flows

## Installation

```bash
dotnet add package ZkpSharp
```

## Quick start

```csharp
using ZkpSharp.Core;
using ZkpSharp.Security;

var proofProvider = new ProofProvider("your-base64-encoded-32-byte-key");
var zkp = new Zkp(proofProvider);

// Prove age >= 18 without revealing birthdate
var (proof, salt) = zkp.ProveAge(new DateTime(1995, 3, 15));
bool valid = zkp.VerifyAge(proof, new DateTime(1995, 3, 15), salt);

// Prove sufficient balance without revealing actual amount
var (bProof, bSalt) = zkp.ProveBalance(1000.0, 500.0);
bool bValid = zkp.VerifyBalance(bProof, 500.0, bSalt, 1000.0);
```

For Bulletproofs (true ZKP):

```csharp
using ZkpSharp.Security;

var zkProvider = new BulletproofsProvider();
var (proof, commitment) = zkProvider.ProveRange(value: 42, min: 0, max: 100);
bool valid = zkProvider.VerifyRange(proof, commitment, min: 0, max: 100);
```

For Privacy SDK (real-world scenarios):

```csharp
using ZkpSharp.Privacy;

// Confidential transfer -- amount hidden, proven valid
var ct = new ConfidentialTransfer();
var transfer = ct.CreateTransfer(senderBalance: 10000, transferAmount: 2500);
bool valid = ct.VerifyTransfer(transfer);

// Sealed-bid auction
var auction = new SealedBidAuction(minBid: 100, maxBid: 50000);
var (bid, secret) = auction.PlaceBid(7500);
bool bidValid = auction.VerifyBid(bid);
long? revealed = auction.RevealBid(bid, secret); // 7500
```

See [QUICKSTART.md](QUICKSTART.md) for a complete walkthrough including Stellar integration.

## API

### `Zkp` -- HMAC-based privacy proofs

| Method | Description |
|--------|-------------|
| `ProveAge(DateTime dateOfBirth)` | Prove age >= 18 (configurable) |
| `VerifyAge(string proof, DateTime dateOfBirth, string salt)` | Verify age proof |
| `ProveBalance(double balance, double requestedAmount)` | Prove balance >= requested |
| `VerifyBalance(string proof, double requestedAmount, string salt, double balance)` | Verify balance proof |
| `ProveMembership(string value, string[] validValues)` | Prove value is in set |
| `VerifyMembership(string proof, string value, string salt, string[] validValues)` | Verify membership proof |
| `ProveRange(double value, double minValue, double maxValue)` | Prove value is in range |
| `VerifyRange(string proof, double minValue, double maxValue, double value, string salt)` | Verify range proof |
| `ProveTimeCondition(DateTime eventDate, DateTime conditionDate)` | Prove event after date |
| `VerifyTimeCondition(string proof, DateTime eventDate, DateTime conditionDate, string salt)` | Verify time proof |

All `Prove*` methods return `(string Proof, string Salt)`. Salts are generated automatically and must be stored alongside proofs.

### `BulletproofsProvider` -- True zero-knowledge proofs

| Method | Description |
|--------|-------------|
| `ProveRange(long value, long min, long max)` | ZK range proof |
| `VerifyRange(byte[] proof, byte[] commitment, long min, long max)` | Verify ZK range proof |
| `ProveAge(DateTime birthDate, int minAge)` | ZK age proof |
| `VerifyAge(byte[] proof, byte[] commitment, int minAge)` | Verify ZK age proof |
| `ProveBalance(long balance, long requiredAmount)` | ZK balance proof |
| `VerifyBalance(byte[] proof, byte[] commitment, long requiredAmount)` | Verify ZK balance proof |
| `SerializeProof(byte[] proof, byte[] commitment)` | Serialize for storage |
| `DeserializeProof(string serialized)` | Deserialize proof |

`Prove*` methods return `(byte[] proof, byte[] commitment)`.

### Privacy SDK -- Ready-to-use privacy primitives

| Class | Method | Description |
|-------|--------|-------------|
| `ConfidentialTransfer` | `CreateTransfer(balance, amount)` | Hide transfer amount, prove solvency |
| `ConfidentialTransfer` | `VerifyTransfer(bundle)` | Verify without knowing the amount |
| `SealedBidAuction` | `PlaceBid(amount)` | Commit to hidden bid with range proof |
| `SealedBidAuction` | `VerifyBid(bid)` | Verify bid is in valid range |
| `SealedBidAuction` | `RevealBid(bid, opening)` | Reveal and verify bid after auction |
| `SealedBidAuction` | `DetermineWinner(bids, openings)` | Pick highest valid bid |
| `PrivateVoting` | `CastVote(voteYes)` | Commit to binary vote (0 or 1) |
| `PrivateVoting` | `VerifyBallot(ballot)` | Verify vote is valid without seeing it |
| `PrivateVoting` | `Tally(ballots, secrets)` | Count votes from verified openings |
| `CredentialProof` | `ProveMinimum(value, threshold, label)` | Prove attribute >= threshold |
| `CredentialProof` | `ProveRange(value, min, max, label)` | Prove attribute in range |
| `CredentialProof` | `Verify(credential)` | Verify credential proof |

### `StellarBlockchain` -- On-chain verification

Simulation uses Soroban RPC `simulateTransaction`. Methods without `WithSourceAccount` require environment variable **`ZKP_SOURCE_ACCOUNT`** (funded `G...` on the same network). Alternatively use `VerifyProofWithSourceAccount`, `VerifyBalanceProofWithSourceAccount`, or `VerifyZk*WithSourceAccount`.

| Method | Description |
|--------|-------------|
| `VerifyProof(contractId, proof, salt, value)` | HMAC proof (uses `ZKP_SOURCE_ACCOUNT` or use `VerifyProofWithSourceAccount`) |
| `VerifyBalanceProof(contractId, proof, balance, required, salt)` | HMAC balance proof (same) |
| `VerifyZkRangeProof` / `VerifyZkAgeProof` / `VerifyZkBalanceProof` | ZK on-chain structural check (same) |
| `VerifyProofWithTransactionXdrAsync(xdr)` | Verify using pre-built XDR |
| `GetAccountBalance(accountId)` | Get XLM balance for an account |

```csharp
Environment.SetEnvironmentVariable("ZKP_SOURCE_ACCOUNT", sourceAccountId);

var blockchain = new StellarBlockchain(
    serverUrl: "https://horizon-testnet.stellar.org",
    sorobanRpcUrl: "https://soroban-testnet.stellar.org",
    hmacKey: hmacKey
);

// HMAC proof on-chain verification
bool result = await blockchain.VerifyBalanceProof(contractId, proof, balance, required, salt);

// Bulletproofs ZK on-chain verification
var zkp = new BulletproofsProvider();
var (zkProof, commitment) = zkp.ProveRange(42, 0, 100);
bool zkResult = await blockchain.VerifyZkRangeProof(contractId, zkProof, commitment, 0, 100);

// ZK age verification on-chain
var (ageProof, ageCmt) = zkp.ProveAge(new DateTime(1990, 5, 15), minAge: 18);
bool ageResult = await blockchain.VerifyZkAgeProof(contractId, ageProof, ageCmt, 18);

// ZK balance verification on-chain
var (balProof, balCmt) = zkp.ProveBalance(10000, 5000);
bool balResult = await blockchain.VerifyZkBalanceProof(contractId, balProof, balCmt, 5000);
```

### `SorobanTransactionBuilder` -- Manual transaction construction

```csharp
var builder = new SorobanTransactionBuilder(Network.Test());

// HMAC proof verification
string xdr = builder.BuildVerifyProofTransaction(contractId, proof, data, salt, hmacKey);

// ZK proof verification
string zkXdr = builder.BuildVerifyZkRangeProofTransaction(contractId, proof, commitment, min, max);
string ageXdr = builder.BuildVerifyZkAgeProofTransaction(contractId, proof, commitment, minAge);
string balXdr = builder.BuildVerifyZkBalanceProofTransaction(contractId, proof, commitment, required);
```

## Soroban contract

The Rust smart contract (`contracts/stellar/contracts/proof-balance/`) exposes the following functions:

| Function | Description |
|----------|-------------|
| `verify_proof` | HMAC-SHA256 proof verification |
| `verify_balance_proof` | Balance proof with numeric comparison |
| `verify_batch` | Batch verification of multiple proofs |
| `verify_zk_range_proof` | Bulletproofs structural validation (full EC verification off-chain) |
| `verify_zk_age_proof` | ZK age proof structural validation |
| `verify_zk_balance_proof` | ZK balance proof structural validation |

See [contracts/stellar/README.md](contracts/stellar/README.md) for contract details and [contracts/stellar/DEPLOYMENT.md](contracts/stellar/DEPLOYMENT.md) for deployment instructions.

## Architecture

```
Application
  |
  +-- Privacy SDK (ready-to-use solutions)
  |     +-- ConfidentialTransfer (hidden amounts + solvency proofs)
  |     +-- SealedBidAuction (commit-reveal with range proofs)
  |     +-- PrivateVoting (anonymous ballots + verifiable tally)
  |     +-- CredentialProof (threshold / range proofs for any attribute)
  |
  +-- Zkp (HMAC-SHA256 commitment proofs)
  +-- BulletproofsProvider (real ZKP)
  |     +-- RangeProof (Bulletproofs prover/verifier)
  |     +-- PedersenCommitment (v*G + r*H on secp256k1)
  |     +-- InnerProductProof (recursive halving)
  |     +-- Transcript (Fiat-Shamir heuristic)
  |     +-- Point / Scalar / FieldElement (secp256k1 math)
  |
  +-- StellarBlockchain
        +-- SorobanTransactionBuilder --> XDR
        +-- SorobanRpcClient --> Soroban RPC --> ZkpVerifier Contract
```

## Cryptography

The Bulletproofs implementation is built from scratch in pure C#:

- **Elliptic curve**: secp256k1 (same curve as Bitcoin) with Jacobian coordinate arithmetic
- **Commitment scheme**: Pedersen commitments `C = v*G + r*H` where G is the standard generator and H is derived via hash-to-curve
- **Range proof**: Bulletproofs protocol with logarithmic proof size (O(log n) inner product argument)
- **Non-interactive**: Fiat-Shamir heuristic via SHA-256 transcript
- **No unsafe dependencies**: All field/scalar/point operations implemented in managed C# using `System.Numerics.BigInteger`

## Security

- **Key management**: Use environment variables for development. Use Azure Key Vault, AWS Secrets Manager, or HashiCorp Vault in production.
- **Salts**: Never reuse. ZkpSharp generates cryptographically secure random salts automatically.
- **Bulletproofs blinding**: Each proof uses a fresh random blinding factor `r`, making commitments computationally hiding.
- **Contract verification**: Always verify deployed contract code matches source.
- **On-chain limitations**: Full Bulletproofs verification (secp256k1 EC math) runs off-chain via `BulletproofsProvider.VerifyRange()`. The Soroban contract performs structural validation only (point prefix checks, IPA length) and emits a transcript binding hash for off-chain auditing. HMAC proofs are fully verified on-chain.

## Documentation

| Document | Description |
|----------|-------------|
| [QUICKSTART.md](QUICKSTART.md) | Step-by-step setup and usage guide |
| [DEPLOYMENT.md](contracts/stellar/DEPLOYMENT.md) | Soroban contract deployment |
| [STELLAR_REALITY_CHECK.md](STELLAR_REALITY_CHECK.md) | Capabilities and limitations |
| [INTEGRATION_STATUS.md](INTEGRATION_STATUS.md) | Feature status and migration guide |
| [CHANGELOG.md](CHANGELOG.md) | Version history |

## FAQ

**Why aren't Bulletproofs fully verified on-chain?**

Soroban provides native support for BLS12-381 curve operations, but the Bulletproofs protocol is defined over secp256k1. Implementing full secp256k1 point arithmetic inside a Soroban contract would exceed compute budgets and add significant complexity with no upstream support. Our contract performs structural validation (compressed point format, IPA length, range bounds) and emits a transcript binding hash as an on-chain anchor. Full cryptographic verification runs off-chain via `BulletproofsProvider.VerifyRange()`. This is a platform limitation, not a design shortcut.

**Why implement Bulletproofs from scratch instead of using an existing library?**

The .NET ecosystem has no mature, maintained Bulletproofs library. Existing options are either abandoned, incomplete, or rely on unsafe native interop. We implemented the protocol from first principles using standard, well-documented primitives: secp256k1 curve parameters (same as Bitcoin), SHA-256 for Fiat-Shamir, and `System.Numerics.BigInteger` for field arithmetic. The implementation is covered by 44 cryptographic tests including soundness checks (tampered proofs, out-of-range values, serialization round-trips).

**Why Bulletproofs and not Groth16 / zkSNARKs?**

Bulletproofs are ideal for range proofs: they require no trusted setup ceremony, produce compact proofs (~690 bytes for a 64-bit range), and rely on standard discrete-log assumptions. zkSNARKs (Groth16) require a per-circuit trusted setup and are better suited for general-purpose computation proofs. Groth16 support is on the roadmap for future releases.

**Is this production-ready?**

The cryptographic core is functionally complete and covered by tests, but it has not undergone an independent security audit. For testnet deployments, PoCs, and non-financial applications, the library is ready to use. For production systems handling real value, we recommend a third-party cryptographic audit before deployment. The HMAC-based proof system (which does not involve custom cryptography) is suitable for production use as-is.

**How does this compare to HMAC-based proofs?**

| | HMAC proofs | Bulletproofs |
|---|---|---|
| Zero-knowledge | No (commitment scheme) | Yes (mathematically proven) |
| Verifier learns | Nothing if key is secret | Nothing about the secret value |
| Security basis | HMAC key secrecy | Discrete logarithm hardness |
| Proof size | 32 bytes | ~690 bytes |
| Speed | < 1 ms | ~200-500 ms (prove), ~50 ms (verify) |
| On-chain verification | Full (HMAC recomputation) | Structural only (full EC off-chain) |
| Best for | Fast checks, internal systems | Regulatory compliance, trustless scenarios |

## Publishing to NuGet (maintainers)

1. Commit and tag the release (e.g. `v2.2.0`), ensure `PackageVersion` in `ZkpSharp/ZkpSharp.csproj` matches.
2. Build and pack: `dotnet pack ZkpSharp/ZkpSharp.csproj -c Release`
3. Create an [API key](https://www.nuget.org/account/apikeys) on nuget.org with scope **Push** for package **ZkpSharp**.
4. Push (replace `YOUR_API_KEY`):

```bash
dotnet nuget push ZkpSharp/bin/Release/ZkpSharp.*.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
dotnet nuget push ZkpSharp/bin/Release/ZkpSharp.*.snupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

The NuGet badge in this README updates after indexing (usually within minutes).

## Contributing

1. Fork the repository
2. Create a branch (`git checkout -b feature/your-feature`)
3. Run tests (`dotnet test`)
4. Submit a pull request

## License

MIT License. See [LICENSE](LICENSE) for details.

## Contact

[sagynbaev6@gmail.com](mailto:sagynbaev6@gmail.com) | [GitHub Issues](https://github.com/asagynbaev/ZkpSharp/issues) | [NuGet](https://www.nuget.org/packages/ZkpSharp)
