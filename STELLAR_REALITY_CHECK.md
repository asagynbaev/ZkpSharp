# Stellar Integration Reality Check

This document provides an honest assessment of ZkpSharp's Stellar integration capabilities and limitations.

## What Works Today

### Off-Chain Proof Generation (Full Support)
- Generate HMAC-based privacy proofs in C#
- Generate real Bulletproofs ZK proofs on secp256k1 in pure C# (no external crypto dependencies)
- Pedersen commitments, inner product argument, Fiat-Shamir transcript
- Serialize/deserialize proofs for transmission
- All proof types: Age, Balance, Membership, Range, TimeCondition

### On-Chain Verification
- **HMAC proofs**: Full HMAC-SHA256 recomputation and constant-time comparison on-chain (complete verification)
- **Bulletproofs**: Structural validation only (point prefix checks, IPA length validation). Transcript binding hash is emitted as an event for off-chain auditing. **Full Bulletproofs verification (EC math) must be performed off-chain** using `BulletproofsProvider.VerifyRange()` -- Soroban does not natively support secp256k1 point arithmetic
- Batch HMAC verification of multiple proofs
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
| Speed | Fast (~1ms) | Slower (~200-500ms prove, ~50ms verify) |
| Proof Size | 32 bytes | ~690 bytes (64-bit range) |
| Zero-Knowledge | No (commitment scheme) | Yes (mathematically proven) |
| Curve | N/A (SHA-256) | secp256k1 |
| On-Chain Verification | Full HMAC recomputation | Structural + Fiat-Shamir binding (full EC off-chain) |
| Use Case | Basic privacy, fast checks | High-security scenarios, regulatory compliance |

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

1. **On-chain Bulletproofs verification**: Soroban does not natively support secp256k1 elliptic curve operations. Full Bulletproofs verification (point decompression, multi-exponentiation, inner product check) must happen off-chain. The on-chain contract performs structural validation and Fiat-Shamir transcript binding as a tamper-detection mechanism.

2. **Simulation source account**: `VerifyProof`, `VerifyBalanceProof`, and `VerifyZk*` (without `WithSourceAccount`) read **`ZKP_SOURCE_ACCOUNT`** for the funded G... account used in the simulated transaction envelope. Set it alongside `ZKP_HMAC_KEY` / `ZKP_CONTRACT_ID`, or call the `*WithSourceAccount` overloads explicitly.

3. **Transaction Signing**: ZkpSharp builds unsigned transactions. You must sign with your secret key before submission.

4. **Fee Estimation**: Use Soroban RPC `simulateTransaction` to get accurate fee estimates.

5. **Contract Deployment**: The Rust contract must be deployed separately using Stellar CLI.

6. **Network Fees**: On-chain verification requires XLM for transaction fees.

7. **Proof generation performance**: Bulletproofs proof generation involves multiple EC scalar multiplications and is slower than HMAC-based proofs. For latency-sensitive applications, consider caching proofs or generating them asynchronously.

## Security Considerations

1. **HMAC Key**: Never expose your HMAC key. Use environment variables or secure vaults.

2. **Salt Reuse**: Never reuse salts. ZkpSharp generates cryptographically secure random salts.

3. **Contract Verification**: Always verify the deployed contract code matches the source.

4. **Proof Freshness**: Consider adding timestamps to prevent replay attacks.

## Next Steps

1. Deploy the ZkpVerifier contract to testnet
2. Set environment variables: `ZKP_HMAC_KEY`, `ZKP_CONTRACT_ID`, `ZKP_SOURCE_ACCOUNT` (funded account on that network; optional if you only use `*WithSourceAccount` in code)
3. Run integration tests to verify setup (`dotnet test --filter "FullyQualifiedName~StellarTestnetSmokeTests"` when the contract is deployed)
4. Move to mainnet when ready

See [INTEGRATION_STATUS.md](INTEGRATION_STATUS.md) for current feature status.
