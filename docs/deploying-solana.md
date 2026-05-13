# Deploying the identity-registry Anchor program

End-to-end guide for deploying [`chains/solana/programs/identity-registry/`](../chains/solana/programs/identity-registry/) to a Solana cluster and exercising the C# adapter against it.

## Prerequisites

- **Rust toolchain**: `rustup` with stable Rust ≥ 1.79.
- **Solana CLI**: `solana --version` ≥ 1.18 ([install instructions](https://docs.solanalabs.com/cli/install)).
- **Anchor CLI**: `anchor --version` ≥ 0.30 (`avm install 0.30.1 && avm use 0.30.1`).
- **.NET 8 SDK** (only required for the smoke tests).

## One-time setup

### 1. Point the Solana CLI at devnet and fund a keypair

```bash
solana config set --url https://api.devnet.solana.com
solana-keygen new --outfile ~/.config/solana/zkp-devnet.json --no-bip39-passphrase
solana config set --keypair ~/.config/solana/zkp-devnet.json
solana airdrop 2                                   # 2 SOL is plenty for several deploys
solana balance                                     # verify it landed
```

> Devnet faucets rate-limit; retry after a few minutes if the airdrop fails.

### 2. Generate the program keypair and sync the on-chain ID

The `declare_id!` macro in [`src/lib.rs`](../chains/solana/programs/identity-registry/src/lib.rs) ships with a placeholder. Replace it with the pubkey of a fresh keypair before the first deploy:

```bash
cd chains/solana
mkdir -p target/deploy
solana-keygen new -o target/deploy/identity_registry-keypair.json --no-bip39-passphrase
anchor keys sync                                   # rewrites declare_id! in src/lib.rs
```

`anchor keys sync` opens the source file and updates `declare_id!("...")` to the pubkey derived from `target/deploy/identity_registry-keypair.json`. Commit the resulting change.

### 3. Build and deploy

```bash
cd chains/solana
anchor build
anchor deploy --provider.cluster devnet
```

`anchor deploy` prints the program ID. Verify it matches `declare_id!`:

```bash
solana address -k target/deploy/identity_registry-keypair.json
```

## Wiring the C# adapter

Once deployed, configure the `SolanaChainAnchor`:

```csharp
var anchor = new SolanaChainAnchor(
    rpcUrl:       "https://api.devnet.solana.com",
    programId:    "<the pubkey printed by anchor deploy>",
    payerKeypair: File.ReadAllBytes("/path/to/64-byte-keypair.bin"));
```

The payer keypair is 64 bytes: 32-byte private seed concatenated with the 32-byte public key. To convert a Solana CLI JSON keypair (which stores the same 64 bytes as a JSON array) into a byte array, parse it with `System.Text.Json`:

```csharp
var bytes = JsonSerializer.Deserialize<byte[]>(File.ReadAllText(keypairPath));
```

## Running the smoke tests

The devnet smoke tests in [`src/ZkpSharp.Chains.Solana.Tests/Smoke/`](../src/ZkpSharp.Chains.Solana.Tests/Smoke/) are gated by three environment variables and skipped otherwise — they will not run in CI by default.

```bash
export ZKP_SOLANA_RPC="https://api.devnet.solana.com"
export ZKP_SOLANA_PROGRAM_ID="<deployed program pubkey>"
export ZKP_SOLANA_PAYER_KEYPAIR="$HOME/.config/solana/zkp-devnet.json"

dotnet test src/ZkpSharp.Chains.Solana.Tests \
    --filter "FullyQualifiedName~Smoke.SolanaDevnetSmokeTests"
```

The tests exercise the full anchor flow:

| Test | Confirms |
|---|---|
| `AnchorRoot_RegistersFreshDid` | `register_did` writes a new PDA, then `get_anchor` reads it back. |
| `AnchorRoot_TwiceOnSameDid_UpdatesRoot` | Second call routes through `update_root` instead of duplicate-creating. |
| `BumpRevocation_IncrementsEpoch` | `bump_revocation` advances `revocation_epoch` monotonically. |
| `GetAnchor_UnknownDid_ReturnsNull` | RPC returns no account for a never-anchored DID. |
| `IsRevokedSince_TracksEpoch` | Convenience comparison against the on-chain epoch. |

Each test uses a freshly randomised DID so PDAs do not collide across runs. Cost per full pass is a few thousand lamports.

## Re-deploying after code changes

After any change to `src/lib.rs`:

```bash
cd chains/solana
anchor build
anchor upgrade target/deploy/identity_registry.so --program-id <programId>
```

The program ID stays stable across upgrades; only the bytecode changes. Account data on existing PDAs is preserved.

## Cleaning up devnet artefacts

Devnet state is wiped periodically by Solana, so cleanup is rarely necessary. If you want to manually close a program (e.g. to reclaim rent):

```bash
solana program close <programId> --bypass-warning
```

> Closing is irreversible. Do it only on devnet or programs you intend to retire.
