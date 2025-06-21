namespace CodeGenerator_Logic
{
    public class ClsGlobal
    {
        public static string FormatId(string? input, bool smallD = true)
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

                chars[^2] = 'I';
                chars[^1] = smallD ? 'd' : 'D';

                return new string(chars);
            }

            return input;
        }

    }
}
