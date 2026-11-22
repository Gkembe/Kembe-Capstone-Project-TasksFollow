using Resend;

namespace TasksFollow.Services
{
    public class EmailService
    {
        //le service qui communique avec l'API Resend
        private readonly IResend _resend;

        public EmailService(IResend resend)
        {
            _resend = resend;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var email = new EmailMessage();
            email.From = "TasksFollow <notifications@tasksfollow.com>";//l'adresse qui envoie les emails.
            email.To.Add(toEmail);
            email.Subject = subject;
            email.TextBody = body;

            await _resend.EmailSendAsync(email);
        }
    }
}
