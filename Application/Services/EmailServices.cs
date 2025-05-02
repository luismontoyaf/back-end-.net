using SendGrid;
using SendGrid.Helpers.Mail;
using Core.Models;

namespace Application.Services
{
    public class EmailService
    {
        private readonly string _apiKey;

        public EmailService(IConfiguration configuration)
        {
            _apiKey = configuration["SendGrid:ApiKey"];
        }

        public async Task SendInvoiceEmailAsync(string toEmail, string clientName, MemoryStream pdfStream, InvoiceData invoicedata)
        {
            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress("tucorreo@dominio.com", "Tu Empresa");
            var subject = "Factura de su compra";
            var to = new EmailAddress(toEmail, clientName);
            var plainTextContent = $"Hola {clientName}, adjuntamos la factura de su compra. ¡Gracias por confiar en nosotros!";
            var htmlContent = $"<strong>Hola {clientName}</strong><br>Adjuntamos la factura de su compra.<br><br>Gracias por confiar en nosotros.";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

            // Adjuntar PDF
            var pdfBytes = pdfStream.ToArray();
            var pdfBase64 = Convert.ToBase64String(pdfBytes);

            msg.AddAttachment("factura.pdf", pdfBase64, "application/pdf");

            var response = await client.SendEmailAsync(msg);

            if ((int)response.StatusCode >= 400)
            {
                var body = await response.Body.ReadAsStringAsync();
                throw new Exception($"Error enviando correo: {response.StatusCode} - {body}");
            }
        }
    }

}
