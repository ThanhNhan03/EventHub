using System;
using System.Collections.Generic;

namespace EventHub.Utilities
{
    public static class PasswordGenerator
    {
        private const string UppercaseChars = "ABCDEFGHJKLMNOPQRSTUVWXYZ";
        private const string LowercaseChars = "abcdefghijkmnopqrstuvwxyz";
        private const string DigitChars = "0123456789";
        // private const string SpecialChars = "!@#$%^&*()_-+=<>?";

        public static string GenerateRandomPassword(int length = 6)
        {
            var random = new Random();
            var chars = new List<char>();
            
            // Ensure at least one character from each required set
            chars.Add(UppercaseChars[random.Next(UppercaseChars.Length)]);
            chars.Add(LowercaseChars[random.Next(LowercaseChars.Length)]);
            chars.Add(DigitChars[random.Next(DigitChars.Length)]);
            // chars.Add(SpecialChars[random.Next(SpecialChars.Length)]);
            
            // Add additional random characters to reach desired length
            var allChars = UppercaseChars + LowercaseChars + DigitChars;
            for (int i = chars.Count; i < length; i++)
            {
                chars.Add(allChars[random.Next(allChars.Length)]);
            }
            
            // Shuffle the characters
            for (int i = 0; i < chars.Count; i++)
            {
                int swapIndex = random.Next(chars.Count);
                var temp = chars[i];
                chars[i] = chars[swapIndex];
                chars[swapIndex] = temp;
            }
            
            return new string(chars.ToArray());
        }
    }
}