using System.Globalization;
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

        public EmailService(IConfiguration configuration)
        {
            _mailjetApiKey = configuration["MAILJET_API_KEY"];
            _mailjetSecretKey = configuration["MAILJET_SECRET_KEY"];
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
    }

}
