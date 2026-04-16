using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PrimaNota.Application.Abstractions;
using PrimaNota.Application.Anagrafiche.Upsert;

namespace PrimaNota.Application.Anagrafiche;

/// <summary>Updates an existing anagrafica.</summary>
/// <param name="Id">Identifier of the anagrafica to update.</param>
/// <param name="Input">New values.</param>
public sealed record UpdateAnagrafica(Guid Id, AnagraficaInput Input) : IRequest;

/// <summary>Validator for <see cref="UpdateAnagrafica"/>.</summary>
public sealed class UpdateAnagraficaValidator : AbstractValidator<UpdateAnagrafica>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateAnagraficaValidator"/> class.</summary>
    public UpdateAnagraficaValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Input).NotNull().SetValidator(new AnagraficaInputValidator());
    }
}

/// <summary>Handles <see cref="UpdateAnagrafica"/>.</summary>
public sealed class UpdateAnagraficaHandler : IRequestHandler<UpdateAnagrafica>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="UpdateAnagraficaHandler"/> class.</summary>
    /// <param name="db">Application DB context.</param>
    public UpdateAnagraficaHandler(IApplicationDbContext db)
    {
        this.db = db;
    }

    /// <inheritdoc />
    public async Task Handle(UpdateAnagrafica request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var anagrafica = await db.Anagrafiche.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Anagrafica {request.Id} non trovata.");

        anagrafica.ApplyInput(request.Input);
        await db.SaveChangesAsync(cancellationToken);
    }
}
