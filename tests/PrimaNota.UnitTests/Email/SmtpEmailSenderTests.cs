using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PrimaNota.Infrastructure.Email;

namespace PrimaNota.UnitTests.Email;

public sealed class SmtpEmailSenderTests
{
    private static SmtpEmailSender Build(SmtpOptions options) =>
        new(Options.Create(options), NullLogger<SmtpEmailSender>.Instance);

    [Fact]
    public void IsConfigured_False_When_Disabled()
    {
        var sender = Build(new SmtpOptions { Enabled = false, Host = "smtp.test", FromAddress = "a@b.it" });
        sender.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void IsConfigured_False_When_Host_Or_From_Missing()
    {
        Build(new SmtpOptions { Enabled = true, Host = "", FromAddress = "a@b.it" }).IsConfigured.Should().BeFalse();
        Build(new SmtpOptions { Enabled = true, Host = "smtp.test", FromAddress = "" }).IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void IsConfigured_True_When_Enabled_Host_And_From_Present()
    {
        var sender = Build(new SmtpOptions { Enabled = true, Host = "smtp.test", FromAddress = "a@b.it" });
        sender.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_Throws_When_Not_Configured()
    {
        var sender = Build(new SmtpOptions { Enabled = false });

        var act = async () => await sender.SendAsync("to@b.it", "subj", "<p>body</p>");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
