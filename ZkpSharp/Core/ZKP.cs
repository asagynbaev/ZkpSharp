using ZkpSharp.Interfaces;

namespace ZkpSharp.Core
{
    public class Zkp
    {
        private const int RequiredAge = 18; 
        private readonly IProofProvider _proofProvider;

        public Zkp(IProofProvider proofProvider)
        {
            _proofProvider = proofProvider ?? throw new ArgumentNullException(nameof(proofProvider));
        }

        // Proof of Age
        public (string Proof, string Salt) ProveAge(DateTime dateOfBirth)
        {
            if (dateOfBirth > DateTime.UtcNow)
            {
                throw new ArgumentException("Date of birth cannot be in the future.");
            }

            if (Utilities.CalculateAge(dateOfBirth) < RequiredAge)
            {
                throw new ArgumentException("Insufficient age");
            }

            string salt = _proofProvider.GenerateSalt();
            string proof = _proofProvider.GenerateHMAC(dateOfBirth.ToString("yyyy-MM-dd") + salt);
            return (proof, salt);
        }

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
            string calculatedProof = _proofProvider.GenerateHMAC(dateOfBirth.ToString("yyyy-MM-dd") + salt);
            return age >= RequiredAge && _proofProvider.SecureEqual(calculatedProof, proof);
        }

        // Proof of Balance
        public (string Proof, string Salt) ProveBalance(double balance, double requestedAmount)
        {
            if (balance < 0)
            {
                throw new ArgumentException("Balance cannot be negative.");
            }

            if (requestedAmount < 0)
            {
                throw new ArgumentException("Requested amount cannot be negative.");
            }

            if (balance < requestedAmount)
            {
                throw new ArgumentException("Insufficient balance");
            }

            string salt = _proofProvider.GenerateSalt();
            string proof = _proofProvider.GenerateHMAC(balance.ToString() + salt);
            return (proof, salt);
        }

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

        // Proof of Membership
        public (string Proof, string Salt) ProveMembership(string value, string[] validValues)
        {
            if (validValues == null || validValues.Length == 0)
            {
                throw new ArgumentException("Valid values array cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("Value cannot be null or empty.");
            }

            if (!validValues.Contains(value))
            {
                throw new ArgumentException("Value does not belong to the set.");
            }

            string salt = _proofProvider.GenerateSalt();
            string proof = _proofProvider.GenerateHMAC(value + salt);
            return (proof, salt);
        }

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

        // Proof of Range
        public (string Proof, string Salt) ProveRange(double value, double minValue, double maxValue)
        {
            if (value < minValue || value > maxValue)
            {
                throw new ArgumentException("Value out of range.");
            }

            string salt = _proofProvider.GenerateSalt();
            string proof = _proofProvider.GenerateHMAC(value.ToString() + salt);
            return (proof, salt);
        }

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


        // Proof of Time Condition
        public (string Proof, string Salt) ProveTimeCondition(DateTime eventDate, DateTime conditionDate)
        {
            if (eventDate < conditionDate)
            {
                throw new ArgumentException("Event does not meet the time condition.");
            }

            string salt = _proofProvider.GenerateSalt();
            string proof = _proofProvider.GenerateHMAC(eventDate.ToString("yyyy-MM-dd") + salt);
            return (proof, salt);
        }

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

            string calculatedProof = _proofProvider.GenerateHMAC(eventDate.ToString("yyyy-MM-dd") + salt);
            return _proofProvider.SecureEqual(proof, calculatedProof);
        }
    }
}