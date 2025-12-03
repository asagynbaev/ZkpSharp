using ZkpSharp.Interfaces;
using ZkpSharp.Constants;
using ZkpSharp.Exceptions;
using ZkpSharp.Validation;

namespace ZkpSharp.Core
{
    /// <summary>
    /// Main class for generating and verifying Zero-Knowledge Proofs (ZKP).
    /// Supports multiple types of proofs: age, balance, membership, range, and time conditions.
    /// </summary>
    public class Zkp
    {
        private readonly int _requiredAge;
        private readonly IProofProvider _proofProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="Zkp"/> class.
        /// </summary>
        /// <param name="proofProvider">The proof provider used for cryptographic operations.</param>
        /// <param name="requiredAge">The minimum age required for age verification. Defaults to 18.</param>
        /// <exception cref="ArgumentNullException">Thrown when proofProvider is null.</exception>
        public Zkp(IProofProvider proofProvider, int requiredAge = ZkpConstants.DefaultRequiredAge)
        {
            _proofProvider = proofProvider ?? throw new ArgumentNullException(nameof(proofProvider));
            _requiredAge = requiredAge;
        }

        /// <summary>
        /// Generates a proof of age for the given date of birth.
        /// </summary>
        /// <param name="dateOfBirth">The date of birth to prove.</param>
        /// <returns>A tuple containing the proof and salt.</returns>
        /// <exception cref="ArgumentException">Thrown when date of birth is in the future.</exception>
        /// <exception cref="InsufficientAgeException">Thrown when the age is below the required minimum.</exception>
        public (string Proof, string Salt) ProveAge(DateTime dateOfBirth)
        {
            ArgumentValidator.ThrowIfFutureDate(dateOfBirth, nameof(dateOfBirth));

            int age = Utilities.CalculateAge(dateOfBirth);
            if (age < _requiredAge)
            {
                throw new InsufficientAgeException(_requiredAge, age);
            }

            string salt = _proofProvider.GenerateSalt();
            string proof = _proofProvider.GenerateHMAC(dateOfBirth.ToString(ZkpConstants.DateFormat) + salt);
            return (proof, salt);
        }

        /// <summary>
        /// Verifies a proof of age.
        /// </summary>
        /// <param name="proof">The proof to verify.</param>
        /// <param name="dateOfBirth">The date of birth that was used to generate the proof.</param>
        /// <param name="salt">The salt that was used to generate the proof.</param>
        /// <returns>True if the proof is valid, false otherwise.</returns>
        public bool VerifyAge(string proof, DateTime dateOfBirth, string salt)
        {
            if (string.IsNullOrEmpty(proof) || string.IsNullOrEmpty(salt))
            {
                return false;
            }

            if (dateOfBirth > DateTime.UtcNow)
            {
                return false;
            }

            int age = Utilities.CalculateAge(dateOfBirth);
            string calculatedProof = _proofProvider.GenerateHMAC(dateOfBirth.ToString(ZkpConstants.DateFormat) + salt);
            return age >= _requiredAge && _proofProvider.SecureEqual(calculatedProof, proof);
        }

        /// <summary>
        /// Generates a proof of balance for the given balance and requested amount.
        /// </summary>
        /// <param name="balance">The account balance.</param>
        /// <param name="requestedAmount">The amount that needs to be proven available.</param>
        /// <returns>A tuple containing the proof and salt.</returns>
        /// <exception cref="ArgumentException">Thrown when balance or requested amount is negative.</exception>
        /// <exception cref="InsufficientBalanceException">Thrown when balance is less than requested amount.</exception>
        public (string Proof, string Salt) ProveBalance(double balance, double requestedAmount)
        {
            ArgumentValidator.ThrowIfNegative(balance, nameof(balance));
            ArgumentValidator.ThrowIfNegative(requestedAmount, nameof(requestedAmount));

            if (balance < requestedAmount)
            {
                throw new InsufficientBalanceException(balance, requestedAmount);
            }

            string salt = _proofProvider.GenerateSalt();
            string proof = _proofProvider.GenerateHMAC(balance.ToString() + salt);
            return (proof, salt);
        }

        /// <summary>
        /// Verifies a proof of balance.
        /// </summary>
        /// <param name="proof">The proof to verify.</param>
        /// <param name="requestedAmount">The amount that was requested.</param>
        /// <param name="salt">The salt that was used to generate the proof.</param>
        /// <param name="balance">The balance that was used to generate the proof.</param>
        /// <returns>True if the proof is valid and balance is sufficient, false otherwise.</returns>
        public bool VerifyBalance(string proof, double requestedAmount, string salt, double balance)
        {
            if (string.IsNullOrEmpty(proof) || string.IsNullOrEmpty(salt))
            {
                return false;
            }

            if (balance < 0 || requestedAmount < 0)
            {
                return false;
            }

            string calculatedProof = _proofProvider.GenerateHMAC(balance.ToString() + salt);
            return _proofProvider.SecureEqual(calculatedProof, proof) && balance >= requestedAmount;
        }

