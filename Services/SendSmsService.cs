using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;


public interface ISmsSender
{
    Task SendSmsAsync(string number, string message);
}

public class SendSmsService : ISmsSender
{
    private readonly ILogger<SendSmsService> _logger;

    public SendSmsService(ILogger<SendSmsService> logger)
    {
        _logger = logger;
    }

    public async Task SendSmsAsync(string number, string message)
    {
        if (string.IsNullOrWhiteSpace(number)) throw new ArgumentException("Phone number cannot be null or empty", nameof(number));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Message cannot be null or empty", nameof(message));

        try
        {
            Directory.CreateDirectory("MailsSave");
            var savePath = Path.Combine("MailsSave", $"{number}-{Guid.NewGuid()}.txt");
            await File.WriteAllTextAsync(savePath, message);
            _logger.LogInformation($"SMS saved to {savePath} for number {number}.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to save SMS. Error: {ex.Message}");
            throw;
        }

    }
}
