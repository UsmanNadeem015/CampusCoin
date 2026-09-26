using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace CampusCoin.Services;

public class EmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendPasswordResetEmailAsync(
        string recipientEmail,
        string resetLink)
    {
        var smtpHost = _configuration["Email:SmtpHost"];
        var smtpPort = int.Parse(
            _configuration["Email:SmtpPort"] ?? "587"
        );
        var smtpUsername = _configuration["Email:SmtpUsername"];
        var smtpPassword = _configuration["Email:SmtpPassword"];
        var senderEmail = _configuration["Email:SenderEmail"];

        var message = new MimeMessage();

        message.From.Add(
            new MailboxAddress(
                "CampusCoin",
                senderEmail
            )
        );

        message.To.Add(
            MailboxAddress.Parse(recipientEmail)
        );

        message.Subject = "CampusCoin Password Reset";

        message.Body = new TextPart("html")
        {
            Text = $"""
                <h2>CampusCoin Password Reset</h2>

                <p>
                    We received a request to reset your CampusCoin password.
                </p>

                <p>
                    Click the button below to choose a new password:
                </p>

                <p>
                    <a href="{resetLink}"
                       style="display:inline-block;
                              padding:10px 18px;
                              background:#2563eb;
                              color:white;
                              text-decoration:none;
                              border-radius:5px;">
                        Reset Password
                    </a>
                </p>

                <p>
                    This link will expire in 30 minutes.
                </p>

                <p>
                    If you did not request this password reset,
                    you can safely ignore this email.
                </p>
                """
        };

        using var smtp = new SmtpClient();

        var connected = false;

        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                await smtp.ConnectAsync(
                    smtpHost,
                    smtpPort,
                    SecureSocketOptions.StartTls
                );

                connected = true;
                break;
            }
            catch (SmtpCommandException) when (attempt < 3)
            {
                await Task.Delay(2000);
            }
        }

        if (!connected)
        {
            throw new Exception(
                "Unable to connect to the Gmail SMTP server. Please try again later."
            );
        }

        await smtp.AuthenticateAsync(
            smtpUsername,
            smtpPassword
        );

        await smtp.SendAsync(message);

        await smtp.DisconnectAsync(true);
    }
}