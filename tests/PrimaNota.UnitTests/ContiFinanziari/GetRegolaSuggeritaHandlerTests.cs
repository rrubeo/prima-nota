using Microsoft.EntityFrameworkCore;
using PrimaNota.Application.ContiFinanziari;
using PrimaNota.Domain.ContiFinanziari;
using PrimaNota.Infrastructure.Persistence;

namespace PrimaNota.UnitTests.ContiFinanziari;

public sealed class GetRegolaSuggeritaHandlerTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static (EstratoContoImport Import, RigaEstrattoConto Riga) SeedImport(
        AppDbContext ctx,
        Guid conto,
        string? causaleOp,
        string? operazione,
        string? descrizione)
    {
        var import = new EstratoContoImport(conto, "x.csv", new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), null);
        var riga = new RigaEstrattoConto(
            new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 2), causaleOp, operazione, descrizione, -326.90m);
        import.AddRiga(riga);
        ctx.EstrattiConto.Add(import);
        return (import, riga);
    }

    [Fact]
    public async Task Returns_Suggestion_For_Exact_Signature_Match()
    {
        var conto = Guid.NewGuid();
        var causale = Guid.NewGuid();
        var categoria = Guid.NewGuid();
        var anagrafica = Guid.NewGuid();

        await using var ctx = NewContext();
        var (import, riga) = SeedImport(ctx, conto, "151F", "RATA FINANZIAMENTO", "ARVAL SERVICE LEASE CID.IT1 MAN.A");

        var sig = RegolaSignature.Compute("151F", "RATA FINANZIAMENTO", "ARVAL SERVICE LEASE CID.IT1 MAN.A");
        ctx.RegoleRiconciliazione.Add(new RegolaRiconciliazione(conto, sig, causale, categoria, anagrafica, null, null));
        await ctx.SaveChangesAsync();

        var result = await new GetRegolaSuggeritaHandler(ctx)
            .Handle(new GetRegolaSuggerita(conto, import.Id, riga.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.CausaleId.Should().Be(causale);
        result.CategoriaId.Should().Be(categoria);
        result.AnagraficaId.Should().Be(anagrafica);
        result.UtilizziCount.Should().Be(1);
    }

    [Fact]
    public async Task Returns_Null_When_No_Rule_Matches()
    {
        var conto = Guid.NewGuid();

        await using var ctx = NewContext();
        var (import, riga) = SeedImport(ctx, conto, "151F", "RATA FINANZIAMENTO", "ARVAL SERVICE LEASE");
        await ctx.SaveChangesAsync();

        var result = await new GetRegolaSuggeritaHandler(ctx)
            .Handle(new GetRegolaSuggerita(conto, import.Id, riga.Id), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Falls_Back_To_Generic_Rule_When_No_Description_Match()
    {
        var conto = Guid.NewGuid();
        var causale = Guid.NewGuid();
        var categoria = Guid.NewGuid();

        await using var ctx = NewContext();
        // Row has a non-empty description fragment, but only a generic (empty-description)
        // cause+operation rule exists -> it must be used as the fallback.
        var (import, riga) = SeedImport(ctx, conto, "26", "BONIFICO SEPA", "Da PINCO PALLINO per saldo");

        var genericSig = RegolaSignature.Compute("26", "BONIFICO SEPA", null);
        genericSig.DescrizioneChiave.Should().BeEmpty();
        ctx.RegoleRiconciliazione.Add(new RegolaRiconciliazione(conto, genericSig, causale, categoria, null, null, null));
        await ctx.SaveChangesAsync();

        var result = await new GetRegolaSuggeritaHandler(ctx)
            .Handle(new GetRegolaSuggerita(conto, import.Id, riga.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.CausaleId.Should().Be(causale);
    }

    [Fact]
    public async Task Does_Not_Match_Rule_From_Different_Account()
    {
        var conto = Guid.NewGuid();
        var altroConto = Guid.NewGuid();

        await using var ctx = NewContext();
        var (import, riga) = SeedImport(ctx, conto, "151F", "RATA FINANZIAMENTO", "ARVAL SERVICE LEASE");

        var sig = RegolaSignature.Compute("151F", "RATA FINANZIAMENTO", "ARVAL SERVICE LEASE");
        ctx.RegoleRiconciliazione.Add(new RegolaRiconciliazione(altroConto, sig, Guid.NewGuid(), Guid.NewGuid(), null, null, null));
        await ctx.SaveChangesAsync();

        var result = await new GetRegolaSuggeritaHandler(ctx)
            .Handle(new GetRegolaSuggerita(conto, import.Id, riga.Id), CancellationToken.None);

        result.Should().BeNull();
    }
}
