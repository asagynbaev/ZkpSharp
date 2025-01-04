namespace ZkpSharp.Interfaces
{
    public interface IProofProvider
    {
        string GenerateSalt();
        string GenerateHMAC(string input);
        bool SecureEqual(string a, string b);
    }
}