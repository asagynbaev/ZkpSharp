namespace ZkpSharp.Core
{
    public static class Utilities
    {
        public static int CalculateAge(DateTime dateOfBirth)
        {
            DateTime today = DateTime.UtcNow;
            int age = today.Year - dateOfBirth.Year;
            if (dateOfBirth > today.AddYears(-age)) age--; 
            return age;
        }
    }
}