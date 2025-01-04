using ZkpSharp.Interfaces;

namespace ZkpSharp.Core
{
    public class Zkp
    {
        private const int RequiredAge = 18; 
        private readonly IProofProvider _proofProvider;

        public Zkp(IProofProvider proofProvider)
        {
            _proofProvider = proofProvider;
        }

        // Proof of Age
        public (string Proof, string Salt) ProveAge(DateTime dateOfBirth)
        {
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
            int age = Utilities.CalculateAge(dateOfBirth);
            string calculatedProof = _proofProvider.GenerateHMAC(dateOfBirth.ToString("yyyy-MM-dd") + salt);
            return age >= RequiredAge && _proofProvider.SecureEqual(calculatedProof, proof);
        }

        // Proof of Balance
        public (string Proof, string Salt) ProveBalance(double balance, double requestedAmount)
        {
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
            string calculatedProof = _proofProvider.GenerateHMAC(balance.ToString() + salt);
            return _proofProvider.SecureEqual(calculatedProof, proof) && balance >= requestedAmount;
        }

        // Proof of Membership
        public string ProveMembership(string value, string[] validValues)
        {
            string salt = _proofProvider.GenerateSalt();
            var validValueHash = validValues.Select(v => _proofProvider.GenerateHMAC(v)).ToArray();

            foreach (var hash in validValueHash)
            {
                if (_proofProvider.SecureEqual(_proofProvider.GenerateHMAC(value + salt), hash))
                {
                    return salt;
                }
            }

            throw new ArgumentException("Value does not belong to the set.");
        }

        public bool VerifyMembership(string value, string salt, string[] validValues)
        {
            var validValueHash = validValues.Select(v => _proofProvider.GenerateHMAC(v)).ToArray();
            string proof = _proofProvider.GenerateHMAC(value + salt);

            return validValueHash.Contains(proof);
        }

        // Proof of Range
        public string ProveRange(double value, double minValue, double maxValue)
        {
            if (value < minValue || value > maxValue)
            {
                throw new ArgumentException("Value out of range.");
            }

            string salt = _proofProvider.GenerateSalt();
            string proof = _proofProvider.GenerateHMAC(value.ToString() + salt);
            return proof;
        }

        public bool VerifyRange(string proof, double minValue, double maxValue, double value)
        {
            if (value < minValue || value > maxValue)
            {
                return false;
            }

            string calculatedProof = _proofProvider.GenerateHMAC(value.ToString() + _proofProvider.GenerateSalt());
            return _proofProvider.SecureEqual(proof, calculatedProof);
        }


        // Proof of Time Condition
        public string ProveTimeCondition(DateTime eventDate, DateTime conditionDate)
        {
            if (eventDate < conditionDate)
            {
                throw new ArgumentException("Event does not meet the time condition.");
            }

            string salt = _proofProvider.GenerateSalt();
            string proof = _proofProvider.GenerateHMAC(eventDate.ToString("yyyy-MM-dd") + salt);
            return proof;
        }

        public bool VerifyTimeCondition(string proof, DateTime eventDate, DateTime conditionDate, string salt)
        {
            if (eventDate < conditionDate)
            {
                return false;
            }

            string calculatedProof = _proofProvider.GenerateHMAC(eventDate.ToString("yyyy-MM-dd") + salt);
            return _proofProvider.SecureEqual(proof, calculatedProof);
        }
    }
}