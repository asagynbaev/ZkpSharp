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

## 5. Privacy SDK

The Privacy SDK provides ready-to-use solutions built on top of Bulletproofs. Instead of working with raw range proofs and commitments, you get domain-specific APIs for common privacy scenarios.

### Confidential transfers

Hide the transfer amount while proving the sender has sufficient balance.

```csharp
using ZkpSharp.Privacy;

var ct = new ConfidentialTransfer();

// Sender has 10,000 and wants to transfer 2,500
// The transfer amount is hidden from the verifier
var transfer = ct.CreateTransfer(senderBalance: 10000, transferAmount: 2500);

// Anyone can verify the transfer is valid (amount >= 0, change >= 0)
// without learning the transfer amount or the sender's balance
bool valid = ct.VerifyTransfer(transfer);

// Serialize for storage or network transmission
string serialized = ct.Serialize(transfer);
var restored = ct.Deserialize(serialized);
```

### Sealed-bid auctions

Commit to a hidden bid, prove it is within the auction's valid range, and reveal it later.

```csharp
using ZkpSharp.Privacy;

var auction = new SealedBidAuction(minBid: 100, maxBid: 50000);

// Bidder places a sealed bid (commitment + range proof)
// The bid amount is hidden until the reveal phase
var (bid, secret) = auction.PlaceBid(amount: 7500);

// Auctioneer verifies the bid is in [100, 50000] without seeing the amount
bool validBid = auction.VerifyBid(bid);

// After the auction closes, the bidder reveals their bid
long? revealedAmount = auction.RevealBid(bid, secret);
// revealedAmount == 7500

// Determine the winner from all revealed bids
int winnerIndex = auction.DetermineWinner(allBids, allOpenings);
```

### Private voting

Cast anonymous binary votes (yes/no) with cryptographic proof that each vote is valid.

```csharp
using ZkpSharp.Privacy;

var voting = new PrivateVoting();

// Each voter casts a private vote
var (ballot1, secret1) = voting.CastVote(voteYes: true);
var (ballot2, secret2) = voting.CastVote(voteYes: false);
var (ballot3, secret3) = voting.CastVote(voteYes: true);

// Anyone can verify each ballot is a valid vote (0 or 1)
// without learning which way the voter voted
bool isValid = voting.VerifyBallot(ballot1);

// Tally all votes by collecting ballot openings
var result = voting.Tally(
    new[] { ballot1, ballot2, ballot3 },
    new[] { secret1, secret2, secret3 });

// result.YesCount == 2, result.NoCount == 1, result.TotalCount == 3
```

### Credential verification

Prove that a numeric attribute (income, credit score, age, balance) meets a threshold or falls within a range without revealing the actual value.

```csharp
using ZkpSharp.Privacy;

var cred = new CredentialProof();

// Prove annual income >= 50,000 (without revealing actual income)
var incomeProof = cred.ProveMinimum(
    actualValue: 85000,
    minimumRequired: 50000,
    label: "annual_income");
bool incomeValid = cred.Verify(incomeProof);

// Prove credit score is in [700, 850] (without revealing exact score)
var scoreProof = cred.ProveRange(
    actualValue: 750,
    min: 700, max: 850,
    label: "credit_score");
bool scoreValid = cred.Verify(scoreProof);

// Serialize for transmission to a verifier
string serialized = cred.Serialize(incomeProof);
var restored = cred.Deserialize(serialized);
```

## 6. On-chain verification (Stellar)

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
