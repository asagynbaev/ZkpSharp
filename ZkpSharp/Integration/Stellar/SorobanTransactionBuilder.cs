using System.Text;
using StellarDotnetSdk;
using StellarDotnetSdk.Accounts;
using StellarDotnetSdk.Responses;

namespace ZkpSharp.Integration.Stellar
{
    /// <summary>
    /// Builder for constructing Soroban smart contract invocation transactions.
    /// Provides methods for building transactions that call ZKP verification functions.
    /// </summary>
    public class SorobanTransactionBuilder
    {
        private readonly Network _network;
        private const uint BaseFee = 100;
        private const long DefaultTimeout = 30;

        /// <summary>
        /// Initializes a new instance of the <see cref="SorobanTransactionBuilder"/> class.
        /// </summary>
        /// <param name="network">The Stellar network to use.</param>
        public SorobanTransactionBuilder(Network network)
        {
            _network = network ?? throw new ArgumentNullException(nameof(network));
        }

        /// <summary>
        /// Builds a transaction XDR for invoking the verify_proof function.
        /// </summary>
        /// <param name="contractId">The smart contract address.</param>
        /// <param name="proof">The proof to verify (Base64 encoded).</param>
        /// <param name="data">The data that was proven.</param>
        /// <param name="salt">The salt used (Base64 encoded).</param>
        /// <param name="hmacKey">The HMAC key (Base64 encoded).</param>
        /// <returns>Base64-encoded transaction XDR ready for simulation.</returns>
        public string BuildVerifyProofTransaction(
            string contractId,
            string proof,
            string data,
            string salt,
            string hmacKey)
        {
            ValidateInputs(contractId, proof, salt);

            var invocation = new SorobanInvocation
            {
                ContractId = contractId,
                FunctionName = "verify_proof",
                Arguments = new[]
                {
                    CreateBytesN32Argument(proof),
                    CreateBytesArgument(data),
                    CreateBytesArgument(salt),
                    CreateBytesN32Argument(hmacKey)
                }
            };

            return BuildInvocationXdr(invocation);
        }

        /// <summary>
        /// Builds a transaction XDR for invoking the verify_proof function with a source account.
        /// </summary>
        /// <param name="sourceAccount">The source account for the transaction.</param>
        /// <param name="contractId">The smart contract address.</param>
        /// <param name="proof">The proof to verify (Base64 encoded).</param>
        /// <param name="data">The data that was proven.</param>
        /// <param name="salt">The salt used (Base64 encoded).</param>
        /// <param name="hmacKey">The HMAC key (Base64 encoded).</param>
        /// <returns>Base64-encoded transaction XDR ready for simulation.</returns>
        public string BuildVerifyProofTransactionWithAccount(
            AccountResponse sourceAccount,
            string contractId,
            string proof,
            string data,
            string salt,
            string hmacKey)
        {
            if (sourceAccount == null)
            {
                throw new ArgumentNullException(nameof(sourceAccount));
            }

            ValidateInputs(contractId, proof, salt);

            var invocation = new SorobanInvocation
            {
                ContractId = contractId,
                FunctionName = "verify_proof",
                Arguments = new[]
                {
                    CreateBytesN32Argument(proof),
                    CreateBytesArgument(data),
                    CreateBytesArgument(salt),
                    CreateBytesN32Argument(hmacKey)
                },
                SourceAccountId = sourceAccount.AccountId,
                SequenceNumber = sourceAccount.SequenceNumber
            };

            return BuildInvocationXdrWithAccount(invocation);
        }

        /// <summary>
        /// Builds a transaction XDR for invoking the verify_balance_proof function.
        /// </summary>
        /// <param name="contractId">The smart contract address.</param>
        /// <param name="proof">The proof to verify (Base64 encoded).</param>
        /// <param name="balanceData">The balance value as string.</param>
        /// <param name="requiredAmountData">The required amount as string.</param>
        /// <param name="salt">The salt used (Base64 encoded).</param>
        /// <param name="hmacKey">The HMAC key (Base64 encoded).</param>
        /// <returns>Base64-encoded transaction XDR ready for simulation.</returns>
        public string BuildVerifyBalanceProofTransaction(
            string contractId,
            string proof,
            string balanceData,
            string requiredAmountData,
            string salt,
            string hmacKey)
        {
            ValidateInputs(contractId, proof, salt);

            var invocation = new SorobanInvocation
            {
                ContractId = contractId,
                FunctionName = "verify_balance_proof",
                Arguments = new[]
                {
                    CreateBytesN32Argument(proof),
                    CreateBytesArgument(balanceData),
                    CreateBytesArgument(requiredAmountData),
                    CreateBytesArgument(salt),
                    CreateBytesN32Argument(hmacKey)
                }
            };

            return BuildInvocationXdr(invocation);
        }

