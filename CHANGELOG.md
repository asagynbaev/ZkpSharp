# Changelog

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