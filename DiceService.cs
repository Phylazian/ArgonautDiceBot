using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace ArgonautDiceBot
{
    public class DiceService
    {
        public int SecureRoll(int minInclusive, int maxInclusive)
        {
            return RandomNumberGenerator.GetInt32(minInclusive, maxInclusive + 1);
        }

        public (int total, string breakdown) Roll(string input)
        {
            var match = Regex.Match(input, @"(\d+)d(\d+)");
            if (!match.Success)
                return (0, "Invalid format. Use XdY.");

            int count = int.Parse(match.Groups[1].Value);
            int sides = int.Parse(match.Groups[2].Value);

            var rolls = new List<int>();
            for (int i = 0; i < count; i++)
                rolls.Add(SecureRoll(1, sides));

            int total = rolls.Sum();
            string breakdown = string.Join(", ", rolls);

            return (total, breakdown);
        }
    }
}
