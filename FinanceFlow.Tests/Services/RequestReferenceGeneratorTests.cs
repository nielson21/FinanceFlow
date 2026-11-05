using FinanceFlow.Services;
using Microsoft.Extensions.Time.Testing;

namespace FinanceFlow.Tests.Services;

public class RequestReferenceGeneratorTests
{
    [Fact]
    public void Generate_ShouldUseFakeCurrentYear()
    {
        // Arrange
        var fakeTime = new FakeTimeProvider(
            new DateTimeOffset(
                2030, 1, 1,
                0, 0, 0,
                TimeSpan.Zero));

        var generator =
            new RequestReferenceGenerator(fakeTime);

        // Act
        var result = generator.Generate();

        // Assert
        Assert.StartsWith("FR-2030-", result);
        Assert.Equal(14, result.Length);
    }
}