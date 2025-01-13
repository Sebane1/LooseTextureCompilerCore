namespace LooseTextureCompilerCore {
    internal static class LtcUtility {
        public static string CreateMD5(string input) {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create()) {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                return Convert.ToHexString(hashBytes);
            }
        }

        public static string CreateIdentifier(string path, List<string> list) {
            string values = path;
            foreach (string value in list) {
                values += value;
            }
            return CreateMD5(values);
        }
    }
}
