# Changelog

## [2.1.0] - 2026-03-04

### Added: Privacy SDK

Ready-to-use privacy primitives built on top of Bulletproofs, solving real-world problems instead of exposing raw cryptographic primitives.

- **`ConfidentialTransfer`** - Hide transfer amounts while proving solvency (paired Pedersen commitments + range proofs for amount and change)
- **`SealedBidAuction`** - Commit-reveal bidding with range proof verification and automatic winner determination
- **`PrivateVoting`** - Anonymous binary voting with Bulletproofs validity proofs and verifiable tally via ballot openings
- **`CredentialProof`** - Prove any numeric attribute meets a threshold (`ProveMinimum`) or falls within a range (`ProveRange`) without revealing the actual value. Supports labeled credentials (income, credit score, age, balance)
- Full serialization support for `TransferBundle` and `CredentialBundle`
- 26 new tests covering all Privacy SDK scenarios including tamper detection, forged openings, and serialization round-trips

---

## [2.0.0] - 2026-03-03

### Major Release: Real Bulletproofs from Scratch in Pure C#

This release replaces the previous wrapper-based ZKP implementation with a cryptographically sound Bulletproofs protocol implemented entirely from scratch in managed C#. No external cryptographic library dependencies.

### Added

#### Bulletproofs Cryptographic Core (all new, from scratch)
- **`FieldElement`** - Finite field arithmetic (mod p) for secp256k1
- **`Scalar`** - Scalar arithmetic (mod n) for secp256k1 curve order
- **`Point`** - Elliptic curve point operations using Jacobian coordinates (add, double, scalar multiply, SEC1 compress/decompress)
- **`Generators`** - Standard generator G, hash-to-curve derived H, and vector generators Gi/Hi for inner product arguments
- **`PedersenCommitment`** - Real Pedersen commitments: `C = v*G + r*H` on secp256k1
- **`Transcript`** - Fiat-Shamir heuristic via SHA-256 for non-interactive proof generation
- **`InnerProductProof`** - Recursive halving protocol for O(log n) proof size
- **`RangeProof`** - Full Bulletproofs range proof prover and verifier (64-bit range, ~690 byte proofs)

#### BulletproofsProvider (rewritten)
- `ProveRange()` / `VerifyRange()` - ZK range proofs backed by real Pedersen commitments
- `ProveAge()` / `VerifyAge()` - ZK age proofs without revealing birthdate
- `ProveBalance()` / `VerifyBalance()` - ZK balance sufficiency proofs
- `SerializeProof()` / `DeserializeProof()` - Compact Base64 serialization
- Implements `IZkProofProvider` interface (drop-in replacement)

#### Rust Contract Enhancements
- **`verify_zk_range_proof()`** - Structural validation of Bulletproofs: compressed point prefixes, IPA length, Fiat-Shamir transcript binding
- **`compute_transcript_binding()`** - Recomputes SHA-256 hash of domain separator, commitment V, range bounds, and proof points A/S to prevent replay and substitution attacks
- **`verify_zk_age_proof()`** - On-chain ZK age proof structural verification
- **`verify_zk_balance_proof()`** - On-chain ZK balance proof structural verification
- Extended error codes: `InvalidCommitment`, `InvalidRange`

#### Stellar Integration
- **`SorobanTransactionBuilder` class** - Full XDR construction for contract calls
  - `BuildVerifyProofTransaction()` - HMAC proof verification
  - `BuildVerifyBalanceProofTransaction()` - Balance proof verification
  - `BuildVerifyZkRangeProofTransaction()` - ZK range verification
  - `BuildVerifyZkAgeProofTransaction()` - ZK age verification
  - `BuildVerifyZkBalanceProofTransaction()` - ZK balance verification
- **`StellarBlockchain` enhancements**
  - `VerifyProofWithSourceAccount()` - Proof verification with account
  - `VerifyBalanceProofWithSourceAccount()` - Balance verification with account
  - Constructor now accepts `hmacKey` parameter
- **StrKey utilities** - Contract ID decoding

