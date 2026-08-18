using QRCoder;

namespace CampusHub.Access.Api.Infrastructure;

public static class QrPng
{
    public static byte[] FromText(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        return png.GetGraphic(8);
    }
}
