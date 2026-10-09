using System.Text;
using System.Text.RegularExpressions;
using Masroof.Application.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Masroof.Infrastructure.Sms;

/// <summary>
/// Reads SMS from the macOS Messages store (<c>chat.db</c>). iPhone text-message forwarding syncs
/// bank/wallet SMS to the Mac, so this is the on-device source for a zero-egress import. The file
/// is opened read-only and the process must have Full Disk Access.
/// </summary>
public sealed partial class MessagesSmsInbox(IOptions<SmsInboxOptions> options, ILogger<MessagesSmsInbox> logger)
    : ISmsInbox
{
    // Apple's Core Data epoch is 2001-01-01 UTC; message.date is nanoseconds since then.
    private static readonly DateTime AppleEpoch = new(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private const long NsPerSecond = 1_000_000_000L;

    private readonly SmsInboxOptions _options = options.Value;

    public async Task<IReadOnlyList<SmsMessage>> ReadAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        var path = ResolvePath();
        if (!File.Exists(path))
        {
            logger.LogWarning("Messages database not found at {Path}; no SMS ingested.", path);
            return [];
        }

        var fromNs = ToAppleNanos(fromUtc);
        var toNs = ToAppleNanos(toUtc);

        var results = new List<SmsMessage>();
        try
        {
            // Open via a file: URI with immutable=1 so we never take a lock on the live Messages
            // DB (it is often held WAL-open by the Messages app). immutable implies read-only.
            var uriPath = new UriBuilder { Scheme = "file", Host = string.Empty, Path = path }.Uri.AbsoluteUri;
            var cs = $"Data Source={uriPath}?immutable=1;Mode=ReadOnly;Cache=Private";

            await using var conn = new SqliteConnection(cs);
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText =
                """
                SELECT COALESCE(h.id, '') AS sender, m.text, m.attributedBody, m.date
                FROM message m
                JOIN handle h ON m.handle_id = h.ROWID
                WHERE m.is_from_me = 0
                  AND m.date >= $from AND m.date < $to
                ORDER BY m.date ASC
                """;
            cmd.Parameters.AddWithValue("$from", fromNs);
            cmd.Parameters.AddWithValue("$to", toNs);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var sender = reader.GetString(0);
                if (!MatchesProvider(sender))
                    continue;

                var body = reader.IsDBNull(1) ? null : reader.GetString(1);
                if (string.IsNullOrWhiteSpace(body) && !reader.IsDBNull(2))
                    body = ExtractFromAttributedBody((byte[])reader["attributedBody"]);
                if (string.IsNullOrWhiteSpace(body))
                    continue;

                var sentAt = FromAppleNanos(reader.GetInt64(3));
                results.Add(new SmsMessage(sender, body!.Trim(), sentAt));
            }
        }
        catch (SqliteException ex)
        {
            // The most common cause is a missing Full Disk Access grant (SQLITE_AUTH / cannot open).
            logger.LogError(ex,
                "Could not read the Messages database at {Path}. Grant Full Disk Access to the host process.", path);
            return [];
        }

        logger.LogInformation("Read {Count} provider SMS from {Path} for {From:u}..{To:u}.",
            results.Count, path, fromUtc, toUtc);
        return results;
    }

    private string ResolvePath()
    {
        if (!string.IsNullOrWhiteSpace(_options.DatabasePath))
            return Environment.ExpandEnvironmentVariables(_options.DatabasePath);
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, "Library", "Messages", "chat.db");
    }

    private bool MatchesProvider(string sender) =>
        !string.IsNullOrEmpty(sender)
        && _options.Providers.Any(p => sender.Contains(p, StringComparison.OrdinalIgnoreCase));

    private static long ToAppleNanos(DateTime utc) =>
        (long)((utc.ToUniversalTime() - AppleEpoch).TotalSeconds) * NsPerSecond;

    private static DateTime FromAppleNanos(long ns) =>
        AppleEpoch.AddSeconds(ns / (double)NsPerSecond);

    /// <summary>
    /// Best-effort recovery of the message text when <c>text</c> is NULL and the body lives only in
    /// the <c>attributedBody</c> NSAttributedString archive. Pulls the longest readable run; good
    /// enough to feed the parser, which normalizes aggressively anyway.
    /// </summary>
    private static string? ExtractFromAttributedBody(byte[] blob)
    {
        if (blob.Length == 0)
            return null;
        var decoded = Encoding.UTF8.GetString(blob);
        var matches = PrintableRun().Matches(decoded);
        string? best = null;
        foreach (Match m in matches)
        {
            var v = m.Value.Trim();
            if (best is null || v.Length > best.Length)
                best = v;
        }
        return best;
    }

    // Runs of printable characters (Latin, Arabic, digits, punctuation, symbols, whitespace),
    // length >= 4. Whitespace is included so a multi-line SMS body (the parts separated by
    // newlines in a bank notification) stays one contiguous run rather than fragmenting into
    // lines, which would lose the amount/merchant relationship.
    [GeneratedRegex(@"[\p{L}\p{N}\p{P}\p{Sc}\p{Sm} \t\r\n]{4,}")]
    private static partial Regex PrintableRun();
}
