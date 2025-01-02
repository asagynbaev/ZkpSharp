# Changelog

## [Unreleased]

### Added
- New method for range proofs (`ProveRange` and `VerifyRange`).
- Support for time-based proofs (`ProveTimestamp` and `VerifyTimestamp`).
- Support for proving set membership (`ProveSetMembership` and `VerifySetMembership`).

### Changed
- Updated HMAC implementation to use more secure random salts.

### Fixed
- Bug in age verification logic that caused incorrect validation for dates close to the required age.

---

## [1.1.0] - 2025-01-01

### Added
- Proof of Balance feature (`ProveBalance` and `VerifyBalance`).
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