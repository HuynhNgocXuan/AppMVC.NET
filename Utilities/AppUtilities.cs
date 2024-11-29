using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace webMVC.Utilities
{
  public static class AppUtilities
  {
    public static bool IsValidEmail(string email)
    {
      if (string.IsNullOrWhiteSpace(email))
        return false;

      try
      {
        email = NormalizeDomain(email);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Lỗi chuẩn hóa domain: {ex.Message}");
        return false;
      }

      return ValidateEmailFormat(email);
    }

    private static string NormalizeDomain(string email)
    {
      return Regex.Replace(email, @"(@)(.+)$", match =>
      {
        var idn = new IdnMapping();

        string domainName = idn.GetAscii(match.Groups[2].Value);
        return match.Groups[1].Value + domainName;
      }, RegexOptions.None, TimeSpan.FromMilliseconds(200));
    }

    private static bool ValidateEmailFormat(string email)
    {
      const string emailRegexPattern =
               @"^(?("")(""[^""]+?""@)|(([0-9a-zA-Z]((\.(?!\.))|[-!#$%&'*+/=?^_`{|}~])*[0-9a-zA-Z])@))" +
               @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-zA-Z][-0-9a-zA-Z]*[0-9a-zA-Z]*\.)+[a-zA-Z]{2,}))$";

      try
      {
        return Regex.IsMatch(email, emailRegexPattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
      }

      catch (RegexMatchTimeoutException ex)
      {
        Console.WriteLine($"Lỗi timeout trong kiểm tra Regex: {ex.Message}");
        return false;
      }
    }
  }
}
