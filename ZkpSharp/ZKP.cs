using System.Security.Cryptography;
using System.Text;

namespace ZkpSharp;

public class ZKP
{
    private const int RequiredAge = 18; // Minimum required age for proof
    private readonly byte[] _hmacKey = new byte[32]; // Secret key for HMAC

    public ZKP()
    {
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(_hmacKey); // Generate a secure HMAC key
        }
    }

    public (string Proof, string Salt) ProveAge(DateTime dateOfBirth)
    {
        if (CalculateAge(dateOfBirth) < RequiredAge)
        {
            throw new ArgumentException("Insufficient age");
        }

        string salt = GenerateSalt();
        string proof = GenerateHMAC(dateOfBirth.ToString("yyyy-MM-dd") + salt);
        return (proof, salt);
    }

    public bool VerifyAge(string proof, DateTime dateOfBirth, string salt)
    {
        int age = CalculateAge(dateOfBirth);
        string calculatedProof = GenerateHMAC(dateOfBirth.ToString("yyyy-MM-dd") + salt);
        return age >= RequiredAge && SecureEqual(calculatedProof, proof);
    }

    public (string Proof, string Salt) ProveBalance(double balance, double requestedAmount)
    {
        if (balance < requestedAmount)
        {
            throw new ArgumentException("Insufficient balance");
        }

        string salt = GenerateSalt();
        string proof = GenerateHMAC(balance.ToString() + salt);
        return (proof, salt);
    }

    public bool VerifyBalance(string proof, double requestedAmount, string salt, double balance)
    {
        string calculatedProof = GenerateHMAC(balance.ToString() + salt);
        return SecureEqual(calculatedProof, proof) && balance >= requestedAmount;
    }

    private int CalculateAge(DateTime dateOfBirth)
    {
        DateTime today = DateTime.UtcNow;
        int age = today.Year - dateOfBirth.Year;

        if (dateOfBirth > today.AddYears(-age)) age--; 
        return age;
    }

    private string GenerateSalt()
    {
        byte[] saltBytes = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }

        return Convert.ToBase64String(saltBytes);
    }

    private string GenerateHMAC(string input)
    {
        using (var hmac = new HMACSHA256(_hmacKey))
        {
            byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hashBytes);
        }
    }

    private bool SecureEqual(string a, string b)
    {
        if (a.Length != b.Length) return false;

        int diff = 0;
        for (int i = 0; i < a.Length; i++)
        {
            diff |= a[i] ^ b[i];
        }

        return diff == 0;
    }
}