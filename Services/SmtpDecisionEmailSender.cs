using System.Net;
using System.Net.Mail;
using System.Text;
using LeaveManagement.Models;
using Microsoft.Extensions.Options;

namespace LeaveManagement.Services;

public interface IDecisionEmailSender
{
    Task<bool> SendDecisionNoticeAsync(LeaveRequest request, ApplicationUser manager);
}

public sealed class SmtpDecisionEmailSender(
    IOptions<EmailSettings> emailOptions,
    ILogger<SmtpDecisionEmailSender> logger) : IDecisionEmailSender
{
    public async Task<bool> SendDecisionNoticeAsync(LeaveRequest request, ApplicationUser manager)
    {
        var settings = emailOptions.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("Decision email is disabled; no message sent for leave request {RequestId}.", request.Id);
            return false;
        }

        if (!MailAddress.TryCreate(settings.GmailAddress, out _) ||
            string.IsNullOrWhiteSpace(settings.AppPassword) ||
            !MailAddress.TryCreate(request.Email, out _))
        {
            logger.LogWarning("Gmail email settings are incomplete; no message sent for leave request {RequestId}.", request.Id);
            return false;
        }

        var outcome = request.Status == LeaveStatus.Approved ? "approved" : request.Status == LeaveStatus.NeedsMoreInfo ? "needs more information" : "rejected";
        var decisionTime = request.DecisionAt?.ToLocalTime().ToString("f") ?? "just now";
        var body = string.Join(
            Environment.NewLine + Environment.NewLine,
            $"Hello {request.EmployeeName},",
            $"Your {request.LeaveType} request for {request.StartDate:MMM d, yyyy} to {request.EndDate:MMM d, yyyy} ({request.Days} {(request.Days == 1 ? "day" : "days")}) was {outcome}.",
            request.Status == LeaveStatus.Rejected ? $"Reason: {request.RejectionReason}" : request.Status == LeaveStatus.NeedsMoreInfo ? $"Please reply in DayOff: {request.MoreInfoRequest}" : "",
            $"Decision made by: {manager.DisplayName} ({manager.Email})",
            $"Decision time: {decisionTime}",
            "DayOff Leave Management");

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(settings.GmailAddress),
                Subject = $"Your leave request was {outcome}",
                Body = body,
                IsBodyHtml = false,
                SubjectEncoding = Encoding.UTF8,
                BodyEncoding = Encoding.UTF8
            };
            message.To.Add(request.Email);

            using var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(settings.GmailAddress, settings.AppPassword)
            };

            await client.SendMailAsync(message);
            logger.LogInformation("Gmail decision email sent for leave request {RequestId} to {Recipient}.", request.Id, request.Email);
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not send decision email for leave request {RequestId} through Gmail SMTP.", request.Id);
            return false;
        }
    }
}
