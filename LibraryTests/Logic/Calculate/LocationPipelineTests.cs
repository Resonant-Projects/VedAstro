using System.Reflection;
using API;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace VedAstro.Library.Tests;

[TestClass]
[DoNotParallelize]
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
    public void GeoapifyParserReturnsValidFeaturesInOrderAndSkipsMissingCoordinates()
    {
        var response = JObject.Parse("""
            {
              "features": [
                {
                  "properties": {
                    "formatted": "Sydney, NSW, Australia",
                    "lat": -33.8698439,
                    "lon": 151.2082848
                  }
                },
                {
                  "properties": {
                    "formatted": "Missing latitude",
                    "lon": 151.0
                  }
                },
                {
                  "properties": {
                    "formatted": "Melbourne, VIC, Australia",
                    "lat": -37.8142454,
                    "lon": 144.9631732
                  }
                }
              ]
            }
            """);

        var locations = LocationManager.ParseGeoapifyGeoLocations(response);

        Assert.AreEqual(2, locations.Count);
        Assert.AreEqual("Sydney, NSW, Australia", locations[0].Name());
        Assert.AreEqual(151.2082848, locations[0].Longitude());
        Assert.AreEqual(-33.8698439, locations[0].Latitude());
        Assert.AreEqual("Melbourne, VIC, Australia", locations[1].Name());
    }

    [TestMethod]
    public void KeylessGeoapifyFallsThroughAndExhaustedAddressLookupFailsClosed()
    {
        const string keyVariableName = "GEOAPIFY_API_KEY";
        const string geoapifyUrlVariableName = "VEDASTRO_GEOAPIFY_URL";
        const string nominatimUrlVariableName = "VEDASTRO_NOMINATIM_URL";
        var originalKey = Environment.GetEnvironmentVariable(keyVariableName);
        var originalGeoapifyUrl = Environment.GetEnvironmentVariable(geoapifyUrlVariableName);
        var originalNominatimUrl = Environment.GetEnvironmentVariable(nominatimUrlVariableName);
        var originalConsoleOut = Console.Out;
        var consoleOutput = new StringWriter();
        Environment.SetEnvironmentVariable(keyVariableName, null);
        Environment.SetEnvironmentVariable(geoapifyUrlVariableName, "http://127.0.0.1:1");
        Environment.SetEnvironmentVariable(nominatimUrlVariableName, "http://127.0.0.1:1");
        Console.SetOut(consoleOutput);

        try
        {
            var address = $"unresolvable-{Guid.NewGuid():N}";

            var exception = Assert.ThrowsException<InvalidOperationException>(
                () => Calculate.AddressToGeoLocation(address));

            StringAssert.Contains(exception.Message, address);
            Assert.IsFalse(consoleOutput.ToString().Contains("Geoapify"),
                "Keyless Geoapify provider should return silently without making a request.");
        }
        finally
        {
            Console.SetOut(originalConsoleOut);
            Environment.SetEnvironmentVariable(keyVariableName, originalKey);
            Environment.SetEnvironmentVariable(geoapifyUrlVariableName, originalGeoapifyUrl);
            Environment.SetEnvironmentVariable(nominatimUrlVariableName, originalNominatimUrl);
            consoleOutput.Dispose();
        }
    }

    [TestMethod]
    public void UnreachableGeoapifyFallsThroughWithoutLeakingApiKeyAndAddressLookupFailsClosed()
    {
        const string keyVariableName = "GEOAPIFY_API_KEY";
        const string geoapifyUrlVariableName = "VEDASTRO_GEOAPIFY_URL";
        const string nominatimUrlVariableName = "VEDASTRO_NOMINATIM_URL";
        var originalKey = Environment.GetEnvironmentVariable(keyVariableName);
        var originalGeoapifyUrl = Environment.GetEnvironmentVariable(geoapifyUrlVariableName);
        var originalNominatimUrl = Environment.GetEnvironmentVariable(nominatimUrlVariableName);
        var originalConsoleOut = Console.Out;
        var consoleOutput = new StringWriter();
        var apiKey = $"test-secret-{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(keyVariableName, apiKey);
        Environment.SetEnvironmentVariable(geoapifyUrlVariableName, "http://127.0.0.1:1");
        Environment.SetEnvironmentVariable(nominatimUrlVariableName, "http://127.0.0.1:1");
        Console.SetOut(consoleOutput);

        try
        {
            var address = $"unresolvable-{Guid.NewGuid():N}";

            var exception = Assert.ThrowsException<InvalidOperationException>(
                () => Calculate.AddressToGeoLocation(address));

            StringAssert.Contains(exception.Message, address);
            Assert.IsFalse(consoleOutput.ToString().Contains(apiKey), "Geoapify API key was written to logs.");
            StringAssert.Contains(consoleOutput.ToString(), "apiKey=REDACTED");
        }
        finally
        {
            Console.SetOut(originalConsoleOut);
            Environment.SetEnvironmentVariable(keyVariableName, originalKey);
            Environment.SetEnvironmentVariable(geoapifyUrlVariableName, originalGeoapifyUrl);
            Environment.SetEnvironmentVariable(nominatimUrlVariableName, originalNominatimUrl);
            consoleOutput.Dispose();
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
