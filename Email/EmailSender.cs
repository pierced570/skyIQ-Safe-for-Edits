using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using MimeKit;
using MimeKit.Text;
using SkyIQ.Data;

using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace SkyIQ.Email
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailSettings _emailSettings;
        private readonly IHostingEnvironment _env;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _host;
        private readonly ApplicationDbContext _context;

        public EmailSender(IOptions<EmailSettings> emailSettings, IHostingEnvironment env, IConfiguration config, IWebHostEnvironment host, ApplicationDbContext context)
        {
            _emailSettings = emailSettings.Value;
            _env = env;
            _config = config;
            _context = context;
            _host = host;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            try
            {
                //request confirmation to org	
                var webRoot = _host.WebRootPath;
                var pathToFile = webRoot
                                + Path.DirectorySeparatorChar.ToString()
                                + "templates"
                                + Path.DirectorySeparatorChar.ToString()
                                + "email-general-notice.html";

                var builder = new BodyBuilder();
                using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
                {
                    builder.HtmlBody = SourceReader.ReadToEnd();
                }
                var htmlBody = builder.HtmlBody;

                var messageBody = string.Format(htmlBody, subject, message);




                var mimeMessage = new MimeMessage();
                mimeMessage.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.Sender));
                mimeMessage.To.Add(new MailboxAddress(email, email));

                mimeMessage.Subject = subject;

                mimeMessage.Body = new TextPart(TextFormat.Html)
                {
                    Text = messageBody
                };

                using (var client = new SmtpClient())
                {
                    // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    if (_env.IsDevelopment())
                    {
                        // The third parameter is useSSL (true if the client should make an SSL-wrapped
                        // connection to the server; otherwise, false).
                        await client.ConnectAsync(_emailSettings.MailServer, _emailSettings.MailPort, SecureSocketOptions.StartTls);
                    }
                    else
                    {
                        await client.ConnectAsync(_emailSettings.MailServer);
                    }

                    // Note: only needed if the SMTP server requires authentication
                    await client.AuthenticateAsync(_emailSettings.Sender, _emailSettings.Password);

                    await client.SendAsync(mimeMessage);

                    await client.DisconnectAsync(true);
                }

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }
        public async Task SendEndTrialEmailAsync(string email)
        {
            try
            {
                //request confirmation to org	
                var webRoot = _host.WebRootPath;
                var pathToFile = webRoot
                                + Path.DirectorySeparatorChar.ToString()
                                + "templates"
                                + Path.DirectorySeparatorChar.ToString()
                                + "email-end-free-trial.html";

                var builder = new BodyBuilder();
                using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
                {
                    builder.HtmlBody = SourceReader.ReadToEnd();
                }
                var htmlBody = builder.HtmlBody;
                string subject = "End of Free Trial";
                var messageBody = string.Format(htmlBody, subject);




                var mimeMessage = new MimeMessage();
                mimeMessage.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.Sender));
                mimeMessage.To.Add(new MailboxAddress(email, email));

                mimeMessage.Subject = subject;

                mimeMessage.Body = new TextPart(TextFormat.Html)
                {
                    Text = messageBody
                };

                using (var client = new SmtpClient())
                {
                    // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    if (_env.IsDevelopment())
                    {
                        // The third parameter is useSSL (true if the client should make an SSL-wrapped
                        // connection to the server; otherwise, false).
                        await client.ConnectAsync(_emailSettings.MailServer, _emailSettings.MailPort, SecureSocketOptions.StartTls);
                    }
                    else
                    {
                        await client.ConnectAsync(_emailSettings.MailServer);
                    }

                    // Note: only needed if the SMTP server requires authentication
                    await client.AuthenticateAsync(_emailSettings.Sender, _emailSettings.Password);

                    await client.SendAsync(mimeMessage);

                    await client.DisconnectAsync(true);
                }

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }
        public async Task SendEndTrialDaysEmailAsync(string email, int days)
        {
            try
            {
                //request confirmation to org	
                var webRoot = _host.WebRootPath;
                var pathToFile = webRoot
                                + Path.DirectorySeparatorChar.ToString()
                                + "templates"
                                + Path.DirectorySeparatorChar.ToString()
                                + "email-end-free-trial-days.html";

                var builder = new BodyBuilder();
                using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
                {
                    builder.HtmlBody = SourceReader.ReadToEnd();
                }
                var htmlBody = builder.HtmlBody;
                string subject = "Free Trial Ending";
                var messageBody = string.Format(htmlBody, subject, days);




                var mimeMessage = new MimeMessage();
                mimeMessage.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.Sender));
                mimeMessage.To.Add(new MailboxAddress(email, email));

                mimeMessage.Subject = subject;

                mimeMessage.Body = new TextPart(TextFormat.Html)
                {
                    Text = messageBody
                };

                using (var client = new SmtpClient())
                {
                    // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    if (_env.IsDevelopment())
                    {
                        // The third parameter is useSSL (true if the client should make an SSL-wrapped
                        // connection to the server; otherwise, false).
                        await client.ConnectAsync(_emailSettings.MailServer, _emailSettings.MailPort, SecureSocketOptions.StartTls);
                    }
                    else
                    {
                        await client.ConnectAsync(_emailSettings.MailServer);
                    }

                    // Note: only needed if the SMTP server requires authentication
                    await client.AuthenticateAsync(_emailSettings.Sender, _emailSettings.Password);

                    await client.SendAsync(mimeMessage);

                    await client.DisconnectAsync(true);
                }

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }
        public async Task SendWelcomeEmailAsync(string email)
        {
            try
            {
                //request confirmation to org	
                var webRoot = _host.WebRootPath;
                var pathToFile = webRoot
                                + Path.DirectorySeparatorChar.ToString()
                                + "templates"
                                + Path.DirectorySeparatorChar.ToString()
                                + "email-welcome.html";

                var builder = new BodyBuilder();
                using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
                {
                    builder.HtmlBody = SourceReader.ReadToEnd();
                }
                var htmlBody = builder.HtmlBody;
                string subject = "Welcome Aboard skyIQ";
                var messageBody = string.Format(htmlBody, subject);




                var mimeMessage = new MimeMessage();
                mimeMessage.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.Sender));
                mimeMessage.To.Add(new MailboxAddress(email, email));

                mimeMessage.Subject = subject;

                mimeMessage.Body = new TextPart(TextFormat.Html)
                {
                    Text = messageBody
                };

                using (var client = new SmtpClient())
                {
                    // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    if (_env.IsDevelopment())
                    {
                        // The third parameter is useSSL (true if the client should make an SSL-wrapped
                        // connection to the server; otherwise, false).
                        await client.ConnectAsync(_emailSettings.MailServer, _emailSettings.MailPort, SecureSocketOptions.StartTls);
                    }
                    else
                    {
                        await client.ConnectAsync(_emailSettings.MailServer);
                    }

                    // Note: only needed if the SMTP server requires authentication
                    await client.AuthenticateAsync(_emailSettings.Sender, _emailSettings.Password);

                    await client.SendAsync(mimeMessage);

                    await client.DisconnectAsync(true);
                }

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }
        public async Task SendTripSummaryAsync(string email, string tripNum, string link)
        {
            try
            {
                //request confirmation to org	
                var webRoot = _host.WebRootPath;
                var pathToFile = webRoot
                                + Path.DirectorySeparatorChar.ToString()
                                + "templates"
                                + Path.DirectorySeparatorChar.ToString()
                                + "email-trip-summary.html";

                var builder = new BodyBuilder();
                using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
                {
                    builder.HtmlBody = SourceReader.ReadToEnd();
                }
                var htmlBody = builder.HtmlBody;

                var messageBody = string.Format(htmlBody, tripNum, link);




                var mimeMessage = new MimeMessage();
                mimeMessage.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.Sender));
                mimeMessage.To.Add(new MailboxAddress(email, email));

                mimeMessage.Subject = "Trip " + tripNum + " Summary";

                mimeMessage.Body = new TextPart(TextFormat.Html)
                {
                    Text = messageBody
                };

                using (var client = new SmtpClient())
                {
                    // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    if (_env.IsDevelopment())
                    {
                        // The third parameter is useSSL (true if the client should make an SSL-wrapped
                        // connection to the server; otherwise, false).
                        await client.ConnectAsync(_emailSettings.MailServer, _emailSettings.MailPort, SecureSocketOptions.StartTls);
                    }
                    else
                    {
                        await client.ConnectAsync(_emailSettings.MailServer);
                    }

                    // Note: only needed if the SMTP server requires authentication
                    await client.AuthenticateAsync(_emailSettings.Sender, _emailSettings.Password);

                    await client.SendAsync(mimeMessage);

                    await client.DisconnectAsync(true);
                }

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }
        public async Task SendEmailWithAttachmentAsync(string email, string tripNum, string link, string attachmentPath)
        {
            try
            {
                //request confirmation to org	
                var webRoot = _host.WebRootPath;
                var pathToFile = webRoot
                                + Path.DirectorySeparatorChar.ToString()
                                + "templates"
                                + Path.DirectorySeparatorChar.ToString()
                                + "email-trip-summary.html";

                var builder = new BodyBuilder();
                using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
                {
                    builder.HtmlBody = SourceReader.ReadToEnd();
                }
                var htmlBody = builder.HtmlBody;
                var messageBody = string.Format(htmlBody, tripNum, link);


                var mimeMessage = new MimeMessage();
                mimeMessage.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.Sender));
                mimeMessage.To.Add(new MailboxAddress(email, email));
                mimeMessage.Subject = "Trip " + tripNum + " Summary";

                builder.TextBody = messageBody;
                builder.Attachments.Add(attachmentPath);
                mimeMessage.Body = builder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    if (_env.IsDevelopment())
                    {
                        // The third parameter is useSSL (true if the client should make an SSL-wrapped
                        // connection to the server; otherwise, false).
                        await client.ConnectAsync(_emailSettings.MailServer, _emailSettings.MailPort, SecureSocketOptions.StartTls);
                    }
                    else
                    {
                        await client.ConnectAsync(_emailSettings.MailServer);
                    }

                    // Note: only needed if the SMTP server requires authentication
                    await client.AuthenticateAsync(_emailSettings.Sender, _emailSettings.Password);

                    await client.SendAsync(mimeMessage);

                    await client.DisconnectAsync(true);
                }

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }
        public async Task SendEmailWithAttachmentAsync(string email, string tripNum, string link, Dictionary<string, byte[]> attachments, string fileName)
        {
            try
            {
                //request confirmation to org	
                var webRoot = _host.WebRootPath;
                var pathToFile = webRoot
                                + Path.DirectorySeparatorChar.ToString()
                                + "templates"
                                + Path.DirectorySeparatorChar.ToString()
                                + "email-trip-summary.html";

                var builder = new BodyBuilder();
                using (StreamReader SourceReader = System.IO.File.OpenText(pathToFile))
                {
                    builder.HtmlBody = SourceReader.ReadToEnd();
                }
                var htmlBody = builder.HtmlBody;
                var messageBody = string.Format(htmlBody, tripNum, link);


                var mimeMessage = new MimeMessage();
                mimeMessage.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.Sender));
                mimeMessage.To.Add(new MailboxAddress(email, email));
                mimeMessage.Subject = "Trip " + tripNum + " Summary";

                builder.TextBody = messageBody;
                builder.HtmlBody = messageBody;
                foreach (var attachment in attachments)
                {
                    builder.Attachments.Add(attachment.Key, attachment.Value);
                }
                mimeMessage.Body = builder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    if (_env.IsDevelopment())
                    {
                        // The third parameter is useSSL (true if the client should make an SSL-wrapped
                        // connection to the server; otherwise, false).
                        await client.ConnectAsync(_emailSettings.MailServer, _emailSettings.MailPort, SecureSocketOptions.StartTls);
                    }
                    else
                    {
                        await client.ConnectAsync(_emailSettings.MailServer);
                    }

                    // Note: only needed if the SMTP server requires authentication
                    await client.AuthenticateAsync(_emailSettings.Sender, _emailSettings.Password);

                    await client.SendAsync(mimeMessage);

                    await client.DisconnectAsync(true);
                }

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }

    }
}
