// using webMVC.Utilities;

// class Program
// {
//     static void Main(string[] args)
//     {
//         Console.WriteLine("🔐 JWT Key Generator");
//         Console.WriteLine("===================");
        
//         Console.WriteLine("\n1. Tạo key ngẫu nhiên (64 ký tự):");
//         var randomKey = JwtKeyGenerator.GenerateSecureKey();
//         Console.WriteLine($"Key: {randomKey}");
        
//         Console.WriteLine("\n2. Tạo key từ passphrase:");
//         Console.Write("Nhập passphrase: ");
//         var passphrase = Console.ReadLine() ?? "webmvc-secure-passphrase-2024";
//         var passphraseKey = JwtKeyGenerator.GenerateKeyFromPassphrase(passphrase);
//         Console.WriteLine($"Key: {passphraseKey}");
        
//         Console.WriteLine("\n3. Tạo key với format đặc biệt:");
//         var formattedKey = JwtKeyGenerator.GenerateFormattedKey();
//         Console.WriteLine($"Key: {formattedKey}");
        
//         Console.WriteLine("\n4. Tạo key cho appsettings.json:");
//         Console.WriteLine("{\n  \"JwtSettings\": {");
//         Console.WriteLine($"    \"Key\": \"{randomKey}\",");
//         Console.WriteLine("    \"Issuer\": \"webMVC\",");
//         Console.WriteLine("    \"Audience\": \"webMVC-Users\",");
//         Console.WriteLine("    \"ExpirationHours\": \"24\"");
//         Console.WriteLine("  }\n}");
        
//         Console.WriteLine("\n⚠️  Lưu ý:");
//         Console.WriteLine("- Không chia sẻ key này với ai");
//         Console.WriteLine("- Sử dụng Environment Variables trong Production");
//         Console.WriteLine("- Thay đổi key định kỳ để bảo mật");
        
//         Console.WriteLine("\nNhấn Enter để thoát...");
//         Console.ReadLine();
//     }
// }
