using StellarDotnetSdk;
using Xunit;
using ZkpSharp.Core;
using ZkpSharp.Integration.Stellar;
using ZkpSharp.Security;

namespace ZkpSharp.Tests.Integration.Stellar;

/// <summary>
/// Optional testnet smoke tests against a deployed ZkpVerifier contract.
/// Skipped automatically when <c>ZKP_CONTRACT_ID</c> is not set.
/// </summary>
/// <remarks>
/// Prerequisites:
/// <list type="number">
/// <item>Deploy <c>contracts/stellar</c> to Stellar testnet (see <c>contracts/stellar/DEPLOYMENT.md</c>).</item>
/// <item><c>ZKP_CONTRACT_ID</c> — deployed contract address (C…).</item>
/// <item><c>ZKP_HMAC_KEY</c> — same Base64 32-byte key used when invoking HMAC verification (optional; tests use a documented dev default if unset).</item>
/// <item><c>ZKP_SOURCE_ACCOUNT</c> — optional; defaults to a known funded testnet account used elsewhere in this suite.</item>
/// </list>
/// Run only these tests:
/// <c>dotnet test --filter "FullyQualifiedName~StellarTestnetSmokeTests"</c>
/// </remarks>
public class StellarTestnetSmokeTests
{
    private const string TestHorizon = "https://horizon-testnet.stellar.org";
    private const string TestSorobanRpc = "https://soroban-testnet.stellar.org";

    /// <summary>Public testnet account with balance (same as <see cref="StellarTests"/>).</summary>
    private const string DefaultSourceAccount = "GAIH3ULLFQ4DGSECF2AR555KZ4KNDGEKN4AFI4SU2M7B43MGK3QJZNSR";

    private static string GetContractId()
    {
        var id = Environment.GetEnvironmentVariable("ZKP_CONTRACT_ID");
        return string.IsNullOrWhiteSpace(id) ? "" : id.Trim();
    }

    private static string GetHmacKey()
        => Environment.GetEnvironmentVariable("ZKP_HMAC_KEY")
           ?? "V0V3Mv4D1USxZYwWL4eG93m0JKdO9KbXQn0mhg+EXHc=";

    private static string GetSourceAccount()
        => Environment.GetEnvironmentVariable("ZKP_SOURCE_ACCOUNT")?.Trim()
           ?? DefaultSourceAccount;

    [SkippableFact]
    public async Task Testnet_Horizon_SourceAccount_HasBalance()
    {
        Skip.If(string.IsNullOrEmpty(GetContractId()));

        var blockchain = new StellarBlockchain(TestHorizon, TestSorobanRpc, Network.Test(), GetHmacKey());
        var balance = await blockchain.GetAccountBalance(GetSourceAccount());
        Assert.True(balance >= 0);
    }

    [SkippableFact]
    public async Task Testnet_VerifyProofWithSourceAccount_ValidMembership()
    {
        Skip.If(string.IsNullOrEmpty(GetContractId()));

        var contractId = GetContractId();
        var hmacKey = GetHmacKey();
        var source = GetSourceAccount();
        var zkp = new Zkp(new ProofProvider(hmacKey));
        var blockchain = new StellarBlockchain(TestHorizon, TestSorobanRpc, Network.Test(), hmacKey);

        var testData = "smoke-membership-" + Guid.NewGuid().ToString("N")[..8];
        var (proof, salt) = zkp.ProveMembership(testData, new[] { testData, "other" });

        var ok = await blockchain.VerifyProofWithSourceAccount(source, contractId, proof, salt, testData);
        Assert.True(ok);
    }

    [SkippableFact]
    public async Task Testnet_VerifyProofWithSourceAccount_WrongData_ReturnsFalse()
    {
        Skip.If(string.IsNullOrEmpty(GetContractId()));

        var contractId = GetContractId();
        var hmacKey = GetHmacKey();
        var source = GetSourceAccount();
        var zkp = new Zkp(new ProofProvider(hmacKey));
        var blockchain = new StellarBlockchain(TestHorizon, TestSorobanRpc, Network.Test(), hmacKey);

        var testData = "smoke-wrong-" + Guid.NewGuid().ToString("N")[..8];
        var (proof, salt) = zkp.ProveMembership(testData, new[] { testData });

        var ok = await blockchain.VerifyProofWithSourceAccount(source, contractId, proof, salt, "not-the-same");
        Assert.False(ok);
    }

    [SkippableFact]
    public async Task Testnet_VerifyBalanceProofWithSourceAccount_Valid()
    {
        Skip.If(string.IsNullOrEmpty(GetContractId()));

        var contractId = GetContractId();
        var hmacKey = GetHmacKey();
        var source = GetSourceAccount();
        var zkp = new Zkp(new ProofProvider(hmacKey));
        var blockchain = new StellarBlockchain(TestHorizon, TestSorobanRpc, Network.Test(), hmacKey);

        const double balance = 1000.0;
        const double required = 500.0;
        var (proof, salt) = zkp.ProveBalance(balance, required);

        var ok = await blockchain.VerifyBalanceProofWithSourceAccount(
            source, contractId, proof, balance, required, salt);
        Assert.True(ok);
    }

    [SkippableFact]
    public async Task Testnet_VerifyZkRangeProofWithSourceAccount_StructuralOk()
    {
        Skip.If(string.IsNullOrEmpty(GetContractId()));

        var contractId = GetContractId();
        var source = GetSourceAccount();
        var zk = new BulletproofsProvider();
        var blockchain = new StellarBlockchain(TestHorizon, TestSorobanRpc, Network.Test(), GetHmacKey());

        var (proof, commitment) = zk.ProveRange(50, 0, 100);
        var ok = await blockchain.VerifyZkRangeProofWithSourceAccount(
            source, contractId, proof, commitment, 0, 100);
        Assert.True(ok);
    }

    [SkippableFact]
    public async Task Testnet_VerifyZkAgeProofWithSourceAccount_StructuralOk()
    {
        Skip.If(string.IsNullOrEmpty(GetContractId()));

        var contractId = GetContractId();
        var source = GetSourceAccount();
        var zk = new BulletproofsProvider();
        var blockchain = new StellarBlockchain(TestHorizon, TestSorobanRpc, Network.Test(), GetHmacKey());

        var birth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var (proof, commitment) = zk.ProveAge(birth, 18);
        var ok = await blockchain.VerifyZkAgeProofWithSourceAccount(source, contractId, proof, commitment, 18);
        Assert.True(ok);
    }

    [SkippableFact]
    public async Task Testnet_VerifyZkBalanceProofWithSourceAccount_StructuralOk()
    {
        Skip.If(string.IsNullOrEmpty(GetContractId()));

        var contractId = GetContractId();
        var source = GetSourceAccount();
        var zk = new BulletproofsProvider();
        var blockchain = new StellarBlockchain(TestHorizon, TestSorobanRpc, Network.Test(), GetHmacKey());

        var (proof, commitment) = zk.ProveBalance(1000, 500);
        var ok = await blockchain.VerifyZkBalanceProofWithSourceAccount(
            source, contractId, proof, commitment, 500);
        Assert.True(ok);
    }
}
