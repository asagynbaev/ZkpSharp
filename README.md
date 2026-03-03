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

**True zero-knowledge proofs** (Bulletproofs) -- mathematically sound:

- ZK range, age, and balance proofs using Pedersen commitments
- Compact serialization for storage and transmission

**Stellar blockchain integration**:

- Soroban SDK 25 with BLS12-381 cryptography
- On-chain verification via `InvokeHostFunctionOp`
- `SorobanTransactionBuilder` for XDR construction

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

### `StellarBlockchain` -- On-chain verification

```csharp
var blockchain = new StellarBlockchain(
    serverUrl: "https://horizon-testnet.stellar.org",
    sorobanRpcUrl: "https://soroban-testnet.stellar.org",
    hmacKey: hmacKey
);

bool result = await blockchain.VerifyBalanceProof(contractId, proof, balance, required, salt);
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
| `verify_zk_range_proof` | BLS12-381 ZK range verification |
| `verify_zk_age_proof` | ZK age verification |
| `verify_zk_balance_proof` | ZK balance verification |

See [contracts/stellar/README.md](contracts/stellar/README.md) for contract details and [contracts/stellar/DEPLOYMENT.md](contracts/stellar/DEPLOYMENT.md) for deployment instructions.

## Architecture

```
Application
  |
  +-- Zkp (HMAC proofs)
  +-- BulletproofsProvider (ZK proofs)
  |
  +-- StellarBlockchain
        +-- SorobanTransactionBuilder --> XDR
        +-- SorobanRpcClient --> Soroban RPC --> ZkpVerifier Contract
```

## Security

- **Key management**: Use environment variables for development. Use Azure Key Vault, AWS Secrets Manager, or HashiCorp Vault in production.
- **Salts**: Never reuse. ZkpSharp generates cryptographically secure random salts automatically.
- **Contract verification**: Always verify deployed contract code matches source.
- **Transaction fees**: Soroban transactions require XLM.

## Documentation

| Document | Description |
|----------|-------------|
| [QUICKSTART.md](QUICKSTART.md) | Step-by-step setup and usage guide |
| [DEPLOYMENT.md](contracts/stellar/DEPLOYMENT.md) | Soroban contract deployment |
| [STELLAR_REALITY_CHECK.md](STELLAR_REALITY_CHECK.md) | Capabilities and limitations |
| [INTEGRATION_STATUS.md](INTEGRATION_STATUS.md) | Feature status and migration guide |
| [CHANGELOG.md](CHANGELOG.md) | Version history |

## Contributing

1. Fork the repository
2. Create a branch (`git checkout -b feature/your-feature`)
3. Run tests (`dotnet test`)
4. Submit a pull request

## License

MIT License. See [LICENSE](LICENSE) for details.

## Contact

[sagynbaev6@gmail.com](mailto:sagynbaev6@gmail.com) | [GitHub Issues](https://github.com/asagynbaev/ZkpSharp/issues) | [NuGet](https://www.nuget.org/packages/ZkpSharp)
