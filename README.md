# ZkpSharp

**ZkpSharp** is a .NET library for implementing Zero-Knowledge Proofs (ZKP). This library allows you to securely prove certain information (such as age or balance) without revealing the actual data. It uses cryptographic hashes and salts to ensure privacy and security.

## Features

- **Proof of Age**: Prove that your age is above a certain threshold without revealing your actual birthdate.
- **Proof of Balance**: Prove that you have sufficient balance to make a transaction without revealing your full balance.
- **Proof of Membership**: Prove that a given value belongs to a set of valid values (e.g., proving you belong to a specific group).
- **Proof of Range**: Prove that a value lies within a specified range without revealing the exact value.
- **Proof of Time Condition**: Prove that an event occurred before or after a specified date without revealing the event date.
- **Secure Hashing**: Uses SHA-256 hashing combined with a random salt to ensure secure and non-reversible proofs.


## Installation

You can install **ZkpSharp** via NuGet. Run the following command in your project directory:

```bash
dotnet add package ZkpSharp
```

## Setup

Before using the ZkpSharp library, you need to configure a secret key for HMAC (SHA-256) hashing. This key is required for generating and verifying proofs.

### Setting Up the HMAC Key in Code

Instead of using environment variables, you can pass the HMAC secret key directly when creating the ProofProvider. The key should be a 256-bit key (32 bytes) encoded in Base64.

Here’s an example of how to configure the HMAC key directly in your application:

```csharp
using ZkpSharp;
using ZkpSharp.Security;
using System;

class Program
{
    static void Main()
    {
        // Example base64-encoded HMAC secret key (256 bits / 32 bytes)
        string hmacSecretKeyBase64 = "your-base64-encoded-key-here";

        // Create an instance of ProofProvider with the provided HMAC key
        var proofProvider = new ProofProvider(hmacSecretKeyBase64);

        var zkp = new ZKP(proofProvider);
        var dateOfBirth = new DateTime(2000, 1, 1); // The user's date of birth

        // Generate proof of age
        var (proof, salt) = zkp.ProveAge(dateOfBirth);

        // Verify the proof of age
        bool isValid = zkp.VerifyAge(proof, dateOfBirth, salt);
        Console.WriteLine($"Age proof valid: {isValid}");
    }
}
```

## Usage

### Proof of Age

You can prove that you are over a certain age (e.g., 18 years old) without revealing your birthdate.

#### Example:

```csharp
using ZkpSharp;
using System;

class Program
{
    static void Main()
    {
        var zkp = new ZKP();
        var dateOfBirth = new DateTime(2000, 1, 1); // The user's date of birth

        // Generate proof of age
        var (proof, salt) = zkp.ProveAge(dateOfBirth);

        // Verify the proof of age
        bool isValid = zkp.VerifyAge(proof, dateOfBirth, salt);
        Console.WriteLine($"Age proof valid: {isValid}");
    }
}
```

### Proof of Balance

You can prove that you have enough balance to make a transaction without revealing your actual balance.

#### Example:

```csharp
using ZkpSharp;

class Program
{
    static void Main()
    {
        var zkp = new ZKP();
        double balance = 1000.0; // The user's balance
        double requestedAmount = 500.0; // The amount the user wants to prove they can pay

        // Generate proof of balance
        var (proof, salt) = zkp.ProveBalance(balance, requestedAmount);

        // Verify the proof of balance
        bool isValidBalance = zkp.VerifyBalance(proof, requestedAmount, salt, balance);
        Console.WriteLine($"Balance proof valid: {isValidBalance}");
    }
}
```

## Stellar Blockchain Integration

ZkpSharp provides production-ready integration with Stellar's Soroban smart contracts, enabling on-chain verification of zero-knowledge proofs.

### Features

- Full Soroban smart contract integration
- HMAC-SHA256 verification on-chain
- Support for all proof types (age, balance, membership, range, time)
- Batch proof verification
- Type-safe XDR encoding/decoding
- Comprehensive test suite

