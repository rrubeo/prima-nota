using FluentValidation;
using MediatR;
using PrimaNota.Application.Abstractions;
using PrimaNota.Application.Anagrafiche.Upsert;
using PrimaNota.Domain.Anagrafiche;

namespace PrimaNota.Application.Anagrafiche;

/// <summary>Creates a new <see cref="Anagrafica"/>.</summary>
/// <param name="Input">Input payload.</param>
public sealed record CreateAnagrafica(AnagraficaInput Input) : IRequest<Guid>;

/// <summary>Validator for <see cref="CreateAnagrafica"/>.</summary>
public sealed class CreateAnagraficaValidator : AbstractValidator<CreateAnagrafica>
{
    /// <summary>Initializes a new instance of the <see cref="CreateAnagraficaValidator"/> class.</summary>
    public CreateAnagraficaValidator()
    {
        RuleFor(x => x.Input).NotNull().SetValidator(new AnagraficaInputValidator());
    }
}

/// <summary>Handles <see cref="CreateAnagrafica"/>.</summary>
public sealed class CreateAnagraficaHandler : IRequestHandler<CreateAnagrafica, Guid>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="CreateAnagraficaHandler"/> class.</summary>
    /// <param name="db">Application DB context.</param>
    public CreateAnagraficaHandler(IApplicationDbContext db)
    {
        this.db = db;
    }

    /// <inheritdoc />
    public async Task<Guid> Handle(CreateAnagrafica request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var anagrafica = new Anagrafica(request.Input.RagioneSociale, request.Input.PersonaFisica);
        anagrafica.ApplyInput(request.Input);

        db.Anagrafiche.Add(anagrafica);
        await db.SaveChangesAsync(cancellationToken);

        return anagrafica.Id;
    }
}
