using ZkpSharp.Security;

namespace ZkpSharp.Core
{
    public class ZKP
    {
        private const int RequiredAge = 18; 
        private readonly IProofProvider _proofProvider;

        public ZKP(IProofProvider proofProvider)
        {
            _proofProvider = proofProvider;
        }

        // Proof of Age
        public (string Proof, string Salt) ProveAge(DateTime dateOfBirth)
        {
            if (CalculateAge(dateOfBirth) < RequiredAge)
            {
                throw new ArgumentException("Insufficient age");
            }

            string salt = _proofProvider.GenerateSalt();
            string proof = _proofProvider.GenerateHMAC(dateOfBirth.ToString("yyyy-MM-dd") + salt);
            return (proof, salt);
        }

        public bool VerifyAge(string proof, DateTime dateOfBirth, string salt)
        {
            int age = CalculateAge(dateOfBirth);
            string calculatedProof = _proofProvider.GenerateHMAC(dateOfBirth.ToString("yyyy-MM-dd") + salt);
            return age >= RequiredAge && _proofProvider.SecureEqual(calculatedProof, proof);
        }

        private int CalculateAge(DateTime dateOfBirth)
        {
            DateTime today = DateTime.UtcNow;
            int age = today.Year - dateOfBirth.Year;
            if (dateOfBirth > today.AddYears(-age)) age--; 
            return age;
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
    }
}