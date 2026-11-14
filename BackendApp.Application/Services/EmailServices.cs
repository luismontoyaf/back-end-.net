using System.Globalization;
using System.Net;
using Core.Models;
using Mailjet.Client;
using Mailjet.Client.Resources;
using Mailjet.Client.TransactionalEmails;
using Newtonsoft.Json.Linq;

namespace Application.Services
{
    public class EmailService
    {
        private readonly string _mailjetApiKey;
        private readonly string _mailjetSecretKey;
        private readonly string? _mailjetSenderEmail;
        private readonly string? _mailjetSenderName;

        public EmailService(IConfiguration configuration)
        {
            _mailjetApiKey = configuration["MAILJET_API_KEY"];
            _mailjetSecretKey = configuration["MAILJET_SECRET_KEY"];
            _mailjetSenderEmail = configuration["MAILJET_SENDER_EMAIL"];
            _mailjetSenderName = configuration["MAILJET_SENDER_NAME"];
        }

        public async Task SendInvoiceEmailAsync(string toEmail, string clientName, string companyName, string companyEmail, MemoryStream pdfStream, InvoiceData invoicedata)
        {
            var client = new MailjetClient(_mailjetApiKey, _mailjetSecretKey);

            // Convertir PDF a Base64
            pdfStream.Position = 0;
            var pdfBytes = pdfStream.ToArray();
            var pdfBase64 = Convert.ToBase64String(pdfBytes);

            var attachment = new Attachment("factura.pdf", "application/pdf", pdfBase64);

            // Construir el correo
            var email = new TransactionalEmailBuilder() 
            .WithFrom(new SendContact(companyEmail, companyName)) 
            .WithSubject("Factura de compra") .WithTextPart($"Hola {clientName}, adjuntamos la factura de su compra.")
            .WithHtmlPart($"<strong>Hola {clientName}</strong><br>Adjuntamos la factura de su compra.") 
            .WithTo(new SendContact(toEmail, clientName)) 
            .WithAttachment(attachment) 
            .Build();

            // Enviar correo
            var response = await client.SendTransactionalEmailAsync(email);

            if (response.Messages[0].Status != "success")
            {
                throw new Exception($"Error enviando correo: {response.Messages[0].Errors?[0].ErrorMessage}");
            }
        }

        public async Task SendPasswordResetEmailAsync(
            string toEmail,
            string userName,
            string companyName,
            string companyEmail,
            string resetUrl)
        {
            var senderEmail = _mailjetSenderEmail ?? companyEmail;
            var senderName = _mailjetSenderName ?? companyName;

            if (string.IsNullOrWhiteSpace(_mailjetApiKey) ||
                string.IsNullOrWhiteSpace(_mailjetSecretKey) ||
                string.IsNullOrWhiteSpace(senderEmail))
            {
                throw new InvalidOperationException("Falta configurar el remitente o las credenciales de Mailjet.");
            }

            var safeUserName = WebUtility.HtmlEncode(userName);
            var safeResetUrl = WebUtility.HtmlEncode(resetUrl);
            var email = new TransactionalEmailBuilder()
                .WithFrom(new SendContact(senderEmail, senderName))
                .WithSubject("Restablece tu contraseña de Lumo")
                .WithTextPart($"Hola {userName}, usa este enlace para crear una nueva contraseña: {resetUrl}. El enlace vence en 30 minutos.")
                .WithHtmlPart(
                    $"<p>Hola {safeUserName},</p>" +
                    "<p>Recibimos una solicitud para restablecer tu contraseña de Lumo.</p>" +
                    $"<p><a href=\"{safeResetUrl}\">Crear una nueva contraseña</a></p>" +
                    "<p>Este enlace vence en 30 minutos. Si no solicitaste el cambio, puedes ignorar este mensaje.</p>")
                .WithTo(new SendContact(toEmail, userName))
                .Build();

            var client = new MailjetClient(_mailjetApiKey, _mailjetSecretKey);
            var response = await client.SendTransactionalEmailAsync(email);

            if (!response.Messages.Any() || response.Messages[0].Status != "success")
            {
                throw new InvalidOperationException("Mailjet no aceptó el correo de restablecimiento.");
            }
        }

        public async Task SendSubscriptionOverdueReminderAsync(
            string toEmail,
            string recipientName,
            string companyName,
            string senderEmail,
            DateTime dueDate)
        {
            if (string.IsNullOrWhiteSpace(_mailjetApiKey) ||
                string.IsNullOrWhiteSpace(_mailjetSecretKey) ||
                string.IsNullOrWhiteSpace(senderEmail))
            {
                throw new InvalidOperationException("Falta configurar el remitente o las credenciales de Mailjet.");
            }

            var safeRecipientName = WebUtility.HtmlEncode(recipientName);
            var safeCompanyName = WebUtility.HtmlEncode(companyName);
            var formattedDueDate = dueDate.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("es-CO"));
            var email = new TransactionalEmailBuilder()
                .WithFrom(new SendContact(senderEmail, _mailjetSenderName ?? "Lumo"))
                .WithSubject($"Recordatorio de pago pendiente - {companyName}")
                .WithTextPart(
                    $"Hola {recipientName}, la obligación mensual de {companyName} con vencimiento " +
                    $"el {formattedDueDate} está pendiente. Por favor, ponte en contacto con Lumo para registrar el pago.")
                .WithHtmlPart(
                    $"<p>Hola {safeRecipientName},</p>" +
                    $"<p>La obligación mensual de <strong>{safeCompanyName}</strong> con vencimiento " +
                    $"el <strong>{formattedDueDate}</strong> continúa pendiente.</p>" +
                    "<p>Por favor, ponte en contacto con Lumo para resolver el pago.</p>")
                .WithTo(new SendContact(toEmail, recipientName))
                .Build();

            var client = new MailjetClient(_mailjetApiKey, _mailjetSecretKey);
            var response = await client.SendTransactionalEmailAsync(email);
            if (!response.Messages.Any() || response.Messages[0].Status != "success")
                throw new InvalidOperationException("Mailjet no aceptó el recordatorio de pago.");
        }

    }

}
