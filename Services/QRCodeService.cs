using QRCoder;


public interface IQRCodeService
{
    string GenerateQrCode(string qrCodeUri);
}

public class QRCodeService : IQRCodeService
{
    public string GenerateQrCode(string qrCodeUri)
    {
        if (string.IsNullOrWhiteSpace(qrCodeUri))
            throw new ArgumentException("QR Code URI cannot be null or empty.", nameof(qrCodeUri));

        using (var qrGenerator = new QRCodeGenerator())
        {
            var qrCodeData = qrGenerator.CreateQrCode(qrCodeUri, QRCodeGenerator.ECCLevel.Q);
            using (var qrCode = new PngByteQRCode(qrCodeData))
            {
                var qrCodeImage = qrCode.GetGraphic(20);
                return $"data:image/png;base64,{Convert.ToBase64String(qrCodeImage)}";
            }
        }
    }
}
