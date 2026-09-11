using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VedAstro.Library.Tests;

[TestClass]
[DoNotParallelize]
public class ExplicitOffsetTests
{
    [DataTestMethod]
    [DataRow("00:00", "28", "+00:00")]
    [DataRow("01:00", "28", "+01:00")]
    [DataRow("23:00", "27", "-01:00")]
    public async Task UrlParserPreservesTheInstantAcrossExplicitOffsets(string clock, string day, string offset)
    {
        Time time = await Time.FromUrl($"Location/51.4779,-0.0015/Time/{clock}/{day}/05/1980/{offset}");

        Assert.AreEqual(DateTimeOffset.Parse("1980-05-28T00:00:00Z"), time.GetStdDateTimeOffset());
        Assert.AreEqual(TimeSpan.Parse(offset.TrimStart('+')), time.GetStdDateTimeOffset().Offset);
    }

    [TestMethod]
    public async Task ParsedUtcMoonMatchesIndependentReferenceDuringBritishSummerTime()
    {
        Time time = await Time.FromUrl("Location/51.4779,-0.0015/Time/00:00/28/05/1980/+00:00");
        int originalAyanamsa = Calculate.Ayanamsa;
        try
        {
            Calculate.Ayanamsa = SwissEphNet.SwissEph.SE_SIDM_LAHIRI;
            var longitude = Calculate.PlanetNirayanaLongitude(PlanetName.Moon, time);
            // pyswisseph 2.10.3.2 / Swiss 2.10.03, SWIEPH | SIDEREAL, Lahiri, UTC.
            Assert.AreEqual(201.34893154648788, longitude.TotalDegrees, 0.01);
        }
        finally
        {
            Calculate.Ayanamsa = originalAyanamsa;
        }
    }
}
