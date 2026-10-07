using System.Text.Json.Nodes;
using Masroof.Domain.Taxonomy;

namespace Masroof.Infrastructure.Llm;

/// <summary>
/// The JSON Schema handed to Ollama's <c>format</c> parameter so the model cannot emit
/// invalid structure. <c>category</c> and <c>direction</c> are enums, pinning them to the
/// taxonomy and the allowed directions; business rules are still re-validated in .NET.
/// </summary>
public static class TransactionSchema
{
    public static JsonObject Build()
    {
        var categoryEnum = new JsonArray();
        foreach (var code in CategoryCodes.All)
            categoryEnum.Add(code);

        return new JsonObject
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["properties"] = new JsonObject
            {
                ["direction"] = new JsonObject
                {
                    ["type"] = "string",
                    ["enum"] = new JsonArray { "debit", "credit" }
                },
                ["amount"] = new JsonObject { ["type"] = "number" },
                ["currency"] = new JsonObject { ["type"] = "string" },
                ["counterparty"] = new JsonObject { ["type"] = new JsonArray { "string", "null" } },
                ["channel"] = new JsonObject { ["type"] = new JsonArray { "string", "null" } },
                ["accountLast4"] = new JsonObject { ["type"] = new JsonArray { "string", "null" } },
                ["date"] = new JsonObject { ["type"] = new JsonArray { "string", "null" } },
                ["category"] = new JsonObject
                {
                    ["type"] = "string",
                    ["enum"] = categoryEnum
                },
                ["confidence"] = new JsonObject
                {
                    ["type"] = "number",
                    ["minimum"] = 0,
                    ["maximum"] = 1
                }
            },
            ["required"] = new JsonArray { "direction", "amount", "currency", "category", "confidence" }
        };
    }
}
