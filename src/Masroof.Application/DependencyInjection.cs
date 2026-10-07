using FluentValidation;
using Masroof.Application.Transactions.CorrectCategory;
using Masroof.Application.Transactions.DeleteTransaction;
using Masroof.Application.Transactions.GetLedger;
using Masroof.Application.Transactions.ParseTransaction;
using Masroof.Application.Categories;
using Masroof.Application.Rules;
using Microsoft.Extensions.DependencyInjection;

namespace Masroof.Application;

public static class DependencyInjection
{
    /// <summary>Registers use-case handlers and their validators.</summary>
    public static IServiceCollection AddMasroofApplication(this IServiceCollection services)
    {
        services.AddScoped<ParseTransactionHandler>();
        services.AddScoped<PatchTransactionHandler>();
        services.AddScoped<GetLedgerHandler>();
        services.AddScoped<DeleteTransactionHandler>();
        services.AddScoped<ListRulesHandler>();
        services.AddScoped<DeleteRuleHandler>();
        services.AddScoped<GetCategoriesHandler>();

        services.AddScoped<IValidator<ParseTransactionCommand>, ParseTransactionCommandValidator>();
        services.AddScoped<IValidator<PatchTransactionCommand>, PatchTransactionCommandValidator>();

        return services;
    }
}
