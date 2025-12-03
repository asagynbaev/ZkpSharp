namespace ZkpSharp.Constants
{
    /// <summary>
    /// Constants used throughout the ZkpSharp library.
    /// </summary>
    public static class ZkpConstants
    {
        /// <summary>
        /// Default minimum age required for age verification (18 years).
        /// </summary>
        public const int DefaultRequiredAge = 18;

        /// <summary>
        /// Size of salt in bytes (32 bytes = 256 bits).
        /// </summary>
        public const int SaltSizeBytes = 32;

        /// <summary>
        /// Size of HMAC key in bytes (32 bytes = 256 bits).
        /// </summary>
        public const int HmacKeySizeBytes = 32;

        /// <summary>
        /// Date format string used for age and time condition proofs.
        /// </summary>
        public const string DateFormat = "yyyy-MM-dd";
    }
}


