using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace AmazonSyncADUsers
{
    class SendMail
    {
        private static string MAILFrom = ConfigurationManager.AppSettings["MAILFrom"];
        private static string MAILServer = ConfigurationManager.AppSettings["MAILServer"];
        private static string MAILPort = ConfigurationManager.AppSettings["MAILPort"];

        public static void EmailTo(string to, string subject, string message,string attachment)
        {
            try
            {
                var mailMessage = new MailMessage(MAILFrom, to);
                mailMessage.Subject = subject;
                mailMessage.Body = message;
                
                if (!string.IsNullOrEmpty(attachment))
                    mailMessage.Attachments.Add(new Attachment(attachment));

                var smtpClient = new SmtpClient(MAILServer, int.Parse(MAILPort));
                smtpClient.UseDefaultCredentials = true;
                smtpClient.Send(mailMessage);
            }
            catch (Exception ex)
            {
                //TODO: Implement log
                throw;
            }
        }
    }
}
