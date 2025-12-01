// Author: Azimbek Sagynbaev
// Last modified on: 05-01-2025 16:20

using System.Text;
using System.Text.Json;

namespace ZkpSharp.Integration.Stellar
{
    public class SorobanContractDeployer
    {
        private const string SorobanRpcUrl = "https://soroban-testnet.stellar.org";

        public class DeploymentParameters
        {
            public string? WasmPath { get; set; }
            public string? WasmHash { get; set; }
            public string? Salt { get; set; }

            // JSON string or raw proof data
            public string? ZkpProof { get; set; }

            // JSON string or raw key data
            public string? VerifyingKey { get; set; }
            public List<string> PublicInputs { get; set; } = [];
        }

        public class Error : Exception
        {
            public Error(string message) : base(message) { }
        }

        public async Task DeployContractAsync(DeploymentParameters parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (string.IsNullOrEmpty(parameters.WasmPath) && string.IsNullOrEmpty(parameters.WasmHash))
            {
                throw new ArgumentException("Either WasmPath or WasmHash must be provided.", nameof(parameters));
            }

            if (string.IsNullOrEmpty(parameters.ZkpProof) || string.IsNullOrEmpty(parameters.VerifyingKey))
            {
                throw new ArgumentException("ZKP proof and verifying key are required.", nameof(parameters));
            }

            var wasmHash = !string.IsNullOrEmpty(parameters.WasmHash)
                ? parameters.WasmHash
                : ComputeWasmHash(parameters.WasmPath!);

            Console.WriteLine($"Using WASM hash: {wasmHash}");

            var salt = string.IsNullOrEmpty(parameters.Salt)
                ? GenerateSalt()
                : ParseSalt(parameters.Salt);

            var contractId = ComputeContractId(wasmHash, salt);

            var transaction = BuildTransaction(wasmHash, salt, contractId, parameters);

            await SimulateTransaction(transaction);
            await SubmitTransaction(transaction);
        }

        private string BuildTransaction(
            string wasmHash,
            byte[] salt,
            string contractId,
            DeploymentParameters parameters)
        {
            return JsonSerializer.Serialize(new
            {
                wasmHash,
                salt = BitConverter.ToString(salt).Replace("-", "").ToLower(),
                contractId,
                zkp = new
                {
                    proof = parameters.ZkpProof,
                    verifyingKey = parameters.VerifyingKey,
                    publicInputs = parameters.PublicInputs
                }
            });
        }

        private static string ComputeWasmHash(string wasmPath)
        {
            if (!File.Exists(wasmPath))
                throw new FileNotFoundException($"WASM file not found: {wasmPath}");

            using var fileStream = new FileStream(wasmPath, FileMode.Open, FileAccess.Read);
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(fileStream);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        private static byte[] GenerateSalt()
        {
            var salt = new byte[32];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        private static byte[] ParseSalt(string saltHex)
        {
            if (saltHex.Length != 64)
                throw new Error($"Invalid salt length: {saltHex}");

            return Enumerable.Range(0, saltHex.Length / 2)
                .Select(x => Convert.ToByte(saltHex.Substring(x * 2, 2), 16))
                .ToArray();
        }

        private static string ComputeContractId(string wasmHash, byte[] salt)
        {
            return $"{wasmHash}:{BitConverter.ToString(salt).Replace("-", "").ToLower()}";
        }

        private static async Task SimulateTransaction(string transaction)
        {
            Console.WriteLine("Simulating transaction...");

            using var client = new HttpClient();
            var requestPayload = new
            {
                jsonrpc = "2.0",
                method = "simulateTransaction",
                @params = new { transaction },
                id = 1
            };

            var response = await client.PostAsync(
                SorobanRpcUrl,
                new StringContent(
                    JsonSerializer.Serialize(requestPayload),
                    Encoding.UTF8,
                    "application/json"
                )
            );

            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Error($"Failed to simulate transaction: {response.ReasonPhrase}. Response: {content}");
            }

            Console.WriteLine($"Simulation Response: {content}");
        }

        private static async Task SubmitTransaction(string transaction)
        {
            Console.WriteLine("Submitting transaction...");

            using var client = new HttpClient();
            var requestPayload = new
            {
                jsonrpc = "2.0",
                method = "sendTransaction",
                @params = new { transaction },
                id = 1
            };

            var response = await client.PostAsync(
                SorobanRpcUrl,
                new StringContent(
                    JsonSerializer.Serialize(requestPayload),
                    Encoding.UTF8,
                    "application/json"
                )
            );

            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Error($"Failed to submit transaction: {response.ReasonPhrase}. Response: {content}");
            }

            Console.WriteLine($"Submission Response: {content}");
        }
    }
}