namespace ZkpSharp.Interfaces
{
    /// <summary>
    /// Provides cryptographic operations for generating and verifying true Zero-Knowledge Proofs.
    /// Unlike HMAC-based commitment schemes, these proofs mathematically guarantee that
    /// the verifier learns nothing about the secret value except that it satisfies the stated condition.
    /// </summary>
    public interface IZkProofProvider
    {
        /// <summary>
        /// Generates a Zero-Knowledge Range Proof proving that a value lies within [min, max]
        /// without revealing the actual value.
        /// </summary>
        /// <param name="value">The secret value to prove.</param>
        /// <param name="min">The minimum allowed value (inclusive).</param>
        /// <param name="max">The maximum allowed value (inclusive).</param>
        /// <returns>
        /// A tuple containing:
        /// - proof: The ZK proof bytes
        /// - commitment: The Pedersen commitment to the value
        /// </returns>
        (byte[] proof, byte[] commitment) ProveRange(long value, long min, long max);

        /// <summary>
        /// Verifies a Zero-Knowledge Range Proof.
        /// </summary>
        /// <param name="proof">The ZK proof to verify.</param>
        /// <param name="commitment">The Pedersen commitment.</param>
        /// <param name="min">The minimum allowed value (inclusive).</param>
        /// <param name="max">The maximum allowed value (inclusive).</param>
        /// <returns>True if the proof is valid, false otherwise.</returns>
        bool VerifyRange(byte[] proof, byte[] commitment, long min, long max);

        /// <summary>
        /// Generates a Zero-Knowledge Age Proof proving that a person is at least minAge years old
        /// without revealing their exact birthdate or age.
        /// </summary>
        /// <param name="birthDate">The person's birth date.</param>
        /// <param name="minAge">The minimum required age.</param>
        /// <returns>
        /// A tuple containing:
        /// - proof: The ZK proof bytes
        /// - commitment: The commitment to the age value
        /// </returns>
        (byte[] proof, byte[] commitment) ProveAge(DateTime birthDate, int minAge);

        /// <summary>
        /// Verifies a Zero-Knowledge Age Proof.
        /// </summary>
        /// <param name="proof">The ZK proof to verify.</param>
        /// <param name="commitment">The commitment.</param>
        /// <param name="minAge">The minimum required age.</param>
        /// <returns>True if the proof is valid (person is at least minAge), false otherwise.</returns>
        bool VerifyAge(byte[] proof, byte[] commitment, int minAge);

        /// <summary>
        /// Generates a Zero-Knowledge Balance Proof proving that balance >= requiredAmount
        /// without revealing the actual balance.
        /// </summary>
        /// <param name="balance">The actual balance (secret).</param>
        /// <param name="requiredAmount">The minimum required amount.</param>
        /// <returns>
        /// A tuple containing:
        /// - proof: The ZK proof bytes
        /// - commitment: The commitment to the balance
        /// </returns>
        (byte[] proof, byte[] commitment) ProveBalance(long balance, long requiredAmount);

        /// <summary>
        /// Verifies a Zero-Knowledge Balance Proof.
        /// </summary>
        /// <param name="proof">The ZK proof to verify.</param>
        /// <param name="commitment">The commitment.</param>
        /// <param name="requiredAmount">The minimum required amount.</param>
        /// <returns>True if the proof is valid (balance >= requiredAmount), false otherwise.</returns>
        bool VerifyBalance(byte[] proof, byte[] commitment, long requiredAmount);

        /// <summary>
        /// Serializes a proof and commitment for transmission or storage.
        /// </summary>
        /// <param name="proof">The proof bytes.</param>
        /// <param name="commitment">The commitment bytes.</param>
        /// <returns>Base64-encoded serialized proof data.</returns>
        string SerializeProof(byte[] proof, byte[] commitment);

        /// <summary>
        /// Deserializes proof data from Base64 format.
        /// </summary>
        /// <param name="serialized">The Base64-encoded proof data.</param>
        /// <returns>A tuple containing the proof and commitment bytes.</returns>
        (byte[] proof, byte[] commitment) DeserializeProof(string serialized);
    }
}