### Quick Start with Stellar

#### 1. Deploy the Soroban Contract

First, deploy the ZKP verifier contract to Stellar testnet:

```bash
cd contracts/stellar
soroban contract deploy \
  --wasm contracts/proof-balance/target/wasm32-unknown-unknown/release/proof_balance.wasm \
  --source <YOUR_SECRET_KEY> \
  --network testnet
```

See the [Deployment Guide](contracts/stellar/DEPLOYMENT.md) for detailed instructions.

#### 2. Configure Your Application

Set up the HMAC key environment variable:

```bash
export ZKP_HMAC_KEY="your-base64-encoded-key-here"
export ZKP_CONTRACT_ID="C..." # Your deployed contract ID
```

#### 3. Use ZkpSharp with Stellar

```csharp
using ZkpSharp;
using ZkpSharp.Core;
using ZkpSharp.Security;
using ZkpSharp.Integration.Stellar;

// Initialize ZKP provider
var hmacKey = Environment.GetEnvironmentVariable("ZKP_HMAC_KEY");
var proofProvider = new ProofProvider(hmacKey);
var zkp = new Zkp(proofProvider);

// Initialize Stellar blockchain client
var blockchain = new StellarBlockchain(
    "https://horizon-testnet.stellar.org",
    "https://soroban-testnet.stellar.org"
);

// Generate a proof (off-chain)
var balance = 1000.0;
var requestedAmount = 500.0;
var (proof, salt) = zkp.ProveBalance(balance, requestedAmount);

// Verify the proof on Stellar blockchain (on-chain)
var contractId = Environment.GetEnvironmentVariable("ZKP_CONTRACT_ID");
bool isValid = await blockchain.VerifyBalanceProof(
    contractId,
    proof,
    balance,
    requestedAmount,
    salt
);

Console.WriteLine($"Proof verified on blockchain: {isValid}");
```

### Advanced Usage

#### Custom Transaction Building

For more control over transactions:

```csharp
using StellarDotnetSdk;
using StellarDotnetSdk.Accounts;
using ZkpSharp.Integration.Stellar;

// Set up source account
var keypair = KeyPair.FromSecretSeed("S...");
var server = new Server("https://horizon-testnet.stellar.org");
var account = await server.Accounts.Account(keypair.AccountId);
var sourceAccount = new Account(keypair.AccountId, account.SequenceNumber);

// Build transaction
var txBuilder = SorobanTransactionBuilder.BuildVerifyProofTransaction(
    sourceAccount,
    Network.Test(),
    contractId,
    proof,
    "data-to-verify",
    salt,
    hmacKey
);

// Get XDR
var xdr = txBuilder.BuildXdr();
Console.WriteLine($"Transaction XDR: {xdr}");

// Submit to network (optional)
// var transaction = txBuilder.Build();
// var response = await server.SubmitTransaction(transaction);
```

#### Batch Verification

Verify multiple proofs at once for better efficiency:

```csharp
// Generate multiple proofs
var proofs = new List<string>();
var salts = new List<string>();
var data = new List<string>();

for (int i = 0; i < 3; i++)
{
    var value = $"value-{i}";
    var (proof, salt) = zkp.ProveMembership(value, new[] { value });
    proofs.Add(proof);
    salts.Add(salt);
    data.Add(value);
}

// Verify on blockchain using batch verification
// (Requires calling the verify_batch function in the contract)
```

#### Working with ScVal Types

ZkpSharp provides helpers for working with Soroban types:

