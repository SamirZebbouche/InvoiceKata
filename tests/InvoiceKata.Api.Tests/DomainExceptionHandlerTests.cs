using System.Runtime.CompilerServices;

using InvoiceKata.Application.Issuers;
using InvoiceKata.Domain;

using Microsoft.AspNetCore.Http;

namespace InvoiceKata.Api.Tests;

public sealed class DomainExceptionHandlerTests
{
    // Every exception the product defines, in the domain and the application: a new one without an HTTP answer fails here.
    public static TheoryData<Type> AllProductExceptions =>
        [.. new[] { typeof(Quote).Assembly, typeof(ConfigureIssuer).Assembly }
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(Exception).IsAssignableFrom(type) && !type.IsAbstract)
            .OrderBy(type => type.FullName)];

    [Theory]
    [MemberData(nameof(AllProductExceptions))]
    public void Should_TranslateTheException_When_GivenAnyExceptionOfTheProduct(Type exceptionType)
    {
        var exception = (Exception)RuntimeHelpers.GetUninitializedObject(exceptionType);

        var status = DomainExceptionHandler.StatusFor(exception);

        Assert.NotNull(status);
    }

    [Fact]
    public void Should_LeaveTheExceptionUntranslated_When_ItIsNotAnExceptionOfTheProduct()
    {
        var status = DomainExceptionHandler.StatusFor(new ArgumentException("A bug, not a refusal."));

        Assert.Null(status);
    }

    // A bug is not answered here: the handler lets it through, untouched, for the 500 of the framework.
    [Fact]
    public async Task Should_LetTheExceptionThrough_When_ItIsNotAnExceptionOfTheProduct()
    {
        var context = new DefaultHttpContext();

        var handled = await new DomainExceptionHandler(problemDetails: null!)
            .TryHandleAsync(context, new ArgumentException("A bug, not a refusal."), CancellationToken.None);

        Assert.Equal((false, StatusCodes.Status200OK), (handled, context.Response.StatusCode));
    }
}
