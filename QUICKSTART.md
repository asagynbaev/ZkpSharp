# ZkpSharp + Stellar: Quick Start Guide

Get up and running with ZkpSharp on Stellar in 5 minutes.

## Prerequisites

- .NET 8.0 SDK
- Rust toolchain
- Soroban CLI
- Stellar testnet account

## Step-by-Step Guide

### 1. Clone and Setup

```bash
git clone https://github.com/asagynbaev/ZkpSharp.git
cd ZkpSharp

# Install .NET dependencies
dotnet restore

# Setup Rust and Soroban
cd contracts/stellar
make setup
```

### 2. Generate HMAC Key

```bash
# Generate a secure 32-byte key
openssl rand -base64 32

# Output example:
# V0V3Mv4D1USxZYwWL4eG93m0JKdO9KbXQn0mhg+EXHc=

# Save this key!
export ZKP_HMAC_KEY="YOUR_GENERATED_KEY"
```

### 3. Setup Stellar Testnet Account

```bash
# Install Soroban CLI (if not done in step 1)
cargo install --locked soroban-cli --features opt

# Configure testnet
soroban network add \
  --global testnet \
  --rpc-url https://soroban-testnet.stellar.org \
  --network-passphrase "Test SDF Network ; September 2015"

# Generate account
soroban keys generate --global alice --network testnet

# Fund account from friendbot
soroban keys fund alice --network testnet

# Or use Makefile
make fund-testnet ACCOUNT=alice
```

### 4. Build and Deploy Contract

```bash
# Navigate to contracts
cd contracts/stellar

# Build, optimize and deploy in one command
make deploy-testnet SOURCE_ACCOUNT=alice

# Output will show your contract ID:
# ✅ Contract deployed!
# 📝 Contract ID: CXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
# 
# 💾 Save this contract ID:
# export ZKP_CONTRACT_ID=CXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX

# Save the contract ID
export ZKP_CONTRACT_ID="YOUR_CONTRACT_ID"
```

### 5. Run Example Application

Create a file `Program.cs`:

```csharp
using System;
using ZkpSharp.Core;
using ZkpSharp.Security;
using ZkpSharp.Integration.Stellar;

// Get configuration from environment
var hmacKey = Environment.GetEnvironmentVariable("ZKP_HMAC_KEY");
var contractId = Environment.GetEnvironmentVariable("ZKP_CONTRACT_ID");

if (string.IsNullOrEmpty(hmacKey) || string.IsNullOrEmpty(contractId))
{
    Console.WriteLine("❌ Error: ZKP_HMAC_KEY and ZKP_CONTRACT_ID must be set");
    return;
}

Console.WriteLine("🚀 ZkpSharp + Stellar Demo");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━");

// Initialize ZKP
var proofProvider = new ProofProvider(hmacKey);
var zkp = new Zkp(proofProvider);

// Initialize Stellar blockchain
var blockchain = new StellarBlockchain(
    "https://horizon-testnet.stellar.org",
    "https://soroban-testnet.stellar.org"
);

// Example 1: Balance Proof
Console.WriteLine("\n📊 Example 1: Balance Proof");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━");

double balance = 1000.0;
double requestedAmount = 500.0;

// Generate proof (off-chain)
var (balanceProof, balanceSalt) = zkp.ProveBalance(balance, requestedAmount);
Console.WriteLine($"✅ Proof generated: {balanceProof[..20]}...");

// Verify off-chain
var isValidOffChain = zkp.VerifyBalance(balanceProof, requestedAmount, balanceSalt, balance);
Console.WriteLine($"✅ Off-chain verification: {isValidOffChain}");

// Verify on-chain (on Stellar)
Console.WriteLine("🔄 Verifying on Stellar blockchain...");
var isValidOnChain = await blockchain.VerifyBalanceProof(
    contractId,
    balanceProof,
    balance,
    requestedAmount,
    balanceSalt
);
Console.WriteLine($"✅ On-chain verification: {isValidOnChain}");

// Example 2: Age Proof
Console.WriteLine("\n👤 Example 2: Age Proof");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━");

var dateOfBirth = new DateTime(1990, 1, 1);
var (ageProof, ageSalt) = zkp.ProveAge(dateOfBirth);
Console.WriteLine($"✅ Age proof generated: {ageProof[..20]}...");

var ageIsValid = zkp.VerifyAge(ageProof, dateOfBirth, ageSalt);
Console.WriteLine($"✅ Age verification: {ageIsValid}");

// Example 3: Membership Proof
Console.WriteLine("\n👥 Example 3: Membership Proof");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━");

var userId = "user123";
var validUsers = new[] { "user123", "user456", "user789" };
var (membershipProof, membershipSalt) = zkp.ProveMembership(userId, validUsers);
Console.WriteLine($"✅ Membership proof generated: {membershipProof[..20]}...");

var membershipIsValid = zkp.VerifyMembership(membershipProof, userId, membershipSalt, validUsers);
Console.WriteLine($"✅ Membership verification: {membershipIsValid}");

Console.WriteLine("\n✨ All examples completed successfully!");
Console.WriteLine($"📝 Contract ID: {contractId}");
Console.WriteLine($"🌐 Network: Stellar Testnet");
```

