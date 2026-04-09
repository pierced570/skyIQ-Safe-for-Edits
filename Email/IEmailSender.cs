namespace SkyIQ.Email
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
        Task SendWelcomeEmailAsync(string email);
        Task SendEndTrialEmailAsync(string email);
        Task SendEndTrialDaysEmailAsync(string email, int days);
        Task SendTripSummaryAsync(string email, string tripNum, string link);
        Task SendEmailWithAttachmentAsync(string email, string subject, string message, string attachmentPath);
        Task SendEmailWithAttachmentAsync(string email, string subject, string message, Dictionary<string, byte[]> attachments, string fileName);

    }
}
