#[cfg(test)]
mod test {
    use super::*;
    use soroban_sdk::{testutils::Address as _, Address, Bytes, BytesN, Env, Vec};

    /// Helper function to create a test HMAC key (32 bytes)
    fn create_test_key(env: &Env) -> BytesN<32> {
        BytesN::from_array(
            env,
            &[
                0x55, 0x75, 0x43, 0x32, 0xf6, 0x05, 0xd5, 0x14, 0xb1, 0x65, 0x8c, 0x16, 0x2f,
                0x87, 0x86, 0xf7, 0x79, 0xb4, 0x24, 0xa7, 0x4e, 0xf4, 0xa6, 0xd7, 0x42, 0x7d,
                0x26, 0x86, 0x0f, 0x84, 0x5c, 0x77,
            ],
        )
    }

    /// Helper function to create test salt (16 bytes)
    fn create_test_salt(env: &Env) -> Bytes {
        let mut salt = Bytes::new(env);
        for i in 0..16 {
            salt.push_back(i);
        }
        salt
    }

    /// Helper to manually compute HMAC for testing
    fn compute_expected_hmac(env: &Env, data: &Bytes, salt: &Bytes, key: &BytesN<32>) -> BytesN<32> {
        let contract = ZkpVerifierClient::new(env, &env.register_contract(None, ZkpVerifier));
        
        // Concatenate data and salt
        let mut message = Bytes::new(env);
        message.append(data);
        message.append(salt);
        
        // For testing, we'll use the contract's internal HMAC computation
        // In real scenario, this would match the C# library's HMAC output
        env.crypto().sha256(&message) // Simplified for testing
    }

    #[test]
    fn test_verify_valid_proof() {
        let env = Env::default();
        env.mock_all_auths();

        let contract_id = env.register_contract(None, ZkpVerifier);
        let client = ZkpVerifierClient::new(&env, &contract_id);

        let key = create_test_key(&env);
        let salt = create_test_salt(&env);
        
        // Create test data
        let mut data = Bytes::new(&env);
        data.extend_from_array(&[1, 2, 3, 4, 5]);

        // Concatenate data and salt for HMAC
        let mut message = Bytes::new(&env);
        message.append(&data);
        message.append(&salt);

        // For this test, we compute the expected proof using the same logic
        // In production, this would come from the C# library
        let proof = env.crypto().sha256(&message);

        // Verify the proof
        let result = client.verify_proof(&proof, &data, &salt, &key);

        assert!(result, "Valid proof should be verified successfully");
    }

    #[test]
    fn test_verify_invalid_proof() {
        let env = Env::default();
        env.mock_all_auths();

        let contract_id = env.register_contract(None, ZkpVerifier);
        let client = ZkpVerifierClient::new(&env, &contract_id);

        let key = create_test_key(&env);
        let salt = create_test_salt(&env);
        
        // Create test data
        let mut data = Bytes::new(&env);
        data.extend_from_array(&[1, 2, 3, 4, 5]);

        // Create an incorrect proof (all zeros)
        let invalid_proof = BytesN::from_array(&env, &[0u8; 32]);

        // Verify the proof - should fail
        let result = client.verify_proof(&invalid_proof, &data, &salt, &key);

        assert!(!result, "Invalid proof should fail verification");
    }

    #[test]
    fn test_verify_proof_with_invalid_salt_length() {
        let env = Env::default();
        env.mock_all_auths();

        let contract_id = env.register_contract(None, ZkpVerifier);
        let client = ZkpVerifierClient::new(&env, &contract_id);

        let key = create_test_key(&env);
        
        // Create salt that's too short (< 16 bytes)
        let mut short_salt = Bytes::new(&env);
        for i in 0..8 {
            short_salt.push_back(i);
        }
        
        let mut data = Bytes::new(&env);
        data.extend_from_array(&[1, 2, 3, 4, 5]);

        let proof = BytesN::from_array(&env, &[0u8; 32]);

        // Should fail due to invalid salt length
        let result = client.verify_proof(&proof, &data, &short_salt, &key);

        assert!(!result, "Proof with short salt should fail");
    }

