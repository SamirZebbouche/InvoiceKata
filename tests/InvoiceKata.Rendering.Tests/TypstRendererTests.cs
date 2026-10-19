using InvoiceKata.Adapters.Rendering;
using InvoiceKata.Application.Quotes;
using InvoiceKata.Application.Quotes.Mentions;
using InvoiceKata.Application.Rendering;
using InvoiceKata.Domain;

using UglyToad.PdfPig;

namespace InvoiceKata.Rendering.Tests;

// Real renderings through the Typst CLI. Each PDF is kept under rendered/, which the CI publishes.
public sealed class TypstRendererTests
{
    private static readonly FrenchMentionTexts Texts = new();
    private readonly TypstRenderer _renderer = new("typst");

    [Fact]
    public async Task Should_RenderTheNumberTheDateTheLinesTheTotalsAndTheMentions_When_GivenAQuote()
    {
        var pdf = await RenderAsync("devis", Quote(lines: 1));

        var text = Text(pdf);
        string[] expected =
        [
            "DEVIS", "D-2026-0001", "18/10/2026", "Batifolio", "Les Toits Rouges SARL", "Audit 1", "1600,00 €",
            "Total TTC", "1920,00 €", "Devis valable 30 jours, jusqu'au 17/11/2026.", "SAS au capital de 20000 €", "Bon pour accord",
        ];
        Assert.All(expected, fragment => Assert.Contains(Flat(fragment), text, StringComparison.Ordinal));
    }

    // The same template lays out the invoice: its title, its quote reference, its payment mentions, and no acceptance box.
    [Fact]
    public async Task Should_RenderTheInvoiceWithItsQuoteReferenceAndPaymentMentions_When_GivenAnInvoice()
    {
        var pdf = await RenderAsync("facture", Invoice());

        var text = Text(pdf);
        string[] expected =
        [
            "FACTURE", "F-2026-0001", "30/11/2026", "D'après le devis n° D-2026-0001 du 18/10/2026", "1920,00 €", "Échéance : 30/12/2026.",
            "Escompte pour paiement anticipé : néant.", "Pénalités de retard : trois fois le taux d'intérêt légal.",
            "Indemnité forfaitaire pour frais de recouvrement : 40 €.", "Facture F-2026-0001 — page 1 / 1",
        ];
        Assert.All(expected, fragment => Assert.Contains(Flat(fragment), text, StringComparison.Ordinal));
        Assert.DoesNotContain(Flat("Bon pour accord"), text, StringComparison.Ordinal);
    }

    // The EI template (ADR-0011): no VAT column nor VAT line, the name followed by "EI", and the franchise mention.
    [Fact]
    public async Task Should_RenderAQuoteWithoutVat_When_TheIssuerIsAnIndividualEntrepreneurInFranchise()
    {
        var pdf = await RenderAsync("devis-ei", EntrepreneurQuote());

        var text = Text(pdf);
        string[] expected =
        [
            "DEVIS", "Jean Dupont EI", "D-2026-0001", "Audit 1", "1600,00 €", "Net à payer",
            "TVA non applicable, art. L. 223 et s. du code des impositions sur les biens et services (CIBS)", "Bon pour accord",
        ];
        Assert.All(expected, fragment => Assert.Contains(Flat(fragment), text, StringComparison.Ordinal));
        Assert.All(["Total TVA", "Total TTC", "TVA intracommunautaire"], fragment => Assert.DoesNotContain(Flat(fragment), text, StringComparison.Ordinal));
    }

    // The number and an n/N pagination on every page (ADR-0005, decision 5).
    [Fact]
    public async Task Should_PutTheNumberAndThePaginationOnEveryPage_When_TheQuoteRunsOverSeveralPages()
    {
        var pdf = await RenderAsync("devis-long", Quote(lines: 60));

        using var document = PdfDocument.Open(pdf);
        var pages = document.GetPages().ToList();
        Assert.True(pages.Count > 1, $"60 lines were expected to run over several pages, not {pages.Count}.");
        Assert.All(pages, page => Assert.Contains(Flat($"D-2026-0001 — page {page.Number} / {pages.Count}"), Flat(page.Text), StringComparison.Ordinal));
    }

