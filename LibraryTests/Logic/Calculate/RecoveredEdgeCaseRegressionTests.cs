using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VedAstro.Library.Tests;

[TestClass]
public class RecoveredEdgeCaseRegressionTests
{
    [TestMethod]
    public void TrimshamshaUsesTraditionalOddAndEvenSegmentRulers()
    {
        AssertTrimshamsha(ZodiacName.Aries, 2, ZodiacName.Aries);
        AssertTrimshamsha(ZodiacName.Aries, 7, ZodiacName.Aquarius);
        AssertTrimshamsha(ZodiacName.Aries, 14, ZodiacName.Sagittarius);
        AssertTrimshamsha(ZodiacName.Aries, 20, ZodiacName.Gemini);
        AssertTrimshamsha(ZodiacName.Aries, 27, ZodiacName.Libra);

        AssertTrimshamsha(ZodiacName.Taurus, 2, ZodiacName.Taurus);
        AssertTrimshamsha(ZodiacName.Taurus, 7, ZodiacName.Virgo);
        AssertTrimshamsha(ZodiacName.Taurus, 14, ZodiacName.Pisces);
        AssertTrimshamsha(ZodiacName.Taurus, 22, ZodiacName.Capricorn);
        AssertTrimshamsha(ZodiacName.Taurus, 27, ZodiacName.Scorpio);
    }

    [TestMethod]
    public void KpHouseMembershipIncludesTwelfthHouseWrap()
    {
        var cusps = House.AllHouses.ToDictionary(
            house => house,
            house => Angle.FromDegrees(((int)house - 1) * 30));

        Assert.IsTrue(CalculateKP.IsPlanetInHouseKP(cusps, Angle.FromDegrees(350), HouseName.House12));
        Assert.IsFalse(CalculateKP.IsPlanetInHouseKP(cusps, Angle.FromDegrees(5), HouseName.House12));
        Assert.IsTrue(CalculateKP.IsPlanetInHouseKP(cusps, Angle.FromDegrees(5), HouseName.House1));
        Assert.IsFalse(CalculateKP.IsPlanetInHouseKP(cusps, Angle.FromDegrees(350), HouseName.House11));
    }

    [TestMethod]
    public void RecoveredCompatibilityMethodsDoNotReturnHistoricalDummyValues()
    {
        Assert.IsTrue(Calculate.IsPlanetYogakarakaToLagna(PlanetName.Saturn, ZodiacName.Taurus));
        Assert.IsFalse(Calculate.IsPlanetYogakarakaToLagna(PlanetName.Mars, ZodiacName.Taurus));

        var time = new Time(
            "12:00 01/01/2000 +08:00",
            new GeoLocation("Singapore, Singapore", 103.85, 1.289));
        Assert.AreEqual(PlanetMotion.Direct, Calculate.PlanetMotionName(PlanetName.Sun, time));
    }

    [TestMethod]
    public void IncompletePunyaSahamFailsExplicitly()
    {
        var time = new Time(
            "12:00 01/01/2000 +08:00",
            new GeoLocation("Singapore, Singapore", 103.85, 1.289));

        Assert.ThrowsException<NotImplementedException>(() => Calculate.PunyaSahamLongitude(time));
    }

    private static void AssertTrimshamsha(
        ZodiacName sourceSign,
        double degrees,
        ZodiacName expectedDivisionSign)
    {
        var result = Calculate.TrimshamshaSignName(
            new ZodiacSign(sourceSign, Angle.FromDegrees(degrees)));

        Assert.AreEqual(expectedDivisionSign, result.GetSignName());
    }
}
