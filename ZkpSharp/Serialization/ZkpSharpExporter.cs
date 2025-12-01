using System.Text.Json;
namespace ZkpSharp.Serialization;

/// <summary>
/// Provides serialization utilities for zero-knowledge proofs.
/// </summary>
public class ZkpSharpExporter
{
    /// <summary>
    /// Serializes a proof and salt into a JSON string.
    /// </summary>
    /// <param name="proof">The proof to serialize.</param>
    /// <param name="salt">The salt to serialize.</param>
    /// <returns>A JSON string containing the proof and salt.</returns>
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