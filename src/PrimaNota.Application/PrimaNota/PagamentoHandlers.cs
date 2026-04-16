using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PrimaNota.Application.Abstractions;
using PrimaNota.Domain.PrimaNota;

namespace PrimaNota.Application.PrimaNota;

/// <summary>Registers a partial / full payment against a movement.</summary>
/// <param name="MovimentoId">Movement id.</param>
/// <param name="Data">Value date.</param>
/// <param name="Importo">Positive settlement amount.</param>
/// <param name="ContoFinanziarioId">Financial account involved in the cash flow.</param>
/// <param name="Note">Optional note.</param>
public sealed record AddPagamentoMovimento(
    Guid MovimentoId,
    DateOnly Data,
    decimal Importo,
    Guid ContoFinanziarioId,
    string? Note) : IRequest<Guid>;

/// <summary>Removes a payment from a movement.</summary>
/// <param name="MovimentoId">Movement id.</param>
/// <param name="PagamentoId">Payment id.</param>
public sealed record RemovePagamentoMovimento(Guid MovimentoId, Guid PagamentoId) : IRequest;

/// <summary>Validator for <see cref="AddPagamentoMovimento"/>.</summary>
public sealed class AddPagamentoMovimentoValidator : AbstractValidator<AddPagamentoMovimento>
{
    /// <summary>Initializes a new instance of the <see cref="AddPagamentoMovimentoValidator"/> class.</summary>
    public AddPagamentoMovimentoValidator()
    {
        RuleFor(x => x.MovimentoId).NotEmpty();
        RuleFor(x => x.ContoFinanziarioId).NotEmpty();
        RuleFor(x => x.Importo).GreaterThan(0m);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

/// <summary>Handler for <see cref="AddPagamentoMovimento"/>.</summary>
public sealed class AddPagamentoMovimentoHandler : IRequestHandler<AddPagamentoMovimento, Guid>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="AddPagamentoMovimentoHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public AddPagamentoMovimentoHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task<Guid> Handle(AddPagamentoMovimento request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var movimento = await db.Movimenti
            .Include(m => m.Pagamenti)
            .Include(m => m.Righe)
            .FirstOrDefaultAsync(m => m.Id == request.MovimentoId, cancellationToken)
            ?? throw new KeyNotFoundException($"Movimento {request.MovimentoId} non trovato.");

        var pagamento = new PagamentoMovimento(request.Data, request.Importo, request.ContoFinanziarioId, request.Note);
        movimento.AddPagamento(pagamento);

        await db.SaveChangesAsync(cancellationToken);
        return pagamento.Id;
    }
}

/// <summary>Handler for <see cref="RemovePagamentoMovimento"/>.</summary>
public sealed class RemovePagamentoMovimentoHandler : IRequestHandler<RemovePagamentoMovimento>
{
    private readonly IApplicationDbContext db;

    /// <summary>Initializes a new instance of the <see cref="RemovePagamentoMovimentoHandler"/> class.</summary>
    /// <param name="db">DB.</param>
    public RemovePagamentoMovimentoHandler(IApplicationDbContext db) => this.db = db;

    /// <inheritdoc />
    public async Task Handle(RemovePagamentoMovimento request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var movimento = await db.Movimenti
            .Include(m => m.Pagamenti)
            .FirstOrDefaultAsync(m => m.Id == request.MovimentoId, cancellationToken)
            ?? throw new KeyNotFoundException($"Movimento {request.MovimentoId} non trovato.");

        var removed = movimento.RemovePagamento(request.PagamentoId);
        if (removed is null)
        {
            return;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
