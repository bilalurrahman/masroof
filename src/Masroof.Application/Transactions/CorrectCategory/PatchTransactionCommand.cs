using FluentValidation;
using Masroof.Domain.Taxonomy;

namespace Masroof.Application.Transactions.CorrectCategory;

/// <summary>
/// Edit one transaction. A category change drives the learn loop (rule upsert + embed job);
/// every changed field is logged to Feedback. All fields are optional — only supplied ones change.
/// </summary>
public sealed record PatchTransactionCommand(
    long Id,
    string? CategoryCode = null,
    decimal? Amount = null,
    DateOnly? TxnDate = null,
    string? Counterparty = null,
    string? Direction = null);

public sealed class PatchTransactionCommandValidator : AbstractValidator<PatchTransactionCommand>
{
    public PatchTransactionCommandValidator()
    {
        RuleFor(x => x.CategoryCode)
            .Must(CategoryCodes.IsValid)
            .When(x => x.CategoryCode is not null)
            .WithMessage("Unknown category code.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).When(x => x.Amount is not null)
            .WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Direction)
            .Must(d => d is "debit" or "credit")
            .When(x => x.Direction is not null)
            .WithMessage("Direction must be 'debit' or 'credit'.");

        RuleFor(x => x)
            .Must(x => x.CategoryCode is not null || x.Amount is not null || x.TxnDate is not null
                       || x.Counterparty is not null || x.Direction is not null)
            .WithMessage("At least one field must be provided.");
    }
}
