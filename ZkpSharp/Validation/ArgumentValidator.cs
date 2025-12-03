namespace ZkpSharp.Validation
{
    /// <summary>
    /// Provides validation methods for method arguments.
    /// </summary>
    internal static class ArgumentValidator
    {
        /// <summary>
        /// Validates that a string argument is not null or empty.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="paramName">The name of the parameter.</param>
        /// <exception cref="ArgumentNullException">Thrown when value is null or empty.</exception>
        public static void ThrowIfNullOrEmpty(string? value, string paramName)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException($"{paramName} cannot be null or empty.", paramName);
            }
        }

        /// <summary>
        /// Validates that a value is not negative.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="paramName">The name of the parameter.</param>
        /// <exception cref="ArgumentException">Thrown when value is negative.</exception>
        public static void ThrowIfNegative(double value, string paramName)
        {
            if (value < 0)
            {
                throw new ArgumentException($"{paramName} cannot be negative.", paramName);
            }
        }

        /// <summary>
        /// Validates that a value is within the specified range.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="minValue">The minimum allowed value.</param>
        /// <param name="maxValue">The maximum allowed value.</param>
        /// <param name="paramName">The name of the parameter.</param>
        /// <exception cref="ArgumentException">Thrown when value is out of range.</exception>
        public static void ThrowIfOutOfRange(double value, double minValue, double maxValue, string paramName)
        {
            if (value < minValue || value > maxValue)
            {
                throw new ArgumentException(
                    $"{paramName} ({value}) is out of range [{minValue}, {maxValue}].", paramName);
            }
        }

        /// <summary>
        /// Validates that an array is not null or empty.
        /// </summary>
        /// <typeparam name="T">The type of array elements.</typeparam>
        /// <param name="array">The array to validate.</param>
        /// <param name="paramName">The name of the parameter.</param>
        /// <exception cref="ArgumentException">Thrown when array is null or empty.</exception>
        public static void ThrowIfNullOrEmpty<T>(T[]? array, string paramName)
        {
            if (array == null || array.Length == 0)
            {
                throw new ArgumentException($"{paramName} cannot be null or empty.", paramName);
            }
        }

        /// <summary>
        /// Validates that a date is not in the future.
        /// </summary>
        /// <param name="date">The date to validate.</param>
        /// <param name="paramName">The name of the parameter.</param>
        /// <exception cref="ArgumentException">Thrown when date is in the future.</exception>
        public static void ThrowIfFutureDate(DateTime date, string paramName)
        {
            if (date > DateTime.UtcNow)
            {
                throw new ArgumentException($"{paramName} cannot be in the future.", paramName);
            }
        }
    }
}


