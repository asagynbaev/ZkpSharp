namespace ZkpSharp.Interfaces;

public interface IProofChecker
{
    Task<bool> VerifyProofAsync(string contractId, string proof, string salt, string value);
}