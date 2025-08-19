# 🔐 JWT Key Generator PowerShell Script
# Sử dụng: .\generate-jwt-key.ps1

Write-Host "🔐 JWT Key Generator" -ForegroundColor Green
Write-Host "===================" -ForegroundColor Green

# 1. Tạo key ngẫu nhiên
Write-Host "`n1. Tạo key ngẫu nhiên (64 ký tự):" -ForegroundColor Yellow
$randomKey = [System.Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(64))
Write-Host "Key: $randomKey" -ForegroundColor Cyan

# 2. Tạo key từ passphrase
Write-Host "`n2. Tạo key từ passphrase:" -ForegroundColor Yellow
$passphrase = Read-Host "Nhập passphrase (hoặc Enter để dùng mặc định)"
if ([string]::IsNullOrEmpty($passphrase)) {
    $passphrase = "webmvc-secure-jwt-key-2024"
}

$bytes = [System.Text.Encoding]::UTF8.GetBytes($passphrase)
$hash = [System.Security.Cryptography.SHA256]::Create().ComputeHash($bytes)
$passphraseKey = [System.Convert]::ToBase64String($hash)
Write-Host "Passphrase: $passphrase" -ForegroundColor Gray
Write-Host "Key: $passphraseKey" -ForegroundColor Cyan

# 3. Tạo key với timestamp
Write-Host "`n3. Tạo key với timestamp:" -ForegroundColor Yellow
$timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$timestampKey = "webmvc_jwt_$timestamp" + "_" + $randomKey.Substring(0, 32)
Write-Host "Key: $timestampKey" -ForegroundColor Cyan

# 4. Hiển thị cấu hình cho appsettings.json
Write-Host "`n4. Cấu hình cho appsettings.json:" -ForegroundColor Yellow
Write-Host "{" -ForegroundColor White
Write-Host "  `"JwtSettings`": {" -ForegroundColor White
Write-Host "    `"Key`": `"$randomKey`"," -ForegroundColor White
Write-Host "    `"Issuer`": `"webMVC`"," -ForegroundColor White
Write-Host "    `"Audience`": `"webMVC-Users`"," -ForegroundColor White
Write-Host "    `"ExpirationHours`": `"24`"" -ForegroundColor White
Write-Host "  }" -ForegroundColor White
Write-Host "}" -ForegroundColor White

# 5. Hiển thị Environment Variable
Write-Host "`n5. Environment Variable cho Production:" -ForegroundColor Yellow
Write-Host "Windows:" -ForegroundColor Gray
Write-Host "set JWT_SECRET_KEY=$randomKey" -ForegroundColor Cyan
Write-Host "`nLinux/Mac:" -ForegroundColor Gray
Write-Host "export JWT_SECRET_KEY=$randomKey" -ForegroundColor Cyan

# 6. Lưu key vào file (tùy chọn)
$saveToFile = Read-Host "`nBạn có muốn lưu key vào file không? (y/n)"
if ($saveToFile -eq "y" -or $saveToFile -eq "Y") {
    $fileName = "jwt-key-$(Get-Date -Format 'yyyyMMdd-HHmmss').txt"
    $content = @"
JWT Key Generated on: $(Get-Date)
================================

1. Random Key (64 chars):
$randomKey

2. Passphrase Key:
Passphrase: $passphrase
Key: $passphraseKey

3. Timestamp Key:
$timestampKey

4. For appsettings.json:
{
  "JwtSettings": {
    "Key": "$randomKey",
    "Issuer": "webMVC",
    "Audience": "webMVC-Users",
    "ExpirationHours": "24"
  }
}

5. Environment Variable:
JWT_SECRET_KEY=$randomKey

⚠️  SECURITY NOTES:
- Keep this file secure and delete after use
- Never commit to version control
- Use Environment Variables in production
- Rotate keys regularly (3-6 months)
"@
    
    $content | Out-File -FilePath $fileName -Encoding UTF8
    Write-Host "`n✅ Key đã được lưu vào file: $fileName" -ForegroundColor Green
}

# 7. Lưu ý bảo mật
Write-Host "`n⚠️  LƯU Ý BẢO MẬT:" -ForegroundColor Red
Write-Host "- Không chia sẻ key này với ai" -ForegroundColor Yellow
Write-Host "- Sử dụng Environment Variables trong Production" -ForegroundColor Yellow
Write-Host "- Thay đổi key định kỳ để bảo mật" -ForegroundColor Yellow
Write-Host "- Không commit key vào Git repository" -ForegroundColor Yellow

Write-Host "`nNhấn Enter để thoát..." -ForegroundColor Gray
Read-Host
