namespace CodeGenerator_Logic
{
    public class clsGlobal
    {
        public static string FormatId(string? input)
        {
            if (input == null)
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            if (input.EndsWith("id", StringComparison.OrdinalIgnoreCase) && input.Length >= 2)
            {
                char[] chars = input.ToCharArray();

                chars[^2] = 'I'; // Using index from end operator (C# 8.0+)
                chars[^1] = 'd';

                return new string(chars);
            }

            return input;
        }

    }
}
