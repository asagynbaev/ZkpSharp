#[cfg(test)]


///
/// 
/// 
/// Warning!
/// This is just a drafts, DO NOT USE it in production
/// 
/// 
/// 

mod test {
    use super::*;
    use soroban_sdk::testutils::Env as TestEnv;

    #[test]
    fn test_verify_balance() {
        let env = TestEnv::default();

        // Example valid proof and verifying key (same length).
        let proof = Bytes::from_slice(&env, &[1, 2, 3, 4, 5]);
        let verifying_key = Bytes::from_slice(&env, &[1, 2, 3, 4, 5]);

        // Perform verification.
        let result = ZkpBalanceVerifier::verify_balance(env, proof, verifying_key);

        // Assert that the verification succeeded.
        assert!(result);
    }

    #[test]
    fn test_invalid_proof() {
        let env = TestEnv::default();

        // Example invalid proof and verifying key (different lengths).
        let proof = Bytes::from_slice(&env, &[1, 2, 3, 4, 5]);
        let verifying_key = Bytes::from_slice(&env, &[6, 7, 8]);

        // Perform verification.
        let result = ZkpBalanceVerifier::verify_balance(env, proof, verifying_key);

        // Assert that the verification failed.
        assert!(!result);
    }
}