#### Test Coverage
- 44 new cryptographic tests (secp256k1 arithmetic, Pedersen commitments, range proofs, soundness, serialization round-trips)
- 10 new Bulletproofs integration tests
- 5 new SorobanTransactionBuilder tests
- 27 new core ZKP tests (Membership, Range, TimeCondition, edge cases)
- 4 new integration tests for ZK on-chain verification
- Total: 108 tests passing

#### Documentation
- **`STELLAR_REALITY_CHECK.md`** - Honest assessment of capabilities and on-chain limitations
- **`INTEGRATION_STATUS.md`** - Current feature and API status
- Updated README with Bulletproofs architecture and cryptography details
- Updated QUICKSTART with real ZKP usage examples

### Changed
- **Bulletproofs**: Replaced Secp256k1.ZKP wrapper with from-scratch implementation (FieldElement, Scalar, Point, Generators, Transcript, InnerProductProof, RangeProof)
- **BulletproofsProvider**: No longer requires a key parameter; uses real Pedersen commitments instead of HMAC-based fake commitments
- **Soroban contract**: Removed broken `verify_zk_response` function; `verify_zk_range_proof` now performs structural validation and Fiat-Shamir binding instead of fake "BP" header checks
- **On-chain verification model**: Full EC verification runs off-chain; contract performs structural validation and emits transcript binding hash for off-chain auditing (secp256k1 is not natively supported in Soroban)

### Removed
- Fake "BP" header-based proof validation in Rust contract
- Broken `verify_zk_response` function from Rust contract

### Breaking Changes
- `BulletproofsProvider` constructor no longer accepts a key parameter
- Proof format changed (real Bulletproofs binary format, not HMAC hashes)
- Rust contract requires redeployment for updated ZK verification functions
- Proof sizes increased (~690 bytes vs ~64 bytes) due to real cryptographic content

---

## [1.3.2] - 2025-12-03

### Security Fixes (Bugbot Review)

This release addresses critical security issues identified by Cursor Bugbot code review.

### Fixed

#### C# Library
- **SorobanHelper: Data truncation vulnerability** - `EncodeBytesAsScVal` and `EncodeStringAsScVal` now validate input length and throw `ArgumentException` for data exceeding 255 bytes, preventing silent data corruption
- **SorobanRpcClient: XDR boolean decode false positives** - Replaced unsafe `xdrBytes.Any(b => b == 0x01)` heuristic with proper SCVal format parsing using type discriminant validation

#### Rust Smart Contract
- **Balance comparison logic bug** - `verify_balance_proof` now uses proper numeric comparison via `parse_decimal_to_scaled()` instead of incorrect byte length comparison (`balance_data.len() >= required_amount_data.len()`)
- **Malformed input vulnerability** - `parse_decimal_to_scaled()` now returns `None` for malformed inputs like "-", ".", or empty bytes instead of `Some(0)`, preventing invalid balance data from being treated as zero
- **Test algorithm mismatch** - All tests now use `compute_test_hmac()` with proper HMAC-SHA256 (RFC 2104 with ipad/opad) instead of plain SHA256, matching production contract behavior

### Added
- `SorobanHelper.MaxBytesLength` constant (255) for explicit length limit documentation
- `parse_decimal_to_scaled()` function in Rust contract for accurate decimal number parsing with `has_digits` validation
- `compute_test_hmac()` helper in Rust tests for consistent HMAC computation
- `test_verify_balance_proof_insufficient()` test case for balance < required scenario
- `test_verify_balance_proof_malformed_input()` test case for malformed inputs like "-", "."

### Changed
- Balance verification now correctly handles edge cases like "99.0" vs "100.0"
- XDR boolean decoding now validates SCValType discriminant before extracting value

### Dependencies Updated
- `stellar-dotnet-sdk`: 13.0.0 → 14.0.1
- `stellar-dotnet-sdk-xdr`: 13.0.0 → 14.0.1
- `coverlet.collector`: 6.0.0 → 6.0.4
- `Microsoft.NET.Test.Sdk`: 17.8.0 → 17.12.0
- `xunit`: 2.9.2 → 2.9.3
- `xunit.runner.visualstudio`: 2.5.3 → 3.0.2
- GitHub Actions: `actions/checkout@v2` → `v4`, `actions/setup-dotnet@v1` → `v4`

---

## [1.3.1] - 2025-12-03

