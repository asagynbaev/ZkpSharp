using System.Text.Json;
namespace ZkpSharp.Serialization;

public class ZkpSharpExporter
{
    public static string SerializeProof(string proof, string salt)
    {
        var proofData = new
        {
            proof,
            salt
        };

        return JsonSerializer.Serialize(proofData);
    }
}