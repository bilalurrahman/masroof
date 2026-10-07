using FluentValidation;

namespace Masroof.Application.Transactions.ParseTransaction;

/// <summary>Parse one message into a saved ledger row.</summary>
public sealed record ParseTransactionCommand(string Text);

public sealed class ParseTransactionCommandValidator : AbstractValidator<ParseTransactionCommand>
{
    public ParseTransactionCommandValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Text is required.")
            .MaximumLength(1000).WithMessage("Text must be 1000 characters or fewer.");
    }
}