### Fixed
- Added `SorobanHelper` class with SCVal encoding/decoding utilities
- Fixed balance parsing with `CultureInfo.InvariantCulture` for consistent decimal handling
- Added `using StellarDotnetSdk.Accounts` for `KeyPair` class access in tests
- Improved test coverage for Stellar integration

---

## [1.3.0] - 2025-12-02

### Major Release: Production-Ready Stellar Integration

This release provides full production-ready integration with Stellar's Soroban smart contracts.

### Added

#### Stellar Blockchain Integration
- Production-ready Soroban smart contract for on-chain ZKP verification
  - Full HMAC-SHA256 verification implementation
  - Constant-time comparison to prevent timing attacks
  - Batch verification support for efficiency
  - Comprehensive error handling and event logging
  
- C# Integration Components
  - SorobanHelper: Type-safe XDR encoding/decoding utilities
  - SorobanTransactionBuilder: Fluent API for building Soroban transactions
  - Enhanced StellarBlockchain: Full IBlockchain implementation with Soroban support
  - Enhanced SorobanRpcClient: Proper XDR decoding and ScVal parsing

- Comprehensive Test Suite
  - Unit tests for all Rust contract functions
  - Integration tests for C# Stellar components
  - End-to-end workflow examples
  - Test coverage: 95%+

- Documentation
  - Complete deployment guide for Soroban contracts
  - Stellar integration examples in README
  - Architecture diagrams and use cases
  - Troubleshooting guides

#### Contract Features
- `verify_proof`: Universal proof verification function
- `verify_balance_proof`: Specialized balance verification with amount checking
- `verify_batch`: Efficient batch verification of multiple proofs
- Event emission for debugging and monitoring
- Input validation and security checks

### Changed
- Breaking: Updated StellarBlockchain constructor to accept optional Network parameter
- Breaking: VerifyProof now properly implements on-chain verification (was NotImplementedException)
- Improved HMAC key management with environment variable support
- Enhanced error messages and exception handling
- Optimized XDR encoding/decoding performance

### Fixed
- Resolved NotImplementedException in StellarBlockchain.VerifyProof
- Fixed XDR decoding issues in SorobanRpcClient
- Corrected ScVal type handling for boolean values
- Fixed timing attack vulnerability in proof comparison

### Security
- Implemented constant-time comparison in Rust contract
- Added comprehensive input validation
- Improved HMAC key security with environment variable support
- Added security best practices documentation

### Documentation
- Added comprehensive [Deployment Guide](contracts/stellar/DEPLOYMENT.md)
- Updated README with Stellar integration examples
- Added architecture diagrams
- Included troubleshooting section
- Added security considerations

### Performance
- Optimized WASM contract size (15-20 KB)
- Implemented efficient batch verification
- Reduced gas costs through optimization
- Improved transaction building performance

---

## [1.2.0] - 2025-12-02

### Added
- New method for range proofs (`ProveRange` and `VerifyRange`).
- Support for time-based proofs (`ProveTimestamp` and `VerifyTimestamp`).
- Support for proving set membership (`ProveSetMembership` and `VerifySetMembership`).

### Changed
- Refined HMAC implementation to retrieve the secret key from environment variables for enhanced security and flexibility.

### Fixed
- Bug in age verification logic that caused incorrect validation for dates close to the required age.

---

## [1.1.1] - 2025-01-03

### Added
- New method for range proofs (`ProveRange` and `VerifyRange`).
- Support for time-based proofs (`ProveTimestamp` and `VerifyTimestamp`).
- Support for proving set membership (`ProveSetMembership` and `VerifySetMembership`).

### Changed
- Refined HMAC implementation to retrieve the secret key from environment variables for enhanced security and flexibility.

### Fixed
- Bug in age verification logic that caused incorrect validation for dates close to the required age.

---

## [1.1.0] - 2025-01-01

### Added
- Salt generation improvements for stronger proofs.

### Changed
- Refactored `ZKP` class to improve performance and modularity.

### Fixed
- Fixed an issue where incorrect salt generation would sometimes lead to hash mismatches.

---

## [1.0.0] - 2025-01-01

### Initial release
- Proof of Age feature (`ProveAge` and `VerifyAge`).
- Proof of Balance feature (`ProveBalance` and `VerifyBalance`).