        /// <summary>
        /// Builds a transaction XDR for invoking the verify_balance_proof function with a source account.
        /// </summary>
        public string BuildVerifyBalanceProofTransactionWithAccount(
            AccountResponse sourceAccount,
            string contractId,
            string proof,
            string balanceData,
            string requiredAmountData,
            string salt,
            string hmacKey)
        {
            if (sourceAccount == null)
            {
                throw new ArgumentNullException(nameof(sourceAccount));
            }

            ValidateInputs(contractId, proof, salt);

            var invocation = new SorobanInvocation
            {
                ContractId = contractId,
                FunctionName = "verify_balance_proof",
                Arguments = new[]
                {
                    CreateBytesN32Argument(proof),
                    CreateBytesArgument(balanceData),
                    CreateBytesArgument(requiredAmountData),
                    CreateBytesArgument(salt),
                    CreateBytesN32Argument(hmacKey)
                },
                SourceAccountId = sourceAccount.AccountId,
                SequenceNumber = sourceAccount.SequenceNumber
            };

            return BuildInvocationXdrWithAccount(invocation);
        }

        /// <summary>
        /// Builds a transaction XDR for invoking the verify_zk_range_proof function.
        /// </summary>
        /// <param name="contractId">The smart contract address.</param>
        /// <param name="proof">The ZK proof bytes (Base64 encoded).</param>
        /// <param name="commitment">The commitment (Base64 encoded, 33 bytes).</param>
        /// <param name="min">The minimum range value.</param>
        /// <param name="max">The maximum range value.</param>
        /// <returns>Base64-encoded transaction XDR ready for simulation.</returns>
        public string BuildVerifyZkRangeProofTransaction(
            string contractId,
            string proof,
            string commitment,
            long min,
            long max)
        {
            if (string.IsNullOrEmpty(contractId))
            {
                throw new ArgumentException("Contract ID cannot be null or empty.", nameof(contractId));
            }

            if (string.IsNullOrEmpty(proof))
            {
                throw new ArgumentException("Proof cannot be null or empty.", nameof(proof));
            }

            if (string.IsNullOrEmpty(commitment))
            {
                throw new ArgumentException("Commitment cannot be null or empty.", nameof(commitment));
            }

            var invocation = new SorobanInvocation
            {
                ContractId = contractId,
                FunctionName = "verify_zk_range_proof",
                Arguments = new[]
                {
                    CreateBytesArgument(proof, isBase64: true),
                    CreateBytesN33Argument(commitment),
                    CreateI64Argument(min),
                    CreateI64Argument(max)
                }
            };

            return BuildInvocationXdr(invocation);
        }

        /// <summary>
        /// Builds a transaction XDR for invoking the verify_zk_age_proof function.
        /// </summary>
        public string BuildVerifyZkAgeProofTransaction(
            string contractId,
            string proof,
            string commitment,
            uint minAge)
        {
            if (string.IsNullOrEmpty(contractId))
            {
                throw new ArgumentException("Contract ID cannot be null or empty.", nameof(contractId));
            }

            var invocation = new SorobanInvocation
            {
                ContractId = contractId,
                FunctionName = "verify_zk_age_proof",
                Arguments = new[]
                {
                    CreateBytesArgument(proof, isBase64: true),
                    CreateBytesN33Argument(commitment),
                    CreateU32Argument(minAge)
                }
            };

            return BuildInvocationXdr(invocation);
        }

        /// <summary>
        /// Builds a transaction XDR for invoking the verify_zk_balance_proof function.
        /// </summary>
        public string BuildVerifyZkBalanceProofTransaction(
            string contractId,
            string proof,
            string commitment,
            long requiredAmount)
        {
            if (string.IsNullOrEmpty(contractId))
            {
                throw new ArgumentException("Contract ID cannot be null or empty.", nameof(contractId));
            }

            var invocation = new SorobanInvocation
            {
                ContractId = contractId,
                FunctionName = "verify_zk_balance_proof",
                Arguments = new[]
                {
                    CreateBytesArgument(proof, isBase64: true),
                    CreateBytesN33Argument(commitment),
                    CreateI64Argument(requiredAmount)
                }
            };

            return BuildInvocationXdr(invocation);
        }

        private void ValidateInputs(string contractId, string proof, string salt)
        {
            if (string.IsNullOrEmpty(contractId))
            {
                throw new ArgumentException("Contract ID cannot be null or empty.", nameof(contractId));
            }

            if (string.IsNullOrEmpty(proof))
            {
                throw new ArgumentException("Proof cannot be null or empty.", nameof(proof));
            }

            if (string.IsNullOrEmpty(salt))
            {
                throw new ArgumentException("Salt cannot be null or empty.", nameof(salt));
            }
        }

