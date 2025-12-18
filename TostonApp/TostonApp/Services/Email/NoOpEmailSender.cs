using Microsoft.AspNetCore.Identity.UI.Services;

namespace TostonApp.Services.Email
{
    // Para desarrollo/academia: no envía correos, solo evita el error del Register
    public sealed class NoOpEmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Aquí podrías loguear si quieres:
            // Console.WriteLine($"[EMAIL] To:{email} Subject:{subject}");
            return Task.CompletedTask;
        }
    }
}
