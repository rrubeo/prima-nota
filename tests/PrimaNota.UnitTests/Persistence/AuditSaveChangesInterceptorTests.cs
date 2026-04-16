using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PrimaNota.Application.Abstractions;
using PrimaNota.Domain.Abstractions;
using PrimaNota.Infrastructure.Persistence;
using PrimaNota.Shared.Clock;

namespace PrimaNota.UnitTests.Persistence;

public sealed class AuditSaveChangesInterceptorTests
{
    [Fact]
    public async Task Adding_Entity_Should_Populate_CreatedAt_And_CreatedBy()
    {
        var fakeNow = new DateTimeOffset(2026, 4, 16, 10, 0, 0, TimeSpan.Zero);
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(fakeNow);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns("user-123");

        var interceptor = new AuditSaveChangesInterceptor(currentUser, clock);

        var options = new DbContextOptionsBuilder<AuditTestContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        await using var ctx = new AuditTestContext(options);

        var entity = new FakeAuditable();
        ctx.Fakes.Add(entity);
        await ctx.SaveChangesAsync(CancellationToken.None);

        entity.CreatedAt.Should().Be(fakeNow);
        entity.CreatedBy.Should().Be("user-123");
        entity.UpdatedAt.Should().BeNull();
        entity.UpdatedBy.Should().BeNull();
    }

    [Fact]
    public async Task Modifying_Entity_Should_Populate_UpdatedAt_And_UpdatedBy()
    {
        var creationTime = new DateTimeOffset(2026, 4, 16, 10, 0, 0, TimeSpan.Zero);
        var updateTime = creationTime.AddHours(3);
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(creationTime, updateTime);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns("creator", "editor");

        var interceptor = new AuditSaveChangesInterceptor(currentUser, clock);

        var options = new DbContextOptionsBuilder<AuditTestContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        await using var ctx = new AuditTestContext(options);

        var entity = new FakeAuditable();
        ctx.Fakes.Add(entity);
        await ctx.SaveChangesAsync(CancellationToken.None);

        entity.Label = "changed";
        await ctx.SaveChangesAsync(CancellationToken.None);

        entity.CreatedAt.Should().Be(creationTime);
        entity.CreatedBy.Should().Be("creator");
        entity.UpdatedAt.Should().Be(updateTime);
        entity.UpdatedBy.Should().Be("editor");
    }

    [Fact]
    public async Task Unauthenticated_User_Should_Fall_Back_To_System_Identifier()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns((string?)null);

        var interceptor = new AuditSaveChangesInterceptor(currentUser, clock);

        var options = new DbContextOptionsBuilder<AuditTestContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        await using var ctx = new AuditTestContext(options);

        var entity = new FakeAuditable();
        ctx.Fakes.Add(entity);
        await ctx.SaveChangesAsync(CancellationToken.None);

        entity.CreatedBy.Should().Be("system");
    }

    private sealed class FakeAuditable : AuditableEntity<Guid>
    {
        public FakeAuditable()
        {
            Id = Guid.NewGuid();
        }

        public string Label { get; set; } = "initial";
    }

    private sealed class AuditTestContext : DbContext
    {
        public AuditTestContext(DbContextOptions<AuditTestContext> options)
            : base(options)
        {
        }

        public DbSet<FakeAuditable> Fakes => Set<FakeAuditable>();
    }
}
