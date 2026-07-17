using System.Net.Mail;

namespace Atmos.Common.Utils;

public static class EmailUtils
{
    public static string Normalize(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    public static bool IsValid(string email)
    {
        return MailAddress.TryCreate(email, out _);
    }
}
