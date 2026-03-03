# ZkpSharp Integration Status

Current version: 1.4.0-preview

## Feature Status

### .NET Library

| Component | Status | Notes |
|-----------|--------|-------|
| `Zkp` (HMAC-based proofs) | Complete | Age, Balance, Membership, Range, TimeCondition |
| `BulletproofsProvider` | Complete | True ZK Range/Age/Balance proofs |
| `IZkProofProvider` interface | Complete | Abstraction for ZK providers |
| `ProofProvider` | Complete | HMAC-SHA256 implementation |
| `StellarBlockchain` | Complete | Horizon + Soroban RPC integration |
| `SorobanTransactionBuilder` | Complete | XDR construction for all proof types |
| `SorobanHelper` | Complete | SCVal encoding/decoding |
| `SorobanRpcClient` | Complete | JSON-RPC client for Soroban |

### Rust Soroban Contract

| Function | Status | Notes |
|----------|--------|-------|
| `verify_proof` | Complete | HMAC-SHA256 verification |
| `verify_balance_proof` | Complete | With numeric comparison |
| `verify_batch` | Complete | Multiple proofs at once |
| `verify_zk_range_proof` | Complete | BLS12-381 based |
| `verify_zk_age_proof` | Complete | Range proof wrapper |
| `verify_zk_balance_proof` | Complete | Range proof wrapper |

### Dependencies

| Package | Version | Status |
|---------|---------|--------|
| `stellar-dotnet-sdk` | 14.0.1 | Latest |
| `stellar-dotnet-sdk-xdr` | 14.0.1 | Latest |
| `soroban-sdk` (Rust) | 25 | Latest |

### Test Coverage

| Test Category | Count | Status |
|---------------|-------|--------|
| Age proofs | 6 | Passing |
| Balance proofs | 4 | Passing |
| Membership proofs | 6 | Passing |
| Range proofs | 8 | Passing |
| TimeCondition proofs | 8 | Passing |
| Edge cases | 6 | Passing |
| Bulletproofs | 10 | Passing |
| SorobanTransactionBuilder | 5 | Passing |
| SorobanHelper | 7 | Passing |
| Stellar integration | 4 | Requires config |

## API Stability

| API | Stability |
|-----|-----------|
| `Zkp` class | Stable |
| `IZkProofProvider` | Stable |
| `BulletproofsProvider` | Stable |
| `StellarBlockchain` | Stable |
| `SorobanTransactionBuilder` | Stable |
| `SorobanHelper` | Stable |

## Breaking Changes in 1.4.0

1. **Soroban SDK 25**: Contract requires recompilation with new SDK.
2. **New ZK methods**: Added `verify_zk_*` functions to contract.
3. **SorobanTransactionBuilder**: New class replaces manual XDR construction.

## Migration Guide

### From 1.3.x to 1.4.0

1. Update NuGet packages:
```bash
dotnet add package ZkpSharp --version 1.4.0
```

2. Recompile Rust contract with Soroban SDK 25:
```bash
cd contracts/stellar
cargo update
cargo build --release --target wasm32-unknown-unknown
```

3. Redeploy contract to network.

4. (Optional) Use new BulletproofsProvider for true ZK:
```csharp
// Old (still works)
var zkp = new Zkp(new ProofProvider(hmacKey));

// New (true ZK)
var zkProvider = new BulletproofsProvider();
```

## Known Issues

1. **Rust tests**: Run with `cargo test` in the contract directory.
2. **Integration tests**: Require deployed contract and environment variables.

## Roadmap

### 1.4.1 (Next)
- [ ] Add BN254 support alongside BLS12-381
- [ ] Improve proof serialization efficiency
- [ ] Add more comprehensive error messages

### 1.5.0 (Future)
- [ ] Add zkSNARK support (Groth16)
- [ ] Add recursive proof composition
- [ ] Multi-chain support (Ethereum, Solana)

## Contributing

See the [Contributing](README.md#contributing) section in the README.

## Support

- Issues: https://github.com/asagynbaev/ZkpSharp/issues
- Discussions: https://github.com/asagynbaev/ZkpSharp/discussions
