using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using StellarDotnetSdk;
using StellarDotnetSdk.Accounts;
using StellarDotnetSdk.Transactions;
using StellarDotnetSdk.Responses;

namespace ZkpSharp.Integration.Stellar
{
    /// <summary>
    /// Client for interacting with Soroban RPC API to invoke smart contracts.
    /// </summary>
    public class SorobanRpcClient : IDisposable
    {
        private readonly string _rpcUrl;
        private readonly HttpClient _httpClient;
        private readonly Server _server;

        public SorobanRpcClient(string rpcUrl, string? horizonUrl = null)
        {
            if (string.IsNullOrEmpty(rpcUrl))
            {
                throw new ArgumentException("RPC URL cannot be null or empty.", nameof(rpcUrl));
            }

            _rpcUrl = rpcUrl.TrimEnd('/');
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            
            // Use Horizon server if provided, otherwise try to infer from RPC URL
            horizonUrl ??= rpcUrl.Replace("soroban", "horizon").Replace("/rpc", "");
            _server = new Server(horizonUrl);
        }

        /// <summary>
        /// Invokes a contract method using Soroban RPC API.
        /// </summary>
        /// <param name="contractId">The contract ID (address) to invoke.</param>
        /// <param name="functionName">The name of the function to call.</param>
        /// <param name="arguments">The arguments to pass to the function as byte arrays.</param>
        /// <returns>The result of the contract invocation.</returns>
        public Task<bool> InvokeContractAsync(string contractId, string functionName, params byte[][] arguments)
        {
            if (string.IsNullOrEmpty(contractId))
            {
                throw new ArgumentException("Contract ID cannot be null or empty.", nameof(contractId));
            }

            if (string.IsNullOrEmpty(functionName))
            {
                throw new ArgumentException("Function name cannot be null or empty.", nameof(functionName));
            }

            // For now, use a simplified approach that requires the transaction XDR to be provided
            // In production, you should build the transaction properly using XDR encoding
            throw new NotImplementedException(
                "Direct contract invocation requires proper Soroban transaction XDR encoding. " +
                "Use InvokeContractWithTransactionXdrAsync method with a pre-built transaction XDR, " +
                "or implement proper XDR encoding using Soroban SDK.");
        }

