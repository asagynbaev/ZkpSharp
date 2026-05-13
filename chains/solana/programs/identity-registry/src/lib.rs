//! # IdentityRegistry
//!
//! Minimal on-chain anchor for the Tessera identity layer.
//!
//! Only commitment roots and revocation epochs live here. DID documents, attestations,
//! reputation scores, and any user-facing data stay off-chain. This program does not
//! verify proofs — verification is performed off-chain by holders of `Tessera.Proofs`.
//!
//! Instructions:
//! - `register_did`     : create a new DID anchor account (rent-paid by owner).
//! - `update_root`      : replace the attestation root for an existing DID (owner-signed).
//! - `bump_revocation`  : increment revocation_epoch (owner-signed; also callable by
//!                        registered issuer authority — left for v2).
//! - `register_issuer`  : admin-gated issuer registration (registry authority signs).
//!
//! Account layout is intentionally small. Adding fields later requires a versioned
//! migration; do not extend without bumping `account_version`.

use anchor_lang::prelude::*;

// Placeholder program ID. Before the first deploy run:
//   solana-keygen new -o target/deploy/identity_registry-keypair.json --no-bip39-passphrase
//   anchor keys sync                                          # writes the real pubkey here
// `anchor build` validates that this matches the on-disk keypair.
declare_id!("11111111111111111111111111111114");

#[program]
pub mod identity_registry {
    use super::*;

    pub fn register_did(
        ctx: Context<RegisterDid>,
        did_hash: [u8; 32],
        attestation_root: [u8; 32],
    ) -> Result<()> {
        let anchor = &mut ctx.accounts.did_anchor;
        anchor.account_version = ACCOUNT_VERSION;
        anchor.did_hash = did_hash;
        anchor.owner = ctx.accounts.owner.key();
        anchor.attestation_root = attestation_root;
        anchor.revocation_epoch = 0;
        anchor.created_at = Clock::get()?.unix_timestamp;
        anchor.updated_at = anchor.created_at;
        emit!(DidRegistered { did_hash, owner: anchor.owner });
        Ok(())
    }

    pub fn update_root(
        ctx: Context<UpdateDid>,
        new_root: [u8; 32],
    ) -> Result<()> {
        let anchor = &mut ctx.accounts.did_anchor;
        require_keys_eq!(anchor.owner, ctx.accounts.owner.key(), ErrorCode::NotOwner);
        anchor.attestation_root = new_root;
        anchor.updated_at = Clock::get()?.unix_timestamp;
        emit!(RootUpdated { did_hash: anchor.did_hash, new_root });
        Ok(())
    }

    pub fn bump_revocation(
        ctx: Context<UpdateDid>,
        reason: u8,
    ) -> Result<()> {
        let anchor = &mut ctx.accounts.did_anchor;
        require_keys_eq!(anchor.owner, ctx.accounts.owner.key(), ErrorCode::NotOwner);
        anchor.revocation_epoch = anchor
            .revocation_epoch
            .checked_add(1)
            .ok_or(ErrorCode::EpochOverflow)?;
        anchor.updated_at = Clock::get()?.unix_timestamp;
        emit!(RevocationBumped {
            did_hash: anchor.did_hash,
            new_epoch: anchor.revocation_epoch,
            reason,
        });
        Ok(())
    }

    pub fn register_issuer(
        ctx: Context<RegisterIssuer>,
        issuer_did_hash: [u8; 32],
        schema_uri: String,
    ) -> Result<()> {
        require!(schema_uri.len() <= MAX_SCHEMA_URI_LEN, ErrorCode::SchemaUriTooLong);
        let issuer = &mut ctx.accounts.issuer;
        issuer.account_version = ACCOUNT_VERSION;
        issuer.issuer_did_hash = issuer_did_hash;
        issuer.signing_key = ctx.accounts.signing_key.key();
        issuer.schema_uri = schema_uri;
        issuer.active = true;
        issuer.created_at = Clock::get()?.unix_timestamp;
        Ok(())
    }
}

// -- Account types ---------------------------------------------------------

pub const ACCOUNT_VERSION: u8 = 1;
pub const MAX_SCHEMA_URI_LEN: usize = 200;
pub const DID_ANCHOR_SEED: &[u8] = b"did";
pub const ISSUER_SEED: &[u8] = b"issuer";

#[account]
pub struct DidAnchor {
    pub account_version: u8,
    pub did_hash: [u8; 32],
    pub owner: Pubkey,
    pub attestation_root: [u8; 32],
    pub revocation_epoch: u64,
    pub created_at: i64,
    pub updated_at: i64,
}

impl DidAnchor {
    // discriminator + version + did_hash + owner + root + epoch + created + updated
    pub const LEN: usize = 8 + 1 + 32 + 32 + 32 + 8 + 8 + 8;
}

#[account]
pub struct Issuer {
    pub account_version: u8,
    pub issuer_did_hash: [u8; 32],
    pub signing_key: Pubkey,
    pub schema_uri: String,
    pub active: bool,
    pub created_at: i64,
}

impl Issuer {
    // 4-byte string length prefix + schema bytes + bool + version + hash + key + i64
    pub const LEN: usize = 8 + 1 + 32 + 32 + 4 + MAX_SCHEMA_URI_LEN + 1 + 8;
}

// -- Contexts --------------------------------------------------------------

#[derive(Accounts)]
#[instruction(did_hash: [u8; 32])]
pub struct RegisterDid<'info> {
    #[account(
        init,
        payer = owner,
        space = DidAnchor::LEN,
        seeds = [DID_ANCHOR_SEED, did_hash.as_ref()],
        bump,
    )]
    pub did_anchor: Account<'info, DidAnchor>,
    #[account(mut)]
    pub owner: Signer<'info>,
    pub system_program: Program<'info, System>,
}

#[derive(Accounts)]
pub struct UpdateDid<'info> {
    #[account(
        mut,
        seeds = [DID_ANCHOR_SEED, did_anchor.did_hash.as_ref()],
        bump,
    )]
    pub did_anchor: Account<'info, DidAnchor>,
    pub owner: Signer<'info>,
}

#[derive(Accounts)]
#[instruction(issuer_did_hash: [u8; 32])]
pub struct RegisterIssuer<'info> {
    #[account(
        init,
        payer = authority,
        space = Issuer::LEN,
        seeds = [ISSUER_SEED, issuer_did_hash.as_ref()],
        bump,
    )]
    pub issuer: Account<'info, Issuer>,
    /// CHECK: the public signing key of the issuer; signature verification happens
    /// off-chain when verifying attestations.
    pub signing_key: AccountInfo<'info>,
    #[account(mut)]
    pub authority: Signer<'info>,
    pub system_program: Program<'info, System>,
}

// -- Events ----------------------------------------------------------------

#[event]
pub struct DidRegistered { pub did_hash: [u8; 32], pub owner: Pubkey }

#[event]
pub struct RootUpdated { pub did_hash: [u8; 32], pub new_root: [u8; 32] }

#[event]
pub struct RevocationBumped { pub did_hash: [u8; 32], pub new_epoch: u64, pub reason: u8 }

// -- Errors ----------------------------------------------------------------

#[error_code]
pub enum ErrorCode {
    #[msg("Signer is not the registered owner of this DID anchor.")]
    NotOwner,
    #[msg("Revocation epoch overflow.")]
    EpochOverflow,
    #[msg("Schema URI exceeds maximum length.")]
    SchemaUriTooLong,
}
