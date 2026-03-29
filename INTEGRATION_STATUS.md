# ZkpSharp Integration Status

Current version: 2.2.0

## Feature Status

### .NET Library

| Component | Status | Notes |
|-----------|--------|-------|
| `Zkp` (HMAC-based proofs) | Complete | Age, Balance, Membership, Range, TimeCondition |
| `BulletproofsProvider` | Complete | Real Bulletproofs on secp256k1 (from scratch, pure C#) |
| `IZkProofProvider` interface | Complete | Abstraction for ZK providers |
| `ProofProvider` | Complete | HMAC-SHA256 implementation |
| `StellarBlockchain` | Complete | Horizon + Soroban RPC integration |
| `SorobanTransactionBuilder` | Complete | XDR construction for all proof types |
| `SorobanHelper` | Complete | SCVal encoding/decoding |
| `SorobanRpcClient` | Complete | JSON-RPC client for Soroban |
| `ConfidentialTransfer` | Complete | Hidden amounts + solvency proofs |
| `SealedBidAuction` | Complete | Commit-reveal bidding with range proofs |
| `PrivateVoting` | Complete | Anonymous binary voting + verifiable tally |
| `CredentialProof` | Complete | Threshold / range proofs for any numeric attribute |

### Rust Soroban Contract

| Function | Status | Notes |
|----------|--------|-------|
| `verify_proof` | Complete | HMAC-SHA256 verification |
| `verify_balance_proof` | Complete | With numeric comparison |
| `verify_batch` | Complete | Multiple proofs at once |
| `verify_zk_range_proof` | Complete | Bulletproofs structural validation + Fiat-Shamir binding |
| `verify_zk_age_proof` | Complete | Range proof wrapper |
| `verify_zk_balance_proof` | Complete | Range proof wrapper |

### Dependencies

| Package | Version | Status |
|---------|---------|--------|
| `stellar-dotnet-sdk` | 14.0.1 | Latest |
| `stellar-dotnet-sdk-xdr` | 14.0.1 | Latest |
| `soroban-sdk` (Rust) | 25.3 | Pinned in workspace `Cargo.toml` |

### Test Coverage

| Test Category | Count | Status |
|---------------|-------|--------|
| Age proofs | 6 | Passing |
| Balance proofs | 4 | Passing |
| Membership proofs | 6 | Passing |
| Range proofs | 8 | Passing |
| TimeCondition proofs | 8 | Passing |
| Edge cases | 6 | Passing |
| Bulletproofs (secp256k1 core + proofs) | 44 | Passing |
| Privacy SDK (CT, Auction, Voting, Credential) | 26 | Passing |
| SorobanTransactionBuilder | 5 | Passing |
| SorobanHelper | 7 | Passing |
| Stellar (unit + Horizon smoke) | 4 | Passing |
| Stellar testnet contract smoke | 7 | Skipped unless `ZKP_CONTRACT_ID` set |

## API Stability

| API | Stability |
|-----|-----------|
| `Zkp` class | Stable |
| `IZkProofProvider` | Stable |
| `BulletproofsProvider` | Stable |
| `StellarBlockchain` | Stable |
| `SorobanTransactionBuilder` | Stable |
| `SorobanHelper` | Stable |

## Breaking Changes in 2.2.0

1. **`ZKP_SOURCE_ACCOUNT`**: `StellarBlockchain.VerifyProof`, `VerifyBalanceProof`, and `VerifyZk*` now require this environment variable (funded `G...` on the same network as Horizon) unless you call the `*WithSourceAccount` overloads instead.

## Breaking Changes in 2.0.0

1. **`BulletproofsProvider` constructor**: No longer accepts a key parameter (uses real Pedersen commitments, not HMAC).
2. **Proof format**: Real Bulletproofs binary format (~690 bytes) replaces HMAC-based fake proofs (~64 bytes).
3. **Soroban contract**: `verify_zk_range_proof` performs structural validation and Fiat-Shamir binding; broken `verify_zk_response` removed.
4. **Contract redeployment required**: Updated ZK verification functions.

## Migration Guide

### From 1.x to 2.0.0

1. Update NuGet packages:
```bash
dotnet add package ZkpSharp --version 2.0.0
```

2. Update BulletproofsProvider usage:
```csharp
// Old (1.x) -- key parameter removed
// var zkProvider = new BulletproofsProvider(hmacKey);

// New (2.0.0) -- real Bulletproofs, no key needed
var zkProvider = new BulletproofsProvider();
var (proof, commitment) = zkProvider.ProveRange(42, 0, 100);
bool valid = zkProvider.VerifyRange(proof, commitment, 0, 100);
```

3. Recompile and redeploy Rust contract:
```bash
cd contracts/stellar
cargo build --release --target wasm32-unknown-unknown
```

4. HMAC-based proofs (`Zkp` class) are unchanged and continue to work.

## Known Issues

1. **Rust tests**: Run with `cargo test` in the contract directory.
2. **On-chain smoke tests**: Set `ZKP_CONTRACT_ID`, `ZKP_SOURCE_ACCOUNT`, and `ZKP_HMAC_KEY`, then `dotnet test --filter "FullyQualifiedName~StellarTestnetSmokeTests"`.

## Roadmap

### 2.x
- [ ] Aggregated range proofs (multiple values in a single proof)
- [ ] Batch verification (verify N proofs faster than N individual verifications)
- [ ] Performance optimization (precomputed multiscalar multiplication)

### 3.0.0 (Future)
- [ ] Groth16 zkSNARK support
- [ ] Recursive proof composition
- [ ] Multi-chain support (Ethereum, Solana)

## Contributing

See the [Contributing](README.md#contributing) section in the README.

## Support

- Issues: https://github.com/asagynbaev/ZkpSharp/issues
- Discussions: https://github.com/asagynbaev/ZkpSharp/discussions
