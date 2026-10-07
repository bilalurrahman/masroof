using System.ComponentModel;
using System.Globalization;
using Masroof.Application.Abstractions;
using Masroof.Application.Ask;
using Masroof.Application.Common;
using Microsoft.Extensions.AI;

namespace Masroof.Infrastructure.Ask;

/// <summary>
/// Answers questions by giving the model a catalog of safe, user-scoped tools (never raw data
/// or SQL). Every tool resolves the UserId from <see cref="ICurrentUser"/>, so the LLM cannot
/// widen scope. The tools it actually invoked are returned for auditability.
/// </summary>
public sealed class AskService(
    IChatClient chat,
    IReportQueries reports,
    ICurrentUser currentUser,
    IClock clock)
    : IAskService
{
    public async Task<AskResponse> AskAsync(AskRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var invocations = new List<ToolInvocation>();

        void Record(string tool, string args, int count) => invocations.Add(new ToolInvocation(tool, args, count));

        [Description("Total spend per category for a month (format YYYY-MM).")]
        async Task<object> GetSpendByCategory(string month, CancellationToken token)
        {
            var key = MonthKey.Parse(month);
            var res = await reports.GetSpendByCategoryAsync(userId, key.Year, key.Month, token);
            Record("get_spend_by_category", $"month={month}", res.Count);
            return res;
        }

        [Description("Summary for a month (format YYYY-MM): total debit, total credit, count, and prior month's debit.")]
        async Task<object> GetMonthlySummary(string month, CancellationToken token)
        {
            var key = MonthKey.Parse(month);
            var res = await reports.GetMonthlySummaryAsync(userId, key.Year, key.Month, token);
            Record("get_monthly_summary", $"month={month}", res.ByCategory.Count);
            return res;
        }

        [Description("Top N merchants by spend between two dates (dates in YYYY-MM-DD).")]
        async Task<object> GetTopMerchants(string from, string to, int n, CancellationToken token)
        {
            var fromDate = ParseDate(from);
            var toDate = ParseDate(to);
            var count = n <= 0 ? 5 : Math.Min(n, 50);
            var res = await reports.GetTopMerchantsAsync(userId, fromDate, toDate, count, token);
            Record("get_top_merchants", $"from={from};to={to};n={count}", res.Count);
            return res;
        }

        [Description("Compare total spend of two months (each YYYY-MM), overall and per category.")]
        async Task<object> CompareMonths(string monthA, string monthB, CancellationToken token)
        {
            var a = MonthKey.Parse(monthA);
            var b = MonthKey.Parse(monthB);
            var res = await reports.CompareMonthsAsync(userId, a.Year, a.Month, b.Year, b.Month, token);
            Record("compare_months", $"a={monthA};b={monthB}", res.ByCategory.Count);
            return res;
        }

        [Description("Search transactions by merchant text, with an optional month (YYYY-MM).")]
        async Task<object> FindTransactions(string query, string? month, CancellationToken token)
        {
            int? year = null, mon = null;
            if (MonthKey.TryParse(month, out var key)) { year = key.Year; mon = key.Month; }
            var res = await reports.FindTransactionsAsync(userId, query, year, mon, 25, token);
            Record("find_transactions", $"query={query};month={month ?? "any"}", res.Count);
            return res;
        }

        var tools = new List<AITool>
        {
            AIFunctionFactory.Create(GetSpendByCategory, "get_spend_by_category"),
            AIFunctionFactory.Create(GetMonthlySummary, "get_monthly_summary"),
            AIFunctionFactory.Create(GetTopMerchants, "get_top_merchants"),
            AIFunctionFactory.Create(CompareMonths, "compare_months"),
            AIFunctionFactory.Create(FindTransactions, "find_transactions"),
        };

        var isArabic = currentUser.Locale.StartsWith("ar", StringComparison.OrdinalIgnoreCase);
        var system =
            $"""
             You are a personal finance assistant. Answer questions about the user's own spending
             using ONLY the provided tools — never invent numbers. Today is {clock.Today:yyyy-MM-dd}.
             The user's currency is {currentUser.Currency}. If a question needs data you cannot get
             from the tools, say so plainly. Keep answers concise and {(isArabic ? "write the final answer in Arabic" : "write the final answer in English")}.
             """;

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, system),
            new(ChatRole.User, request.Question)
        };

        var options = new ChatOptions { Tools = tools, Temperature = 0f };
        var response = await chat.GetResponseAsync(messages, options, ct);

        return new AskResponse(response.Text ?? string.Empty, invocations);
    }

    private static DateOnly ParseDate(string value) =>
        DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d
            : throw new FormatException($"Invalid date '{value}'. Expected YYYY-MM-DD.");
}
