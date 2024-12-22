namespace NadeoAPI
{
    internal static class Utils
    {
        public static string BuildQueryString(IEnumerable<KeyValuePair<string, string>> parameters)
        {
            return "?" + string.Join('&', parameters.Select(x => $"{x.Key}={x.Value}"));
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            if (base64EncodedData.Length % 4 != 0)
            {
                base64EncodedData += new string('=', 4 - base64EncodedData.Length % 4);
            }

            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
        public static DateTime ConvertEpochToDateTime(long epochTime)
        {
            // Define the Unix epoch start time
            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(epochTime);

            // Convert to DateTime
            return dateTimeOffset.UtcDateTime;
        }
    }
}
