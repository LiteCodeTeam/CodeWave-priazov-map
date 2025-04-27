using Backend.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Backend
{
    public class EmailService
    {
        private readonly SmtpSettings _smtpSettings;

        public EmailService(IOptions<SmtpSettings> smtpSettings)
        {
            _smtpSettings = smtpSettings.Value;
        }

        public async Task SendPasswordResetEmail(string email, string resetCode)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Priazov-Impact", "priazovimpact@gmail.com"));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = "Сброс пароля";
            message.Body = new TextPart("html")
            {
                Text = $"""
                <div style ="
                margin: 20px;
                padding: 5px;
                border: groove 2px black;"><p style = "font-size: 20px">Здравствуйте!</p>
                <p style = "font-size: 20px">Код для сброса пароля: {resetCode}</p>
                <p style = "font-size: 20px">Если вы ничего не запрашивали проигнорируйте это сообщение</p></div>
                """
            };

            using var client = new SmtpClient();
            await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_smtpSettings.Login, _smtpSettings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        public async Task SendPasswordOkayEmail(string email)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Priazov-Impact", "priazovimpact@gmail.com"));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = "Пароль успешно изменён";
            message.Body = new TextPart("html")
            {
                Text = $"""
                <p style = "font-size: 20px">Здравствуйте!</p>
                <p style = "font-size: 20px">Ваш пароль был успешно изменён, если это были не вы
                срочно измените пароль по ссылке: </p>
                <p style = "font-size: 20px">Если вы ничего не запрашивали проигнорируйте это сообщение</p>
                """
            };

            using var client = new SmtpClient();
            await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_smtpSettings.Login, _smtpSettings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