    // The totals, the mentions and the acceptance box always share a page (ADR-0005, decision 6), wherever the
    // lines end: these lengths end the table at the top, the middle and the bottom of a page.
    [Theory]
    [InlineData(60)]
    [InlineData(84)]
    [InlineData(88)]
    [InlineData(92)]
    [InlineData(96)]
    public async Task Should_KeepTheTotalsTheMentionsAndTheAcceptanceBoxTogether_When_TheQuoteRunsOverSeveralPages(int lines)
    {
        var pdf = await RenderAsync($"devis-{lines}-lignes", Quote(lines));

        using var document = PdfDocument.Open(pdf);
        var last = Flat(document.GetPages().Last().Text);
        Assert.All(["Total TTC", "Devis valable 30 jours", "Bon pour accord"], fragment => Assert.Contains(Flat(fragment), last, StringComparison.Ordinal));
    }

    // Each rendering works in a folder of its own, removed afterwards, whether Typst succeeds or not.
    [Theory]
    [InlineData("typst")]
    [InlineData("typst-is-not-installed")]
    public async Task Should_LeaveNoWorkingFolderBehind_When_TheRenderingEnds(string executable)
    {
        var root = Directory.CreateTempSubdirectory("invoicekata-render-root-");
        try
        {
            await Record.ExceptionAsync(() => new TypstRenderer(executable, root.FullName).RenderAsync(Quote(lines: 1), CancellationToken.None));

            Assert.Empty(root.EnumerateFileSystemInfos());
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }

    // The rendering deletes its own working folder only, never the folder around it.
    [Fact]
    public async Task Should_DeleteOnlyItsOwnWorkingFolder_When_TheRenderingEnds()
    {
        var root = Directory.CreateTempSubdirectory("invoicekata-render-root-");
        var witness = Path.Combine(root.FullName, "witness.txt");
        await File.WriteAllTextAsync(witness, "still here");
        try
        {
            await new TypstRenderer("typst", root.FullName).RenderAsync(Quote(lines: 1), CancellationToken.None);

            Assert.True(File.Exists(witness), "The rendering deleted more than its own working folder.");
        }
        finally
        {
            if (root.Exists)
            {
                root.Delete(recursive: true);
            }
        }
    }

    // The rendering works under the folder it is given: a folder that cannot be created makes it fail.
    [Fact]
    public async Task Should_WorkUnderTheGivenFolder_When_OneIsGiven()
    {
        var file = Path.GetTempFileName();
        try
        {
            var refusal = await Record.ExceptionAsync(() => new TypstRenderer("typst", file).RenderAsync(Quote(lines: 1), CancellationToken.None));

            Assert.IsAssignableFrom<IOException>(refusal);
        }
        finally
        {
            File.Delete(file);
        }
    }

    // A document the template cannot lay out makes Typst fail: the renderer says so rather than return no PDF.
    [Fact]
    public async Task Should_FailAndSayWhy_When_TypstCannotCompileTheDocument()
    {
        var document = Quote(lines: 1);

        var refusal = await Record.ExceptionAsync(() => _renderer.RenderAsync(document with { Document = document.Document with { Kind = "letter" } }, CancellationToken.None));

        Assert.Contains("Typst failed to render the document", Assert.IsType<InvalidOperationException>(refusal).Message, StringComparison.Ordinal);
    }

    // The template follows the kind of issuer: a kind without one is refused before anything is rendered.
    [Fact]
    public async Task Should_FailAndSayWhy_When_TheIssuerKindHasNoTemplate()
    {
        var document = Quote(lines: 1);

        var refusal = await Record.ExceptionAsync(() => _renderer.RenderAsync(document with { Issuer = document.Issuer with { Kind = "trust" } }, CancellationToken.None));

        Assert.Contains("trust", Assert.IsType<ArgumentOutOfRangeException>(refusal).Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Should_FailAndSayWhy_When_TypstCannotBeRun()
    {
        var refusal = await Record.ExceptionAsync(() => new TypstRenderer("typst-is-not-installed").RenderAsync(Quote(lines: 1), CancellationToken.None));

        var failure = Assert.IsType<InvalidOperationException>(refusal);
        Assert.Contains("typst-is-not-installed", failure.Message, StringComparison.Ordinal);
    }

    private async Task<byte[]> RenderAsync(string name, ResolvedDocument document)
    {
        var pdf = await _renderer.RenderAsync(document, CancellationToken.None);
        var folder = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "rendered"));
        await File.WriteAllBytesAsync(Path.Combine(folder.FullName, $"{name}.pdf"), pdf);
        return pdf;
    }

    private static ResolvedDocument Quote(int lines)
    {
        var (quote, issuer, customer) = Finalized(lines);
        return ResolvedDocument.OfQuote(new QuoteScope(quote, issuer, customer));
    }

    private static (InvoiceKata.Domain.Quote Quote, Issuer Issuer, Customer Customer) Finalized(int lines)
    {
        var issuer = Issuer.ConfigureCompany(
            "Batifolio", LegalForm.Sas, 20000m, new Siren("912345678"), new VatNumber("FR12912345678"),
            new Address { Street = "12 rue des Artisans", PostalCode = "47000", City = "Agen" });
        var customer = Customer.RegisterProfessional(
            issuer.Id, "Les Toits Rouges SARL", new Siren("834567890"), new VatNumber("FR45834567890"),
            new Address { Street = "4 place du Marché", PostalCode = "33000", City = "Bordeaux" });
        var quote = InvoiceKata.Domain.Quote.DraftFor(issuer, customer);
        for (var line = 1; line <= lines; line++)
        {
            quote.AddLine(new NewQuoteLine
            {
                Designation = $"Audit {line}",
                Quantity = new Quantity(2m),
                Unit = "jour",
                UnitPriceExcludingVat = new Money(800m),
                VatRate = new VatRate(20m),
            });
        }

        quote.PutMention(new LegalMention(MentionCode.IssuerLegalIdentity, Texts.IssuerLegalIdentity(issuer)));
        quote.Finalize(new QuoteNumber(2026, 1), new DateOnly(2026, 10, 18), new DateOnly(2026, 11, 17), Texts.QuoteValidity(30, new DateOnly(2026, 11, 17)));
        return (quote, issuer, customer);
    }

    private static ResolvedDocument EntrepreneurQuote()
    {
        var issuer = Issuer.ConfigureIndividualEntrepreneur(
            "Jean Dupont", new Siren("853123456"), new Address { Street = "8 chemin des Vignes", PostalCode = "47000", City = "Agen" });
        var customer = Customer.RegisterProfessional(
            issuer.Id, "Les Toits Rouges SARL", new Siren("834567890"), new VatNumber("FR45834567890"),
            new Address { Street = "4 place du Marché", PostalCode = "33000", City = "Bordeaux" });
        var quote = InvoiceKata.Domain.Quote.DraftFor(issuer, customer);
        quote.AddLine(new NewQuoteLine
        {
            Designation = "Audit 1",
            Quantity = new Quantity(2m),
            Unit = "jour",
            UnitPriceExcludingVat = new Money(800m),
            VatRate = new VatRate(0m),
        });
        quote.PutMention(new LegalMention(MentionCode.IssuerLegalIdentity, Texts.IssuerLegalIdentity(issuer)));
        quote.PutMention(new LegalMention(MentionCode.VatFranchise, Texts.VatFranchise()));
        quote.Finalize(new QuoteNumber(2026, 1), new DateOnly(2026, 10, 18), new DateOnly(2026, 11, 17), Texts.QuoteValidity(30, new DateOnly(2026, 11, 17)));
        return ResolvedDocument.OfQuote(new QuoteScope(quote, issuer, customer));
    }

    private static ResolvedDocument Invoice()
    {
        var (quote, issuer, customer) = Finalized(lines: 1);
        var invoice = InvoiceKata.Domain.Invoice.IssueFor(quote, new InvoiceNumber(2026, 1), new DateOnly(2026, 11, 30), new DateOnly(2026, 12, 30), [
            new(MentionCode.PaymentDueDate, Texts.InvoiceDueDate(new DateOnly(2026, 12, 30))),
            new(MentionCode.EarlyPaymentDiscount, Texts.NoEarlyPaymentDiscount()),
            new(MentionCode.LatePaymentPenalties, Texts.LatePaymentPenalties()),
            new(MentionCode.RecoveryIndemnity, Texts.RecoveryIndemnity()),
        ]);
        return ResolvedDocument.OfInvoice(invoice, issuer, customer);
    }

    private static string Text(byte[] pdf)
    {
        using var document = PdfDocument.Open(pdf);
        return Flat(string.Concat(document.GetPages().Select(page => page.Text)));
    }

    // The PDF spaces words and thousands its own way, and Typst curls apostrophes: compare without either.
    private static string Flat(string text) => string.Concat(text.Where(character => !char.IsWhiteSpace(character))).Replace('’', '\'');
}
