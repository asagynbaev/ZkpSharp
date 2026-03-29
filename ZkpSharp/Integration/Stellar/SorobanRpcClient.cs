using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using StellarDotnetSdk;

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
                if (!string.IsNullOrEmpty(result.Error))
                    throw new InvalidOperationException($"Simulation failed: {result.Error}");

                var returnXdr = result.ReturnValue;
                if (string.IsNullOrEmpty(returnXdr) && result.Results is { Count: > 0 })
                    returnXdr = result.Results[0].Xdr;

                if (string.IsNullOrEmpty(returnXdr))
                    return false;

                return DecodeBoolReturnXdr(returnXdr);
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
        /// Decodes a Soroban <c>bool</c> return value from simulation (<c>results[0].xdr</c> or legacy <c>returnValue</c>).
        /// </summary>
        private static bool DecodeBoolReturnXdr(string xdrBase64)
        {
            try
            {
                var data = Convert.FromBase64String(xdrBase64);
                if (data.Length == 4)
                    return SorobanHelper.DecodeBoolFromScVal(xdrBase64);
            }
            catch (ArgumentException)
            {
                // Fall through to wider XDR layouts
            }

            return DecodeBooleanFromXdr(xdrBase64);
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
        private static bool DecodeBooleanFromXdr(string xdrBase64)
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

                // Unit variants ScTrue / ScFalse (discriminant-only ScVal, common on Soroban RPC)
                if (typeDiscriminant == 1)
                    return true;
                if (typeDiscriminant == 2)
                    return false;

                // Default to false for unknown formats
                return false;
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException($"Invalid XDR format: {ex.Message}", ex);
            }
            catch
            {
                return false;
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

            /// <summary>Host function results from current Soroban RPC (preferred over <see cref="ReturnValue"/>).</summary>
            [JsonPropertyName("results")]
            public List<SimulateTransactionHostResult>? Results { get; set; }

            [JsonPropertyName("error")]
            public string? Error { get; set; }

            [JsonPropertyName("cost")]
            public CostInfo? Cost { get; set; }
        }

        private class SimulateTransactionHostResult
        {
            [JsonPropertyName("xdr")]
            public string? Xdr { get; set; }
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

