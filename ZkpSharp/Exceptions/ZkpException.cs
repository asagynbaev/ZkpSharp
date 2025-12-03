namespace ZkpSharp.Exceptions
{
    /// <summary>
    /// Base exception class for all ZKP-related exceptions.
    /// </summary>
    public class ZkpException : Exception
    {
        public ZkpException(string message) : base(message)
        {
        }

        public ZkpException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when age verification fails due to insufficient age.
    /// </summary>
    public class InsufficientAgeException : ZkpException
    {
        public InsufficientAgeException(int requiredAge, int actualAge)
            : base($"Insufficient age. Required: {requiredAge}, Actual: {actualAge}")
        {
            RequiredAge = requiredAge;
            ActualAge = actualAge;
        }

        public int RequiredAge { get; }
        public int ActualAge { get; }
    }

    /// <summary>
    /// Exception thrown when balance verification fails due to insufficient balance.
    /// </summary>
    public class InsufficientBalanceException : ZkpException
    {
        public InsufficientBalanceException(double balance, double requestedAmount)
            : base($"Insufficient balance. Balance: {balance}, Requested: {requestedAmount}")
        {
            Balance = balance;
            RequestedAmount = requestedAmount;
        }

        public double Balance { get; }
        public double RequestedAmount { get; }
    }

    /// <summary>
    /// Exception thrown when proof verification fails.
    /// </summary>
    public class InvalidProofException : ZkpException
    {
        public InvalidProofException(string message) : base(message)
        {
        }

        public InvalidProofException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a value is out of the expected range.
    /// </summary>
    public class ValueOutOfRangeException : ZkpException
    {
        public ValueOutOfRangeException(double value, double minValue, double maxValue)
            : base($"Value {value} is out of range [{minValue}, {maxValue}]")
        {
            Value = value;
            MinValue = minValue;
            MaxValue = maxValue;
        }

        public double Value { get; }
        public double MinValue { get; }
        public double MaxValue { get; }
    }

    /// <summary>
    /// Exception thrown when a value does not belong to a valid set.
    /// </summary>
    public class ValueNotInSetException : ZkpException
    {
        public ValueNotInSetException(string value)
            : base($"Value '{value}' does not belong to the set of valid values")
        {
            Value = value;
        }

        public string Value { get; }
    }
}