        /// <summary>
        /// Generates a proof of membership for a value in a set of valid values.
        /// </summary>
        /// <param name="value">The value to prove membership for.</param>
        /// <param name="validValues">The set of valid values.</param>
        /// <returns>A tuple containing the proof and salt.</returns>
        /// <exception cref="ArgumentException">Thrown when validValues is null or empty, or value is null or empty.</exception>
        /// <exception cref="ValueNotInSetException">Thrown when value does not belong to the set of valid values.</exception>
        public (string Proof, string Salt) ProveMembership(string value, string[] validValues)
        {
            ArgumentValidator.ThrowIfNullOrEmpty(validValues, nameof(validValues));
            ArgumentValidator.ThrowIfNullOrEmpty(value, nameof(value));

            if (!validValues.Contains(value))
            {
                throw new ValueNotInSetException(value);
            }

            string salt = _proofProvider.GenerateSalt();
            string proof = _proofProvider.GenerateHMAC(value + salt);
            return (proof, salt);
        }

        /// <summary>
        /// Verifies a proof of membership.
        /// </summary>
        /// <param name="proof">The proof to verify.</param>
        /// <param name="value">The value that was used to generate the proof.</param>
        /// <param name="salt">The salt that was used to generate the proof.</param>
        /// <param name="validValues">The set of valid values.</param>
        /// <returns>True if the proof is valid and value belongs to the set, false otherwise.</returns>
        public bool VerifyMembership(string proof, string value, string salt, string[] validValues)
        {
            if (validValues == null || validValues.Length == 0)
            {
                return false;
            }

            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(proof) || string.IsNullOrEmpty(salt))
            {
                return false;
            }

            if (!validValues.Contains(value))
            {
                return false;
            }

            string calculatedProof = _proofProvider.GenerateHMAC(value + salt);
            return _proofProvider.SecureEqual(calculatedProof, proof);
        }

        /// <summary>
        /// Generates a proof that a value lies within a specified range.
        /// </summary>
        /// <param name="value">The value to prove.</param>
        /// <param name="minValue">The minimum allowed value.</param>
        /// <param name="maxValue">The maximum allowed value.</param>
        /// <returns>A tuple containing the proof and salt.</returns>
        /// <exception cref="ValueOutOfRangeException">Thrown when value is outside the specified range.</exception>
        public (string Proof, string Salt) ProveRange(double value, double minValue, double maxValue)
        {
            if (value < minValue || value > maxValue)
            {
                throw new ValueOutOfRangeException(value, minValue, maxValue);
            }

            string salt = _proofProvider.GenerateSalt();
            string proof = _proofProvider.GenerateHMAC(value.ToString() + salt);
            return (proof, salt);
        }

        /// <summary>
        /// Verifies a proof of range.
        /// </summary>
        /// <param name="proof">The proof to verify.</param>
        /// <param name="minValue">The minimum allowed value.</param>
        /// <param name="maxValue">The maximum allowed value.</param>
        /// <param name="value">The value that was used to generate the proof.</param>
        /// <param name="salt">The salt that was used to generate the proof.</param>
        /// <returns>True if the proof is valid and value is within range, false otherwise.</returns>
        public bool VerifyRange(string proof, double minValue, double maxValue, double value, string salt)
        {
            if (string.IsNullOrEmpty(proof) || string.IsNullOrEmpty(salt))
            {
                return false;
            }

            if (minValue > maxValue)
            {
                return false;
            }

            if (value < minValue || value > maxValue)
            {
                return false;
            }

            string calculatedProof = _proofProvider.GenerateHMAC(value.ToString() + salt);
            return _proofProvider.SecureEqual(proof, calculatedProof);
        }

        /// <summary>
        /// Generates a proof that an event occurred after a specified condition date.
        /// </summary>
        /// <param name="eventDate">The date of the event.</param>
        /// <param name="conditionDate">The condition date that must be met.</param>
        /// <returns>A tuple containing the proof and salt.</returns>
        /// <exception cref="ArgumentException">Thrown when event date is before the condition date.</exception>
        public (string Proof, string Salt) ProveTimeCondition(DateTime eventDate, DateTime conditionDate)
        {
            if (eventDate < conditionDate)
            {
                throw new ArgumentException("Event does not meet the time condition.");
            }

            string salt = _proofProvider.GenerateSalt();
            string proof = _proofProvider.GenerateHMAC(eventDate.ToString(ZkpConstants.DateFormat) + salt);
            return (proof, salt);
        }

        /// <summary>
        /// Verifies a proof of time condition.
        /// </summary>
        /// <param name="proof">The proof to verify.</param>
        /// <param name="eventDate">The date of the event that was used to generate the proof.</param>
        /// <param name="conditionDate">The condition date.</param>
        /// <param name="salt">The salt that was used to generate the proof.</param>
        /// <returns>True if the proof is valid and event date meets the condition, false otherwise.</returns>
        public bool VerifyTimeCondition(string proof, DateTime eventDate, DateTime conditionDate, string salt)
        {
            if (string.IsNullOrEmpty(proof) || string.IsNullOrEmpty(salt))
            {
                return false;
            }

            if (eventDate < conditionDate)
            {
                return false;
            }

            string calculatedProof = _proofProvider.GenerateHMAC(eventDate.ToString(ZkpConstants.DateFormat) + salt);
            return _proofProvider.SecureEqual(proof, calculatedProof);
        }
    }
}