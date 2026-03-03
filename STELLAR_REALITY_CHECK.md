# Stellar Integration Reality Check

This document provides an honest assessment of ZkpSharp's Stellar integration capabilities and limitations.

## What Works Today

### Off-Chain Proof Generation (Full Support)
- Generate HMAC-based privacy proofs in C#
- Generate Bulletproofs-based ZK proofs in C#
- Serialize/deserialize proofs for transmission
- All proof types: Age, Balance, Membership, Range, TimeCondition

### On-Chain Verification (Full Support with Soroban)
- Rust Soroban smart contract for HMAC-SHA256 verification
- Rust Soroban smart contract for BLS12-381 ZK verification
- Batch verification of multiple proofs
- Proper numeric balance comparison (not byte-length comparison)

### Transaction Building (Production-Ready)
- `SorobanTransactionBuilder` creates proper XDR for:
  - `verify_proof` - Basic HMAC proof verification
  - `verify_balance_proof` - Balance proof with numeric comparison
  - `verify_zk_range_proof` - True ZK range proof verification
  - `verify_zk_age_proof` - ZK age verification
  - `verify_zk_balance_proof` - ZK balance verification

### Stellar Network Integration
- Horizon API integration for account queries
- Soroban RPC integration for contract simulation
- Proper SCVal encoding/decoding via `SorobanHelper`
- XDR boolean result parsing

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     .NET Application                        │
│                                                             │
│  ┌─────────────────┐    ┌──────────────────────────────┐    │
│  │ Zkp             │    │ BulletproofsProvider         │    │
│  │ (HMAC proofs)   │    │ (True ZK proofs)             │    │
│  └────────┬────────┘    └──────────────┬───────────────┘    │
│           │                            │                    │
│           └──────────┬─────────────────┘                    │
│                      │                                      │
│           ┌──────────▼───────────┐                          │
│           │ StellarBlockchain    │                          │
│           │ + SorobanTransactionBuilder                     │
│           └──────────┬───────────┘                          │
└──────────────────────┼──────────────────────────────────────┘
                       │
                       │ InvokeHostFunctionOp (XDR)
                       │
┌──────────────────────▼───────────────────────────────────────┐
│                    Stellar Network                           │
│                                                              │
│  ┌─────────────────┐         ┌────────────────────────────┐  │
│  │ Soroban RPC     │         │ ZkpVerifier Contract       │  │ 
│  │ (Simulation)    │─────────│ ├── verify_proof           │  │
│  └─────────────────┘         │ ├── verify_balance_proof   │  │
│                              │ ├── verify_zk_range_proof  │  │
│                              │ ├── verify_zk_age_proof    │  │
│                              │ └── verify_batch           │  │
│                              └────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
```

## Proof Types Comparison

| Feature | HMAC-based (Privacy Proofs) | Bulletproofs (True ZKP) |
|---------|----------------------------|-------------------------|
| Speed | Fast (~1ms) | Slower (~100ms) |
| Proof Size | 32 bytes | ~600 bytes |
| Zero-Knowledge | No (commitment scheme) | Yes (mathematically proven) |
| On-Chain Cost | Low | Higher |
| Use Case | Basic privacy | High-security scenarios |

## Recommended Approach

### For Basic Privacy Needs
Use HMAC-based proofs:
```csharp
var zkp = new Zkp(new ProofProvider(hmacKey));
var (proof, salt) = zkp.ProveAge(birthDate);
```

### For True Zero-Knowledge
Use BulletproofsProvider:
```csharp
var zkProvider = new BulletproofsProvider();
var (proof, commitment) = zkProvider.ProveAge(birthDate, minAge: 18);
```

### For On-Chain Verification
```csharp
var blockchain = new StellarBlockchain(horizonUrl, sorobanRpcUrl, hmacKey: hmacKey);
var isValid = await blockchain.VerifyProofWithSourceAccount(
    sourceAccountId, contractId, proof, salt, data);
```

## Limitations

1. **Transaction Signing**: ZkpSharp builds unsigned transactions. You must sign with your secret key before submission.

2. **Fee Estimation**: Use Soroban RPC `simulateTransaction` to get accurate fee estimates.

3. **Contract Deployment**: The Rust contract must be deployed separately using Stellar CLI.

4. **Network Fees**: On-chain verification requires XLM for transaction fees.

## Security Considerations

1. **HMAC Key**: Never expose your HMAC key. Use environment variables or secure vaults.

2. **Salt Reuse**: Never reuse salts. ZkpSharp generates cryptographically secure random salts.

3. **Contract Verification**: Always verify the deployed contract code matches the source.

4. **Proof Freshness**: Consider adding timestamps to prevent replay attacks.

## Next Steps

1. Deploy the ZkpVerifier contract to testnet
2. Set environment variables: `ZKP_HMAC_KEY`, `ZKP_CONTRACT_ID`
3. Run integration tests to verify setup
4. Move to mainnet when ready

See [INTEGRATION_STATUS.md](INTEGRATION_STATUS.md) for current feature status.