        private string BuildInvocationXdr(SorobanInvocation invocation)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            WriteInvokeHostFunctionOp(writer, invocation);

            return Convert.ToBase64String(ms.ToArray());
        }

        private string BuildInvocationXdrWithAccount(SorobanInvocation invocation)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            WriteTransactionEnvelope(writer, invocation);

            return Convert.ToBase64String(ms.ToArray());
        }

        private void WriteTransactionEnvelope(BinaryWriter writer, SorobanInvocation invocation)
        {
            WriteInt32BigEndian(writer, 0);
            
            WriteBytes(writer, _network.NetworkId);
            
            WriteInt32BigEndian(writer, 0);
            
            if (!string.IsNullOrEmpty(invocation.SourceAccountId))
            {
                WritePublicKey(writer, invocation.SourceAccountId);
            }
            else
            {
                writer.Write(new byte[32]);
            }

            WriteUInt32BigEndian(writer, BaseFee);
            
            WriteInt64BigEndian(writer, invocation.SequenceNumber + 1);

            WriteInt32BigEndian(writer, 0);
            
            WriteInt32BigEndian(writer, 0);
            WriteInt64BigEndian(writer, DateTimeOffset.UtcNow.AddSeconds(DefaultTimeout).ToUnixTimeSeconds());
            
            WriteInvokeHostFunctionOp(writer, invocation);
            
            WriteInt32BigEndian(writer, 0);
        }

        private void WriteInvokeHostFunctionOp(BinaryWriter writer, SorobanInvocation invocation)
        {
            WriteInt32BigEndian(writer, 24);
            
            WriteInt32BigEndian(writer, 0);
            
            WriteContractId(writer, invocation.ContractId);
            
            WriteSymbol(writer, invocation.FunctionName);
            
            WriteInt32BigEndian(writer, invocation.Arguments.Length);
            foreach (var arg in invocation.Arguments)
            {
                writer.Write(arg);
            }

            WriteInt32BigEndian(writer, 0);
        }

        private void WriteContractId(BinaryWriter writer, string contractId)
        {
            WriteInt32BigEndian(writer, 1);
            
            var contractBytes = DecodeContractId(contractId);
            writer.Write(contractBytes);
        }

        private byte[] DecodeContractId(string contractId)
        {
            if (contractId.StartsWith("C"))
            {
                try
                {
                    return StrKey.DecodeContractId(contractId);
                }
                catch
                {
                }
            }

            if (contractId.Length == 64 && contractId.All(c => 
                (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
            {
                return Enumerable.Range(0, 32)
                    .Select(i => Convert.ToByte(contractId.Substring(i * 2, 2), 16))
                    .ToArray();
            }

            try
            {
                var bytes = Convert.FromBase64String(contractId);
                if (bytes.Length == 32)
                {
                    return bytes;
                }
            }
            catch { }

            throw new ArgumentException($"Invalid contract ID format: {contractId}", nameof(contractId));
        }

        private void WriteSymbol(BinaryWriter writer, string symbol)
        {
            WriteInt32BigEndian(writer, 15);
            
            var bytes = Encoding.UTF8.GetBytes(symbol);
            WriteInt32BigEndian(writer, bytes.Length);
            writer.Write(bytes);
            
            var padding = (4 - (bytes.Length % 4)) % 4;
            for (int i = 0; i < padding; i++)
            {
                writer.Write((byte)0);
            }
        }

        private void WritePublicKey(BinaryWriter writer, string accountId)
        {
            WriteInt32BigEndian(writer, 0);
            var keyPair = KeyPair.FromAccountId(accountId);
            writer.Write(keyPair.PublicKey);
        }

        private void WriteString(BinaryWriter writer, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            WriteInt32BigEndian(writer, bytes.Length);
            writer.Write(bytes);
            
            var padding = (4 - (bytes.Length % 4)) % 4;
            for (int i = 0; i < padding; i++)
            {
                writer.Write((byte)0);
            }
        }

        private void WriteBytes(BinaryWriter writer, byte[] bytes)
        {
            WriteInt32BigEndian(writer, bytes.Length);
            writer.Write(bytes);
            
            var padding = (4 - (bytes.Length % 4)) % 4;
            for (int i = 0; i < padding; i++)
            {
                writer.Write((byte)0);
            }
        }

        private void WriteInt32BigEndian(BinaryWriter writer, int value)
        {
            writer.Write((byte)((value >> 24) & 0xFF));
            writer.Write((byte)((value >> 16) & 0xFF));
            writer.Write((byte)((value >> 8) & 0xFF));
            writer.Write((byte)(value & 0xFF));
        }

        private void WriteUInt32BigEndian(BinaryWriter writer, uint value)
        {
            writer.Write((byte)((value >> 24) & 0xFF));
            writer.Write((byte)((value >> 16) & 0xFF));
            writer.Write((byte)((value >> 8) & 0xFF));
            writer.Write((byte)(value & 0xFF));
        }

        private void WriteInt64BigEndian(BinaryWriter writer, long value)
        {
            writer.Write((byte)((value >> 56) & 0xFF));
            writer.Write((byte)((value >> 48) & 0xFF));
            writer.Write((byte)((value >> 40) & 0xFF));
            writer.Write((byte)((value >> 32) & 0xFF));
            writer.Write((byte)((value >> 24) & 0xFF));
            writer.Write((byte)((value >> 16) & 0xFF));
            writer.Write((byte)((value >> 8) & 0xFF));
            writer.Write((byte)(value & 0xFF));
        }

        private byte[] CreateBytesN32Argument(string base64Value)
        {
            var bytes = Convert.FromBase64String(base64Value);
            
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            
            WriteInt32BigEndian(writer, 64);
            WriteInt32BigEndian(writer, 32);
            
            if (bytes.Length >= 32)
            {
                writer.Write(bytes, 0, 32);
            }
            else
            {
                writer.Write(bytes);
                writer.Write(new byte[32 - bytes.Length]);
            }
            
            return ms.ToArray();
        }

        private byte[] CreateBytesN33Argument(string base64Value)
        {
            var bytes = Convert.FromBase64String(base64Value);
            
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            
            WriteInt32BigEndian(writer, 64);
            WriteInt32BigEndian(writer, 33);
            
            if (bytes.Length >= 33)
            {
                writer.Write(bytes, 0, 33);
            }
            else
            {
                writer.Write(bytes);
                writer.Write(new byte[33 - bytes.Length]);
            }
            
            var padding = (4 - (33 % 4)) % 4;
            for (int i = 0; i < padding; i++)
            {
                writer.Write((byte)0);
            }
            
            return ms.ToArray();
        }

        private byte[] CreateBytesArgument(string value, bool isBase64 = false)
        {
            byte[] bytes;
            if (isBase64)
            {
                bytes = Convert.FromBase64String(value);
            }
            else
            {
                bytes = Encoding.UTF8.GetBytes(value);
            }
            
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            
            WriteInt32BigEndian(writer, 14);
            WriteInt32BigEndian(writer, bytes.Length);
            writer.Write(bytes);
            
            var padding = (4 - (bytes.Length % 4)) % 4;
            for (int i = 0; i < padding; i++)
            {
                writer.Write((byte)0);
            }
            
            return ms.ToArray();
        }

        private byte[] CreateI64Argument(long value)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            
            WriteInt32BigEndian(writer, 6);
            WriteInt64BigEndian(writer, value);
            
            return ms.ToArray();
        }

        private byte[] CreateU32Argument(uint value)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            
            WriteInt32BigEndian(writer, 4);
            WriteUInt32BigEndian(writer, value);
            
            return ms.ToArray();
        }

        private class SorobanInvocation
        {
            public string ContractId { get; set; } = string.Empty;
            public string FunctionName { get; set; } = string.Empty;
            public byte[][] Arguments { get; set; } = Array.Empty<byte[]>();
            public string? SourceAccountId { get; set; }
            public long SequenceNumber { get; set; }
        }
    }

    /// <summary>
    /// Utility class for Stellar StrKey encoding/decoding.
    /// </summary>
    internal static class StrKey
    {
        private const byte VersionByteContract = 0x02 << 3;

        public static byte[] DecodeContractId(string contractId)
        {
            if (string.IsNullOrEmpty(contractId) || contractId[0] != 'C')
            {
                throw new ArgumentException("Invalid contract ID format", nameof(contractId));
            }

            var decoded = Base32Decode(contractId);
            if (decoded.Length != 35)
            {
                throw new ArgumentException("Invalid contract ID length", nameof(contractId));
            }

            var payload = new byte[32];
            Array.Copy(decoded, 1, payload, 0, 32);
            
            return payload;
        }

        private static byte[] Base32Decode(string input)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            
            input = input.TrimEnd('=').ToUpperInvariant();
            
            var bits = 0;
            var value = 0;
            var output = new List<byte>();

            foreach (var c in input)
            {
                var index = alphabet.IndexOf(c);
                if (index < 0)
                {
                    throw new ArgumentException($"Invalid character in base32 string: {c}");
                }

                value = (value << 5) | index;
                bits += 5;

                if (bits >= 8)
                {
                    output.Add((byte)((value >> (bits - 8)) & 0xFF));
                    bits -= 8;
                }
            }

            return output.ToArray();
        }
    }
}
