using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VedAstro.Library.Tests;

/// <summary>
/// Stable snapshots captured from vedastro.org's public calculation API.
/// They make the deployed engine—not the incomplete public master—the
/// compatibility oracle for recovered calculation paths.
/// </summary>
[TestClass]
public class HostedOracleRegressionTests
{
    private static readonly Time SingaporeJ2000 = new(
        "12:00 01/01/2000 +08:00",
        new GeoLocation("Singapore, Singapore", 103.85, 1.289));

    [TestInitialize]
    public void UseProductionAyanamsa() => Calculate.Ayanamsa = (int)Ayanamsa.LAHIRI;

    [TestMethod]
    public void PlanetLongitudesMatchHostedApiWithinOneArcSecond()
    {
        AssertDegrees(256.17583333333334,
            Calculate.PlanetNirayanaLongitude(PlanetName.Sun, SingaporeJ2000).TotalDegrees);
        AssertDegrees(195.45472222222222,
            Calculate.PlanetNirayanaLongitude(PlanetName.Moon, SingaporeJ2000).TotalDegrees);
        Assert.AreEqual(8.587264987110146,
            Calculate.PlanetIshtaScore(PlanetName.Sun, SingaporeJ2000), 0.000001);
    }

    [TestMethod]
    public void LagnaAndDivisionalSignsMatchHostedApi()
    {
        var lagna = Calculate.HouseRasiSign(HouseName.House1, SingaporeJ2000);
        Assert.AreEqual(ZodiacName.Aquarius, lagna.GetSignName());
        AssertDegrees(29.525555555555556, lagna.GetDegreesInSign().TotalDegrees, 10);

        var navamsha = Calculate.PlanetNavamshaD9Sign(PlanetName.Sun, SingaporeJ2000);
        Assert.AreEqual(ZodiacName.Leo, navamsha.GetSignName());
        AssertDegrees(25.579722222222223, navamsha.GetDegreesInSign().TotalDegrees);

    }

    [TestMethod]
    public void ShadbalaDerivedScoresMatchHostedApi()
    {
        var standardHoroscope = new Time(
            "14:20 16/10/1918 +05:30",
            new GeoLocation("Bengaluru, Karnataka, India", 77.575, 12.977));

        Assert.AreEqual(8.912938984661976,
            Calculate.PlanetIshtaScore(PlanetName.Sun, standardHoroscope), 0.000001);
        Assert.AreEqual(45.95948013933187,
            Calculate.PlanetKashtaScore(PlanetName.Sun, standardHoroscope), 0.000001);
        Assert.AreEqual(-3.502,
            Calculate.PlanetIshtaKashtaScoreDegree(PlanetName.Venus, standardHoroscope), 0.0005);
    }

    private static void AssertDegrees(double expected, double actual, double toleranceArcSeconds = 1) =>
        Assert.AreEqual(expected, actual, toleranceArcSeconds / 3600d,
            $"Expected {expected:F8}°, got {actual:F8}°.");
}
