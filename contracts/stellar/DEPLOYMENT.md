# Soroban ZKP Contract Deployment Guide

This guide will walk you through deploying the ZKP Verifier smart contract to the Stellar network using Soroban.

## Prerequisites

Before you begin, ensure you have the following installed:

1. **Rust and Cargo** (latest stable version)
   ```bash
   curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh
   ```

2. **Soroban CLI**
   ```bash
   cargo install --locked soroban-cli --features opt
   ```

3. **wasm32 target**
   ```bash
   rustup target add wasm32-unknown-unknown
   ```

4. **Stellar account with XLM** for testnet or mainnet

## Step 1: Build the Contract

Navigate to the contract directory and build the WASM file:

```bash
cd contracts/stellar
cargo build --target wasm32-unknown-unknown --release --package proof-balance
```

The compiled WASM file will be located at:
```
target/wasm32-unknown-unknown/release/proof_balance.wasm
```

## Step 2: Optimize the WASM (Optional but Recommended)

Optimize the WASM file to reduce size and gas costs:

```bash
soroban contract optimize \
  --wasm target/wasm32-unknown-unknown/release/proof_balance.wasm
```

This creates an optimized version:
```
target/wasm32-unknown-unknown/release/proof_balance_optimized.wasm
```

## Step 3: Configure Stellar Network

### For Testnet

```bash
# Configure Soroban CLI for testnet
soroban network add \
  --global testnet \
  --rpc-url https://soroban-testnet.stellar.org \
  --network-passphrase "Test SDF Network ; September 2015"

# Generate or import your account
soroban keys generate --global alice --network testnet

# Fund your account from the friendbot
soroban keys fund alice --network testnet
```

### For Mainnet (Production)

```bash
# Configure Soroban CLI for mainnet
soroban network add \
  --global mainnet \
  --rpc-url https://soroban-rpc.mainnet.stellar.org \
  --network-passphrase "Public Global Stellar Network ; September 2015"

# Import your funded account
soroban keys add --global production-key --secret-key

# Verify your account has sufficient XLM
soroban keys address production-key
```

WARNING: Never commit or share your mainnet secret keys.

## Step 4: Deploy the Contract

### Deploy to Testnet

```bash
soroban contract deploy \
  --wasm target/wasm32-unknown-unknown/release/proof_balance.wasm \
  --source alice \
  --network testnet
```

The command will output your contract ID:
```
CXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
```

**Save this contract ID!** You'll need it to interact with the contract.

### Deploy to Mainnet

```bash
soroban contract deploy \
  --wasm target/wasm32-unknown-unknown/release/proof_balance_optimized.wasm \
  --source production-key \
  --network mainnet
```

## Step 5: Verify Deployment

Test that your contract is deployed and working:

```bash
# Set your contract ID as an environment variable
export CONTRACT_ID="CXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX"

# Prepare test data (example)
# Note: You'll need to encode these properly as XDR
soroban contract invoke \
  --id $CONTRACT_ID \
  --source alice \
  --network testnet \
  -- \
  verify_proof \
  --proof "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=" \
  --data "dGVzdC1kYXRh" \
  --salt "cmFuZG9tc2FsdDEyMzQ1Ng==" \
  --hmac_key "V0V3Mv4D1USxZYwWL4eG93m0JKdO9KbXQn0mhg+EXHc="
```

## Step 6: Configure Your C# Application

After deployment, configure your ZkpSharp application:

### Environment Variables

```bash
# Set the contract ID
export ZKP_CONTRACT_ID="CXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX"

# Set the HMAC key (same key used in the contract)
export ZKP_HMAC_KEY="V0V3Mv4D1USxZYwWL4eG93m0JKdO9KbXQn0mhg+EXHc="
```

### appsettings.json (Alternative)

```json
{
  "Stellar": {
    "ContractId": "CXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
    "HorizonUrl": "https://horizon-testnet.stellar.org",
    "SorobanRpcUrl": "https://soroban-testnet.stellar.org"
  },
  "ZkpSharp": {
    "HmacKey": "V0V3Mv4D1USxZYwWL4eG93m0JKdO9KbXQn0mhg+EXHc="
  }
}
```

## Step 7: Test with C#

Create a simple test application:

```csharp
using ZkpSharp.Core;
using ZkpSharp.Security;
using ZkpSharp.Integration.Stellar;

var hmacKey = Environment.GetEnvironmentVariable("ZKP_HMAC_KEY");
var contractId = Environment.GetEnvironmentVariable("ZKP_CONTRACT_ID");

var zkp = new Zkp(new ProofProvider(hmacKey));
var blockchain = new StellarBlockchain(
    "https://horizon-testnet.stellar.org",
    "https://soroban-testnet.stellar.org"
);

// Generate proof
var balance = 1000.0;
var requestedAmount = 500.0;
var (proof, salt) = zkp.ProveBalance(balance, requestedAmount);

// Verify on blockchain
var isValid = await blockchain.VerifyBalanceProof(
    contractId,
    proof,
    balance,
    requestedAmount,
    salt
);

Console.WriteLine($"Proof verified: {isValid}");
```

## Troubleshooting

### Problem: "account not found" error

**Solution**: Make sure your account is funded. For testnet, use:
```bash
soroban keys fund alice --network testnet
```

### Problem: "insufficient balance" error

**Solution**: Your account needs more XLM. The minimum amount for operations is typically 1 XLM plus transaction fees.

### Problem: Contract deployment fails

**Solution**: 
1. Check that the WASM file exists and is valid
2. Verify your network configuration
3. Ensure you have sufficient XLM for deployment fees
4. Try optimizing the WASM file first

### Problem: Contract invocation fails

**Solution**:
1. Verify the contract ID is correct
2. Check that function arguments are properly encoded
3. Ensure the contract is deployed to the correct network
4. Review Soroban RPC logs for detailed error messages

## Contract Upgrade

To upgrade an existing contract:

```bash
# Build new version
cargo build --target wasm32-unknown-unknown --release --package proof-balance

# Install the new WASM
soroban contract install \
  --wasm target/wasm32-unknown-unknown/release/proof_balance.wasm \
  --source alice \
  --network testnet
```

**Note**: Contract upgrades require proper authorization. Refer to Soroban documentation for upgrade patterns.

## Cost Estimation

### Testnet
- **Deployment**: Free (using friendbot-funded account)
- **Invocations**: Free (testnet has no fees)

### Mainnet
- **Deployment**: ~0.1-1 XLM (varies by contract size)
- **Invocations**: ~0.0001-0.001 XLM per call (varies by complexity)
- **Storage**: Ongoing fee based on contract state size

Always test thoroughly on testnet before deploying to mainnet.

## Security Best Practices

1. **Code Audit**: Have your contract audited before mainnet deployment
2. **Key Management**: Use hardware wallets or secure key management systems for mainnet keys
3. **Testing**: Run comprehensive tests on testnet
4. **Monitoring**: Set up monitoring for contract invocations and errors
5. **Upgrades**: Plan for contract upgrades and migrations
6. **Documentation**: Document all contract functions and their parameters

## Continuous Integration

### GitHub Actions Example

```yaml
name: Build and Test Soroban Contract

on: [push, pull_request]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Install Rust
        uses: actions-rs/toolchain@v1
        with:
          toolchain: stable
          target: wasm32-unknown-unknown
      
      - name: Install Soroban CLI
        run: cargo install --locked soroban-cli --features opt
      
      - name: Build Contract
        run: |
          cd contracts/stellar
          cargo build --target wasm32-unknown-unknown --release
      
      - name: Run Tests
        run: |
          cd contracts/stellar
          cargo test
      
      - name: Optimize WASM
        run: |
          soroban contract optimize \
            --wasm contracts/stellar/target/wasm32-unknown-unknown/release/proof_balance.wasm
```

## Resources

- [Soroban Documentation](https://soroban.stellar.org/docs)
- [Stellar Documentation](https://developers.stellar.org/)
- [Soroban CLI Reference](https://soroban.stellar.org/docs/reference/soroban-cli)
- [Stellar Discord](https://discord.gg/stellar) - Get help from the community
- [Soroban Quest](https://quest.stellar.org/soroban) - Interactive tutorials

## Support

If you encounter issues:

1. Check the [Soroban Discord](https://discord.gg/stellar) for community support
2. Review [Soroban examples](https://github.com/stellar/soroban-examples)
3. Open an issue in this repository
4. Contact the maintainers at sagynbaev6@gmail.com

## Next Steps

After successful deployment:

1. ✅ Test all contract functions thoroughly
2. ✅ Integrate with your C# application
3. ✅ Set up monitoring and logging
4. ✅ Document your integration for your team
5. ✅ Plan for contract upgrades and maintenance
6. ✅ Consider security audits for production use

For additional support, refer to the official Stellar documentation or community channels.

