using System.Reflection;
using API;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VedAstro.Library.Tests;

[TestClass]
public class LocationPipelineTests
{
    private static readonly GeoLocation Sydney =
        new("Sydney, Australia", 151.2093, -33.8688);

    [DataTestMethod]
    [DataRow("2020-06-15T00:00:00+00:00", "+10:00")]
    [DataRow("2020-12-15T00:00:00+00:00", "+11:00")]
    public async Task OfflineTimezoneLookupUsesHistoricalTzdbOffset(string timestamp, string expectedOffset)
    {
        var manager = new LocationManager();

        var actualOffset = await manager.GeoLocationToTimezone(Sydney, DateTimeOffset.Parse(timestamp));

        Assert.AreEqual(expectedOffset, actualOffset);
    }

    [TestMethod]
    public void ExhaustedAddressLookupFailsInsteadOfReturningIpohSentinel()
    {
        const string variableName = "VEDASTRO_NOMINATIM_URL";
        var originalUrl = Environment.GetEnvironmentVariable(variableName);
        Environment.SetEnvironmentVariable(variableName, "http://127.0.0.1:1");

        try
        {
            var address = $"unresolvable-{Guid.NewGuid():N}";

            var exception = Assert.ThrowsException<InvalidOperationException>(
                () => Calculate.AddressToGeoLocation(address));

            StringAssert.Contains(exception.Message, address);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, originalUrl);
        }
    }

    [TestMethod]
    public void OpenApiFailurePayloadUsesInnermostMeaningfulExceptionMessage()
    {
        const string expectedMessage =
            "Address lookup failed for 'X': all geocoding providers were exhausted.";
        var exception = new TargetInvocationException(
            new AggregateException(new InvalidOperationException(expectedMessage)));

        var actualMessage = APITools.GetInnermostExceptionMessage(exception);

        Assert.AreEqual(expectedMessage, actualMessage);
    }
}