```csharp
using ZkpSharp.Integration.Stellar;

// Encode data
var bytesScVal = SorobanHelper.EncodeBytesAsScVal(myBytes);
var stringScVal = SorobanHelper.EncodeStringAsScVal("Hello");
var boolScVal = SorobanHelper.EncodeBoolAsScVal(true);

// Decode data
var bytes = SorobanHelper.DecodeBytesFromScVal(scVal);
var text = SorobanHelper.DecodeStringFromScVal(scVal);
var flag = SorobanHelper.DecodeBoolFromScVal(scVal);

// Convert proofs and salts
var proofBytes = SorobanHelper.ConvertProofToBytes(base64Proof);
var saltBytes = SorobanHelper.ConvertSaltToBytes(base64Salt);
```

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        Your Application                      │
│                                                              │
│  ┌──────────────┐        ┌─────────────────────────────┐   │
│  │  ZkpSharp    │────────│  Generate Proof (Off-chain) │   │
│  │  Core        │        └─────────────────────────────┘   │
│  └──────────────┘                      │                    │
│         │                               │                    │
│         │                               ▼                    │
│         │                    ┌─────────────────────┐        │
│         │                    │  Proof + Salt       │        │
│         │                    └─────────────────────┘        │
│         │                               │                    │
│         ▼                               │                    │
│  ┌──────────────────────────┐          │                    │
│  │  StellarBlockchain       │◄─────────┘                    │
│  │  Integration             │                                │
│  └──────────────────────────┘                                │
│         │                                                    │
└─────────┼─────────────────────────────────────────────────┘
          │
          │  XDR Transaction
          ▼
┌─────────────────────────────────────────────────────────────┐
│                    Stellar Network                           │
│                                                              │
│  ┌────────────────┐        ┌──────────────────────────┐    │
│  │  Soroban RPC   │───────▶│  ZKP Verifier Contract   │    │
│  │  (Simulate)    │        │  (HMAC-SHA256)           │    │
│  └────────────────┘        └──────────────────────────┘    │
│                                       │                      │
│                                       ▼                      │
│                            ┌─────────────────┐              │
│                            │  Result: bool   │              │
│                            └─────────────────┘              │
└─────────────────────────────────────────────────────────────┘
```

### Use Cases

- DeFi: Prove sufficient balance without revealing exact amounts
- Identity: Verify age or membership without exposing personal data
- Gaming: Prove achievements or stats without revealing full game state
- Compliance: Demonstrate regulatory compliance while maintaining privacy
- Voting: Anonymous voting with eligibility verification

### Security Considerations

1. **HMAC Key Management**: Store your HMAC keys securely using:
   - Environment variables (development)
   - Azure Key Vault (production)
   - AWS Secrets Manager (production)
   - HashiCorp Vault (production)

2. **Contract Deployment**: Always verify contract source code before deployment

3. **Transaction Fees**: Soroban transactions require XLM for fees. Ensure your account is funded.

4. **Network Selection**: Use testnet for development, mainnet for production

5. **Salt Generation**: Never reuse salts. ZkpSharp generates cryptographically secure random salts automatically.

### Troubleshooting

**Problem**: `Contract ID not configured` error

**Solution**: Set the `ZKP_CONTRACT_ID` environment variable with your deployed contract ID.

**Problem**: `HMAC key not configured` error

**Solution**: Set the `ZKP_HMAC_KEY` environment variable with your base64-encoded 32-byte key.

**Problem**: Transaction simulation fails

**Solution**: Ensure the contract is deployed and the contract ID is correct. Check Soroban RPC endpoint is accessible.

## Contributing

We welcome contributions! To contribute:

1. Fork the repository.  
2. Create a new branch for your changes (`git checkout -b feature/your-feature`).  
3. Commit your changes (`git commit -m 'Add new feature'`).  
4. Push to your branch (`git push origin feature/your-feature`).  
5. Create a pull request.

Please ensure that your code passes all tests and adheres to the code style of the project.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Contact

For questions, issues, or suggestions, feel free to open an issue or contact Azimbek Sagynbaev at [sagynbaev6@gmail.com].

## Additional Resources

- Stellar Documentation: https://developers.stellar.org/
- Soroban Documentation: https://soroban.stellar.org/
- NuGet Package: https://www.nuget.org/packages/ZkpSharp