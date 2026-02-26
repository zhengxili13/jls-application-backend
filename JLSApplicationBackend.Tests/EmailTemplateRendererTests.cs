using System;
using System.IO;
using System.Threading.Tasks;
using JLSApplicationBackend.Services;
using JLSApplicationBackend.Services.EmailTemplateModel;
using Moq;
using NUnit.Framework;

namespace JLSApplicationBackend.Tests;




// =====================================================================
// PART 2 — ScribanEmailTemplateRenderer (Phase 2, after migration)
//
// Tests the actual rendered HTML output using real .html template files.
// Each test: given input parameters → assert output HTML contains the
// expected text fragments from the corresponding Scriban template.
//
// These tests will FAIL until ScribanEmailTemplateRenderer is implemented
// and the EmailTemplates/*.html files are created.
// =====================================================================
[TestFixture]
public class ScribanEmailTemplateRendererTests
{
    private const string Username    = "client@example.com";
    private const string OrderNumber = "99887";
    private const string Link        = "https://example.com/reset?token=xyz";
    private const string Entreprise  = "ACME Corp";
    private const string Phone       = "0612345678";
    private const string ClientEmail = "visitor@example.com";
    private const string Message     = "Bonjour, j'ai une question sur ma commande.";

    private ScribanEmailTemplateRenderer _renderer;
    private string _snapshotsDir;

    [SetUp]
    public void SetUp()
    {
        var templateDir = Path.Combine(AppContext.BaseDirectory, "EmailTemplates");
        _renderer = new ScribanEmailTemplateRenderer(templateDir);

        var solutionDir = Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.Parent!.FullName;
        _snapshotsDir = Path.Combine(solutionDir, "JLSApplicationBackend.Tests", "TestData", "Snapshots");
    }

    private async Task VerifySnapshotAsync(string snapshotFilename, EmailTemplateContext context)
    {
        var actualHtml = await _renderer.RenderAsync(context);
        actualHtml = actualHtml.Replace("\r\n", "\n").Trim();

        var snapshotPath = Path.Combine(_snapshotsDir, snapshotFilename);
        var expectedHtml = await File.ReadAllTextAsync(snapshotPath);
        expectedHtml = expectedHtml.Replace("\r\n", "\n").Trim();

        // Compare actual to expected, ignoring arbitrary formatting differences
        // Scriban might format whitespace slightly differently than MVC Razor.
        // We strip all whitespace strictly for comparison to ensure purely semantic match.
        var strippedActual = StripWhitespace(actualHtml);
        var strippedExpected = StripWhitespace(expectedHtml);

        Assert.That(strippedActual, Is.EqualTo(strippedExpected), 
            $"Template {snapshotFilename} did not match the Razor snapshot.");
    }

    private static string StripWhitespace(string input)
    {
        var decoded = System.Net.WebUtility.HtmlDecode(input);
        return System.Text.RegularExpressions.Regex.Replace(decoded, @"\s+", "");
    }

    [Test]
    public Task NewOrderClient_MatchesSnapshot() => 
        VerifySnapshotAsync("new-order-client.html", new NewOrderClientContext(Username, OrderNumber));

    [Test]
    public Task NewOrderAdmin_MatchesSnapshot() => 
        VerifySnapshotAsync("new-order-admin.html", new NewOrderAdminContext(Username, OrderNumber));

    [Test]
    public Task ModifyOrderClient_MatchesSnapshot() => 
        VerifySnapshotAsync("modify-order-client.html", new ModifyOrderClientContext(Username, OrderNumber));

    [Test]
    public Task ModifyOrderAdmin_MatchesSnapshot() => 
        VerifySnapshotAsync("modify-order-admin.html", new ModifyOrderAdminContext(Username, OrderNumber));

    [Test]
    public Task EmailConfirmation_MatchesSnapshot() => 
        VerifySnapshotAsync("email-confirmation.html", new EmailConfirmationContext(Username, Link, Entreprise, Phone));

    [Test]
    public Task ResetPassword_MatchesSnapshot() => 
        VerifySnapshotAsync("reset-password.html", new ResetPasswordContext(Username, Link, Entreprise, Phone));

    [Test]
    public Task AfterEmailConfirmation_MatchesSnapshot() => 
        VerifySnapshotAsync("after-email-confirmation.html", new AfterEmailConfirmationContext());

    [Test]
    public Task AfterResetPassword_MatchesSnapshot() => 
        VerifySnapshotAsync("after-reset-password.html", new AfterResetPasswordContext());

    [Test]
    public Task ClientMessageToAdmin_MatchesSnapshot() => 
        VerifySnapshotAsync("client-message-to-admin.html", new ClientMessageToAdminContext(ClientEmail, Message));
}
