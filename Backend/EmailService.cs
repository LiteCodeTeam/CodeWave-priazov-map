using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Backend
{
    public static class EmailService
    {
        public static async Task SendPasswordResetEmail(string email, string resetLink, IConfigurationSection smpt)
        {
            // Реализуйте отправку через SMTP (MailKit, SendGrid и т. д.)
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Priazov-Map", "dedpylqwer@gmail.com"));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = "Сброс пароля";
            message.Body = new TextPart("plain")
            {
                Text = $"Здравствуйте!\n" +
                $"Для сброса пароля перейдите по ссылке: {resetLink}\n" +
                $"Если вы ничего не запрашивали проигнорируйте это сообщение"
            };

            using var client = new SmtpClient();
            await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smpt["Login"], smpt["Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
