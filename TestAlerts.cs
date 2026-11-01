using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json.Nodes;

namespace MediantTrace;

/// <summary>
/// Tells someone when a scheduled test starts failing, and when it passes again.
/// A Teams incoming webhook (or any address that accepts a JSON post) and email.
/// A test run by hand does not send anything, and a test that keeps failing is not sent again.
/// </summary>
public static class TestAlerts
{
    public static string? Kind(string trigger, string? previous, string outcome)
    {
        if (!string.Equals(trigger, "scheduled", StringComparison.Ordinal)) return null;
        bool failed = outcome == "failed";
        bool wasFailed = previous == "failed";
        if (failed && !wasFailed) return "failed";
        if (outcome == "passed" && wasFailed) return "recovered";
        return null;
    }

    public static string Title(TestResult result, string kind) =>
        kind == "recovered"
            ? $"MediantTrace: {result.Name} passed again"
            : $"MediantTrace: {result.Name} failed";

    public static string Text(TestResult result, string kind)
    {
        string finding = result.Findings.FirstOrDefault() ?? "";
        var lines = new List<string> { Title(result, kind), result.Target };
        if (finding.Length > 0) lines.Add(finding);
        if (result.FinalStatus > 0) lines.Add($"SIP {result.FinalStatus} {result.FinalReason}".Trim());
        if (result.MosRx is double mos) lines.Add($"MOS {mos:0.00}");
        lines.Add(result.Started.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
        return string.Join("\n", lines);
    }

    /// <summary>Teams MessageCard plus a text field, so a Slack-style webhook shows the same words.</summary>
    public static string Payload(TestResult result, string kind)
    {
        string title = Title(result, kind);
        var card = new JsonObject
        {
            ["text"] = Text(result, kind),
            ["summary"] = title,
            ["title"] = title,
            ["@type"] = "MessageCard",
            ["@context"] = "https://schema.org/extensions",
            ["themeColor"] = kind == "recovered" ? "15803D" : "B91C1C",
        };
        return card.ToJsonString();
    }

    public static bool WebhookOk(string url) =>
        url.Length is > 0 and <= 2000
        && Uri.TryCreate(url, UriKind.Absolute, out Uri? u)
        && u.Scheme == "https"
        && string.IsNullOrEmpty(u.UserInfo);

    public static bool EmailOk(string address) =>
        address.Length is > 0 and <= 200 && address.Contains('@') && !address.Contains(' ') && !address.Contains(',');

    public static async Task Send(Settings settings, TestResult result, string kind)
    {
        string payload = Payload(result, kind);
        if (settings.AlertWebhook.Length > 0)
        {
            try { await Post(settings.AlertWebhook, payload); Log.Write($"Alert sent for {result.Name} ({kind})"); }
            catch (Exception e) { Log.Write($"Alert webhook failed for {result.Name}: {e.Message}"); }
        }
        if (settings.AlertEmailTo.Length > 0 && settings.AlertSmtpHost.Length > 0)
        {
            try { PostMail(settings, Title(result, kind), Text(result, kind)); Log.Write($"Alert email sent for {result.Name} ({kind})"); }
            catch (Exception e) { Log.Write($"Alert email failed for {result.Name}: {e.Message}"); }
        }
    }

    /// <summary>Sends a message (title and text) to the webhook and email set in Settings.</summary>
    public static async Task SendText(Settings settings, string title, string text, bool good)
    {
        var card = new JsonObject
        {
            ["text"] = text, ["summary"] = title, ["title"] = title,
            ["@type"] = "MessageCard", ["@context"] = "https://schema.org/extensions",
            ["themeColor"] = good ? "15803D" : "B91C1C",
        };
        if (settings.AlertWebhook.Length > 0)
        {
            try { await Post(settings.AlertWebhook, card.ToJsonString()); Log.Write($"Alert sent: {title}"); }
            catch (Exception e) { Log.Write($"Alert webhook failed ({title}): {e.Message}"); }
        }
        if (settings.AlertEmailTo.Length > 0 && settings.AlertSmtpHost.Length > 0)
        {
            try { PostMail(settings, title, text); Log.Write($"Alert email sent: {title}"); }
            catch (Exception e) { Log.Write($"Alert email failed ({title}): {e.Message}"); }
        }
    }

    private static async Task Post(string url, string json)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        using var body = new StringContent(json, Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await client.PostAsync(url, body);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"the address answered {(int)response.StatusCode}");
    }

    private static void PostMail(Settings settings, string subject, string body)
    {
        using var message = new MailMessage(settings.AlertEmailFrom, settings.AlertEmailTo, subject, body);
        using var smtp = new SmtpClient(settings.AlertSmtpHost, settings.AlertSmtpPort)
        {
            EnableSsl = settings.AlertSmtpPort != 25,
            DeliveryMethod = SmtpDeliveryMethod.Network,
        };
        if (settings.AlertSmtpUser.Length > 0)
            smtp.Credentials = new NetworkCredential(settings.AlertSmtpUser, settings.AlertSmtpPassword);
        smtp.Send(message);
    }
}
