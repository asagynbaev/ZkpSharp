# ZkpSharp Quick Start

Get up and running with ZkpSharp in 5 minutes.

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- (Optional) [Rust toolchain](https://rustup.rs/) and [Stellar CLI](https://developers.stellar.org/docs/tools/developer-tools/cli/stellar-cli) for on-chain verification

## 1. Install the package

```bash
dotnet add package ZkpSharp
```

## 2. Generate an HMAC key

The library requires a 32-byte Base64-encoded key for HMAC-SHA256 operations.

Using OpenSSL:

```bash
openssl rand -base64 32
```

Or in C#:

```csharp
using System.Security.Cryptography;

var key = new byte[32];
RandomNumberGenerator.Fill(key);
string hmacKey = Convert.ToBase64String(key);
```

Store this key securely. You will need it for both generating and verifying proofs.

## 3. Basic usage

```csharp
using ZkpSharp.Core;
using ZkpSharp.Security;

var proofProvider = new ProofProvider(hmacKey);
var zkp = new Zkp(proofProvider);
```

### Age verification

```csharp
var dateOfBirth = new DateTime(1995, 3, 15);

var (proof, salt) = zkp.ProveAge(dateOfBirth);
bool isValid = zkp.VerifyAge(proof, dateOfBirth, salt);
// isValid == true (age >= 18)
```

### Balance verification

```csharp
double balance = 1000.0;
double required = 500.0;

var (proof, salt) = zkp.ProveBalance(balance, required);
bool isValid = zkp.VerifyBalance(proof, required, salt, balance);
```

### Membership verification

```csharp
var tiers = new[] { "gold", "silver", "bronze" };

var (proof, salt) = zkp.ProveMembership("gold", tiers);
bool isValid = zkp.VerifyMembership(proof, "gold", salt, tiers);
```

### Range verification

```csharp
var (proof, salt) = zkp.ProveRange(value: 75.0, minValue: 0.0, maxValue: 100.0);
bool isValid = zkp.VerifyRange(proof, 0.0, 100.0, 75.0, salt);
```

### Time condition verification

```csharp
var eventDate = new DateTime(2025, 6, 1);
var cutoff = new DateTime(2025, 1, 1);

var (proof, salt) = zkp.ProveTimeCondition(eventDate, cutoff);
bool isValid = zkp.VerifyTimeCondition(proof, eventDate, cutoff, salt);
```

## 4. True Zero-Knowledge Proofs (Bulletproofs)

ZkpSharp includes a full Bulletproofs implementation built from scratch in pure C# on the secp256k1 curve. Unlike the HMAC-based proofs above, Bulletproofs provide real zero-knowledge: the verifier learns nothing about the secret value beyond the stated claim. Proofs use Pedersen commitments (`C = v*G + r*H`), an inner product argument for O(log n) size, and a Fiat-Shamir transcript for non-interactivity.

```csharp
using ZkpSharp.Security;
using ZkpSharp.Interfaces;

IZkProofProvider zkProvider = new BulletproofsProvider();

// Range proof: prove value is in [0, 100] without revealing it
var (proof, commitment) = zkProvider.ProveRange(value: 42, min: 0, max: 100);
bool isValid = zkProvider.VerifyRange(proof, commitment, min: 0, max: 100);

// Age proof: prove age >= 18 without revealing birthdate
var (ageProof, ageCmt) = zkProvider.ProveAge(new DateTime(1990, 5, 15), minAge: 18);
bool ageValid = zkProvider.VerifyAge(ageProof, ageCmt, minAge: 18);

// Balance proof: prove balance >= required without revealing amount
var (balProof, balCmt) = zkProvider.ProveBalance(balance: 10000, requiredAmount: 5000);
bool balValid = zkProvider.VerifyBalance(balProof, balCmt, requiredAmount: 5000);
```

Proofs can be serialized for storage or network transmission:

```csharp
string serialized = zkProvider.SerializeProof(proof, commitment);
var (deserializedProof, deserializedCommitment) = zkProvider.DeserializeProof(serialized);
```

## 5. On-chain verification (Stellar)

### Deploy the Soroban contract

```bash
cd contracts/stellar
rustup target add wasm32-unknown-unknown
cargo build --target wasm32-unknown-unknown --release --package proof-balance

soroban contract deploy \
  --wasm target/wasm32-unknown-unknown/release/proof_balance.wasm \
  --source <YOUR_SECRET_KEY> \
  --network testnet
```

Save the contract ID from the output.

### Set environment variables

```bash
export ZKP_HMAC_KEY="your-base64-encoded-key"
export ZKP_CONTRACT_ID="CABC..."
```

### Verify HMAC proofs on-chain

```csharp
using ZkpSharp.Core;
using ZkpSharp.Security;
using ZkpSharp.Integration.Stellar;

var hmacKey = Environment.GetEnvironmentVariable("ZKP_HMAC_KEY")!;
var contractId = Environment.GetEnvironmentVariable("ZKP_CONTRACT_ID")!;

var zkp = new Zkp(new ProofProvider(hmacKey));

var blockchain = new StellarBlockchain(
    "https://horizon-testnet.stellar.org",
    "https://soroban-testnet.stellar.org",
    hmacKey: hmacKey
);

var (proof, salt) = zkp.ProveBalance(1000.0, 500.0);
bool verified = await blockchain.VerifyBalanceProof(
    contractId, proof, 1000.0, 500.0, salt
);
```

### Verify Bulletproofs ZK proofs on-chain

```csharp
using ZkpSharp.Security;
using ZkpSharp.Integration.Stellar;

var contractId = Environment.GetEnvironmentVariable("ZKP_CONTRACT_ID")!;

var blockchain = new StellarBlockchain(
    "https://horizon-testnet.stellar.org",
    "https://soroban-testnet.stellar.org"
);

var zkp = new BulletproofsProvider();

// Generate ZK proof off-chain
var (proof, commitment) = zkp.ProveRange(42, 0, 100);

// Verify on-chain (structural validation + Fiat-Shamir binding)
bool verified = await blockchain.VerifyZkRangeProof(
    contractId, proof, commitment, 0, 100
);

// Age proof: prove age >= 18 and verify on-chain
var (ageProof, ageCmt) = zkp.ProveAge(new DateTime(1990, 5, 15), minAge: 18);
bool ageVerified = await blockchain.VerifyZkAgeProof(
    contractId, ageProof, ageCmt, 18
);

// Balance proof: prove balance >= 5000 and verify on-chain
var (balProof, balCmt) = zkp.ProveBalance(10000, 5000);
bool balVerified = await blockchain.VerifyZkBalanceProof(
    contractId, balProof, balCmt, 5000
);
```

### Build transactions manually (advanced)

```csharp
using StellarDotnetSdk;
using ZkpSharp.Integration.Stellar;

var builder = new SorobanTransactionBuilder(Network.Test());

// HMAC proof transaction
string xdr = builder.BuildVerifyProofTransaction(
    contractId, proof, "data-to-verify", salt, hmacKey
);

// ZK range proof transaction
string zkXdr = builder.BuildVerifyZkRangeProofTransaction(
    contractId,
    Convert.ToBase64String(zkProof),
    Convert.ToBase64String(commitment),
    min: 0, max: 100
);

var rpcClient = new SorobanRpcClient("https://soroban-testnet.stellar.org");
bool result = await rpcClient.InvokeContractWithTransactionXdrAsync(zkXdr);
```

## Next steps

- [README.md](README.md) -- Full API reference and architecture overview
- [contracts/stellar/DEPLOYMENT.md](contracts/stellar/DEPLOYMENT.md) -- Detailed contract deployment guide
- [STELLAR_REALITY_CHECK.md](STELLAR_REALITY_CHECK.md) -- Capabilities and limitations
- [CHANGELOG.md](CHANGELOG.md) -- Version history