Run the application:

```bash
# Set environment variables
export ZKP_HMAC_KEY="your-key-here"
export ZKP_CONTRACT_ID="your-contract-id-here"

# Run
dotnet run
```

## Next Steps

### Learn More

- [Full Documentation](README.md)
- [Deployment Guide](contracts/stellar/DEPLOYMENT.md)
- [API Reference](ZkpSharp/Core/ZKP.cs)
- [Soroban Contract](contracts/stellar/contracts/proof-balance/src/lib.rs)

### Try More Examples

1. **Range Proofs**: Prove a value is within a range
```csharp
var (proof, salt) = zkp.ProveRange(75.5, 0, 100);
var isValid = zkp.VerifyRange(proof, 0, 100, 75.5, salt);
```

2. **Time Condition Proofs**: Prove an event occurred after a date
```csharp
var eventDate = DateTime.UtcNow;
var conditionDate = DateTime.UtcNow.AddDays(-7);
var (proof, salt) = zkp.ProveTimeCondition(eventDate, conditionDate);
var isValid = zkp.VerifyTimeCondition(proof, eventDate, conditionDate, salt);
```

3. **Batch Verification**: Verify multiple proofs efficiently
```csharp
// Generate multiple proofs
var proofs = new List<(string proof, string salt)>();
for (int i = 0; i < 5; i++)
{
    var (proof, salt) = zkp.ProveBalance(1000 * i, 500);
    proofs.Add((proof, salt));
}

// Verify using batch operations on the contract
// (See contract documentation for batch verification)
```

### Deploy to Mainnet

⚠️ **Before deploying to mainnet:**

1. ✅ Test thoroughly on testnet
2. ✅ Have contract audited
3. ✅ Use secure key management (Azure Key Vault, AWS Secrets Manager)
4. ✅ Ensure you have sufficient XLM for fees
5. ✅ Review security best practices

```bash
# Deploy to mainnet (requires confirmation)
cd contracts/stellar
make deploy-mainnet SOURCE_ACCOUNT=production-key
```

## Troubleshooting

### "Contract ID not configured"
```bash
# Make sure you've set the environment variable
export ZKP_CONTRACT_ID="C..."
```

### "HMAC key not configured"
```bash
# Generate and set the HMAC key
export ZKP_HMAC_KEY=$(openssl rand -base64 32)
```

### "Account not found"
```bash
# Fund your testnet account
make fund-testnet ACCOUNT=alice
# Or manually:
soroban keys fund alice --network testnet
```

### Contract deployment fails
```bash
# Check you have the wasm32 target
rustup target add wasm32-unknown-unknown

# Clean and rebuild
make clean
make build

# Try deploying again
make deploy-testnet SOURCE_ACCOUNT=alice
```

## Tips

1. **Development Workflow**:
   ```bash
   # Quick development cycle
   make dev  # Builds and tests
   ```

2. **Check Contract Status**:
   ```bash
   make verify CONTRACT_ID=C...
   ```

3. **Keep Dependencies Updated**:
   ```bash
   cargo update
   dotnet update
   ```

4. **Monitor Gas Costs**:
   - Single verification: ~1,000-2,000 operations
   - Batch verification: ~800-1,500 operations per proof
   - Monitor costs in production and optimize as needed

## Common Use Cases

### DeFi Application
```csharp
// Prove sufficient balance for a swap without revealing amount
var userBalance = 10000.0;
var swapAmount = 1000.0;
var (proof, salt) = zkp.ProveBalance(userBalance, swapAmount);

// Verify on-chain
var canSwap = await blockchain.VerifyBalanceProof(
    contractId, proof, userBalance, swapAmount, salt
);
```

### Age Verification
```csharp
// Prove user is over 18 without revealing birthdate
var dateOfBirth = new DateTime(1995, 5, 15);
var (proof, salt) = zkp.ProveAge(dateOfBirth);

// Verify on-chain or off-chain
var isAdult = zkp.VerifyAge(proof, dateOfBirth, salt);
```

### Membership Verification
```csharp
// Prove membership in a group without revealing identity
var memberId = "premium-user-12345";
var validMembers = GetPremiumMembers(); // From database
var (proof, salt) = zkp.ProveMembership(memberId, validMembers);

// Verify membership
var isMember = zkp.VerifyMembership(proof, memberId, salt, validMembers);
```

## Resources

- [Stellar Developer Portal](https://developers.stellar.org/)
- [Soroban Docs](https://soroban.stellar.org/)
- [Stellar Discord](https://discord.gg/stellar)
- [ZkpSharp GitHub](https://github.com/asagynbaev/ZkpSharp)

## Support

For assistance:
- Email: sagynbaev6@gmail.com
- Discord: Stellar Community (https://discord.gg/stellar)
- Issues: GitHub Issues (https://github.com/asagynbaev/ZkpSharp/issues)