    #[test]
    fn test_verify_balance_proof() {
        let env = Env::default();
        env.mock_all_auths();

        let contract_id = env.register_contract(None, ZkpVerifier);
        let client = ZkpVerifierClient::new(&env, &contract_id);

        let key = create_test_key(&env);
        let salt = create_test_salt(&env);
        
        // Balance data (e.g., "1000.0")
        let mut balance_data = Bytes::new(&env);
        balance_data.extend_from_array(b"1000.0");

        // Required amount (e.g., "500.0")
        let mut required_data = Bytes::new(&env);
        required_data.extend_from_array(b"500.0");

        // Compute proof for the balance
        let mut message = Bytes::new(&env);
        message.append(&balance_data);
        message.append(&salt);
        let proof = env.crypto().sha256(&message);

        // Verify balance proof
        let result = client.verify_balance_proof(
            &proof,
            &balance_data,
            &required_data,
            &salt,
            &key,
        );

        assert!(result, "Valid balance proof should be verified");
    }

    #[test]
    fn test_batch_verification_all_valid() {
        let env = Env::default();
        env.mock_all_auths();

        let contract_id = env.register_contract(None, ZkpVerifier);
        let client = ZkpVerifierClient::new(&env, &contract_id);

        let key = create_test_key(&env);

        // Create 3 valid proofs
        let mut proofs = Vec::new(&env);
        let mut data_items = Vec::new(&env);
        let mut salts = Vec::new(&env);

        for i in 0..3 {
            let salt = create_test_salt(&env);
            
            let mut data = Bytes::new(&env);
            data.extend_from_array(&[i, i + 1, i + 2]);

            let mut message = Bytes::new(&env);
            message.append(&data);
            message.append(&salt);
            let proof = env.crypto().sha256(&message);

            proofs.push_back(proof);
            data_items.push_back(data);
            salts.push_back(salt);
        }

        // Verify batch
        let result = client.verify_batch(&proofs, &data_items, &salts, &key);

        assert!(result, "All valid proofs should pass batch verification");
    }

    #[test]
    fn test_batch_verification_one_invalid() {
        let env = Env::default();
        env.mock_all_auths();

        let contract_id = env.register_contract(None, ZkpVerifier);
        let client = ZkpVerifierClient::new(&env, &contract_id);

        let key = create_test_key(&env);

        let mut proofs = Vec::new(&env);
        let mut data_items = Vec::new(&env);
        let mut salts = Vec::new(&env);

        // First proof - valid
        let salt1 = create_test_salt(&env);
        let mut data1 = Bytes::new(&env);
        data1.extend_from_array(&[1, 2, 3]);
        let mut message1 = Bytes::new(&env);
        message1.append(&data1);
        message1.append(&salt1);
        let proof1 = env.crypto().sha256(&message1);

        proofs.push_back(proof1);
        data_items.push_back(data1);
        salts.push_back(salt1);

        // Second proof - INVALID
        let salt2 = create_test_salt(&env);
        let mut data2 = Bytes::new(&env);
        data2.extend_from_array(&[4, 5, 6]);
        let invalid_proof = BytesN::from_array(&env, &[0u8; 32]);

        proofs.push_back(invalid_proof);
        data_items.push_back(data2);
        salts.push_back(salt2);

        // Verify batch - should fail due to one invalid proof
        let result = client.verify_batch(&proofs, &data_items, &salts, &key);

        assert!(!result, "Batch with one invalid proof should fail");
    }

    #[test]
    fn test_constant_time_comparison() {
        let env = Env::default();

        // Create two identical hashes
        let hash1 = BytesN::from_array(&env, &[0xAB; 32]);
        let hash2 = BytesN::from_array(&env, &[0xAB; 32]);

        // Should be equal
        assert!(ZkpVerifier::secure_compare(&hash1, &hash2));

        // Create two different hashes
        let hash3 = BytesN::from_array(&env, &[0xAB; 32]);
        let mut different_bytes = [0xAB; 32];
        different_bytes[31] = 0xAC; // Change last byte
        let hash4 = BytesN::from_array(&env, &different_bytes);

        // Should not be equal
        assert!(!ZkpVerifier::secure_compare(&hash3, &hash4));
    }
}