        /// <summary>
        /// Invokes a contract method using a pre-built transaction XDR.
        /// This method allows you to provide your own transaction XDR built with proper Soroban SDK.
        /// </summary>
        /// <param name="transactionXdr">The transaction XDR (base64 encoded) for invoking the contract.</param>
        /// <returns>The result of the contract invocation.</returns>
        public async Task<bool> InvokeContractWithTransactionXdrAsync(string transactionXdr)
        {
            if (string.IsNullOrEmpty(transactionXdr))
            {
                throw new ArgumentException("Transaction XDR cannot be null or empty.", nameof(transactionXdr));
            }

            try
            {
                // Simulate the transaction to get the result
                var simulateResponse = await SimulateTransactionAsync(transactionXdr);

                if (simulateResponse.Error != null)
                {
                    throw new InvalidOperationException(
                        $"RPC Error: {simulateResponse.Error.Code} - {simulateResponse.Error.Message}");
                }

                if (simulateResponse.Result == null)
                {
                    throw new InvalidOperationException("Invalid response from RPC: missing result");
                }

                // Parse the result
                return ParseSimulationResult(simulateResponse.Result);
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Failed to communicate with Soroban RPC: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Builds a transaction for invoking a Soroban contract method.
        /// Note: This is a simplified implementation that creates a basic transaction structure.
        /// For production use with Soroban contracts, you should use a Soroban-specific SDK
        /// or properly build XDR transactions with InvokeHostFunctionOp.
        /// </summary>
        private async Task<string> BuildInvokeTransactionAsync(
            string sourceAccountId,
            string contractId,
            string functionName,
            byte[][] arguments)
        {
            try
            {
                var sourceAccount = await _server.Accounts.Account(sourceAccountId);

                // Create a basic transaction
                // Note: For actual Soroban invocation, you need to create a transaction
                // with InvokeHostFunctionOp operation, which requires proper XDR encoding
                var transactionBuilder = new TransactionBuilder(sourceAccount);
                var transaction = transactionBuilder.Build();

                // For now, we'll create a placeholder XDR
                // In production, this should be a proper Soroban transaction with InvokeHostFunctionOp
                // This requires using XDR encoding libraries or Soroban-specific SDK
                throw new NotImplementedException(
                    "Proper Soroban transaction building requires XDR encoding. " +
                    "Consider using a Soroban-specific SDK or XDR encoding library. " +
                    "For testing, you can manually create the transaction XDR.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to build transaction: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Simulates a transaction without submitting it to the network.
        /// </summary>
        private async Task<RpcResponse<SimulateTransactionResponse>> SimulateTransactionAsync(string transactionXdr)
        {
            var requestPayload = new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "simulateTransaction",
                @params = new
                {
                    transaction = transactionXdr
                }
            };

            var json = JsonSerializer.Serialize(requestPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_rpcUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"RPC request failed with status {response.StatusCode}: {responseContent}");
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var rpcResponse = JsonSerializer.Deserialize<RpcResponse<SimulateTransactionResponse>>(
                responseContent, options);

            return rpcResponse ?? throw new InvalidOperationException("Failed to deserialize RPC response");
        }

        /// <summary>
        /// Parses the simulation result to extract the boolean return value.
        /// </summary>
        private bool ParseSimulationResult(SimulateTransactionResponse result)
        {
            try
            {
                // The result should contain returnValue in XDR format
                // For boolean results, we need to decode the XDR
                if (!string.IsNullOrEmpty(result.ReturnValue))
                {
                    // Decode XDR to get the boolean value using proper XDR decoding
                    return DecodeBooleanFromXdr(result.ReturnValue);
                }

                // If no return value but also no error, log warning
                if (string.IsNullOrEmpty(result.Error))
                {
                    // Some contracts might not return a value
                    // Default to false for safety
                    return false;
                }

                // If there's an error, throw it
                throw new InvalidOperationException($"Contract execution error: {result.Error}");
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to parse simulation result: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Decodes a boolean value from XDR-encoded ScVal.
        /// </summary>
        /// <param name="xdrBase64">The base64-encoded XDR ScVal.</param>
        /// <returns>The decoded boolean value.</returns>
        /// <remarks>
        /// NOTE: This is a simplified implementation. Full XDR decoding requires
        /// proper Stellar SDK support which is under development for Soroban.
        /// For production use, consider using Stellar JavaScript SDK.
        /// </remarks>
        private bool DecodeBooleanFromXdr(string xdrBase64)
        {
            try
            {
                // Decode XDR base64 string
                var xdrBytes = Convert.FromBase64String(xdrBase64);
                
                // Soroban SCVal boolean format:
                // - First 4 bytes: type discriminant (0x00000000 for SCV_BOOL)
                // - Next 4 bytes: value (0x00000000 for false, 0x00000001 for true)
                // Minimum 8 bytes required for a valid SCVal boolean
                
                if (xdrBytes.Length < 4)
                {
                    return false;
                }

                // Check for SCVal boolean type discriminant
                // SCV_BOOL = 0, SCV_TRUE = 1, SCV_FALSE = 0 (in older format)
                // In XDR big-endian format:
                // - Type 0 (SCV_BOOL) with value in next bytes
                // - Or Type 1 (SCV_VOID which we treat as false)
                
                // Check the type discriminant (first 4 bytes, big-endian)
                int typeDiscriminant = (xdrBytes[0] << 24) | (xdrBytes[1] << 16) | 
                                       (xdrBytes[2] << 8) | xdrBytes[3];

                // SCValType::SCV_BOOL = 0
                if (typeDiscriminant == 0 && xdrBytes.Length >= 8)
                {
                    // For SCV_BOOL, the value is in the next 4 bytes
                    int value = (xdrBytes[4] << 24) | (xdrBytes[5] << 16) | 
                               (xdrBytes[6] << 8) | xdrBytes[7];
                    return value != 0;
                }

                // SCValType::SCV_TRUE = 1 (alternative representation)
                if (typeDiscriminant == 1)
                {
                    return true;
                }

                // Default to false for unknown formats
                return false;
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException($"Invalid XDR format: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to decode XDR (using fallback): {ex.Message}");
                return false;
            }
        }

        private byte[] ConvertContractIdToBytes(string contractId)
        {
            // Remove common prefixes
            contractId = contractId.Replace("0x", "").Replace("C", "").Trim();

            // If it's a hex string, convert it
            if (contractId.All(c => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
            {
                return Enumerable.Range(0, contractId.Length)
                    .Where(x => x % 2 == 0)
                    .Select(x => Convert.ToByte(contractId.Substring(x, 2), 16))
                    .ToArray();
            }

            // Otherwise, treat as base64
            try
            {
                return Convert.FromBase64String(contractId);
            }
            catch
            {
                // Fallback: use UTF-8 encoding
                return Encoding.UTF8.GetBytes(contractId);
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        #region RPC Response Models

        private class RpcResponse<T>
        {
            [JsonPropertyName("jsonrpc")]
            public string JsonRpc { get; set; } = "2.0";

            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("result")]
            public T? Result { get; set; }

            [JsonPropertyName("error")]
            public RpcError? Error { get; set; }
        }

        private class RpcError
        {
            [JsonPropertyName("code")]
            public int Code { get; set; }

            [JsonPropertyName("message")]
            public string Message { get; set; } = string.Empty;

            [JsonPropertyName("data")]
            public object? Data { get; set; }
        }

        private class SimulateTransactionResponse
        {
            [JsonPropertyName("transactionData")]
            public string? TransactionData { get; set; }

            [JsonPropertyName("returnValue")]
            public string? ReturnValue { get; set; }

            [JsonPropertyName("error")]
            public string? Error { get; set; }

            [JsonPropertyName("cost")]
            public CostInfo? Cost { get; set; }
        }

        private class CostInfo
        {
            [JsonPropertyName("cpuInsns")]
            public string? CpuInsns { get; set; }

            [JsonPropertyName("memBytes")]
            public string? MemBytes { get; set; }
        }

        #endregion
    }
}

