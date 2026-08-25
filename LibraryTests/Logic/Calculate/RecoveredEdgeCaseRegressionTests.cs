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

        Assert.AreEqual(6, Calculate.DivisionalLongitude(4, 9).TotalDegrees, 0.000001);
        Assert.AreEqual(ZodiacName.Taurus, Calculate.ZodiacSignAtLongitude(Angle.FromDegrees(30)).GetSignName());
        Assert.AreEqual(ZodiacName.Aries, Calculate.ZodiacSignAtLongitude(Angle.FromDegrees(360)).GetSignName());
        Assert.AreEqual(ConstellationName.Aswini,
            Calculate.ConstellationAtLongitude(Angle.Zero).GetConstellationName());
        Assert.AreEqual(ConstellationName.Aswini,
            Calculate.ConstellationAtLongitude(Angle.Degrees360).GetConstellationName());
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
        Assert.IsFalse(CalculateKP.IsPlanetInHouseKP(cusps, Angle.FromDegrees(30), HouseName.House1));
        Assert.IsTrue(CalculateKP.IsPlanetInHouseKP(cusps, Angle.FromDegrees(30), HouseName.House2));
        Assert.IsFalse(CalculateKP.IsPlanetInHouseKP(cusps, Angle.FromDegrees(350), HouseName.House11));
        Assert.AreEqual(HouseName.House12, CalculateKP.HouseAtLongitude(cusps, Angle.FromDegrees(350)));

        cusps.Remove(HouseName.House1);
        Assert.IsFalse(CalculateKP.IsPlanetInHouseKP(cusps, Angle.FromDegrees(350), HouseName.House12));
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

    [TestMethod]
    public void OtherUnsupportedDivisionsAndInvalidHoraryInputsFailExplicitly()
    {
        Assert.ThrowsException<NotImplementedException>(() =>
            Calculate.PanchamsaSignName(new ZodiacSign(ZodiacName.Aries, Angle.FromDegrees(10))));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => CalculateKP.HoraryNumberSiderealAsc(0));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => CalculateKP.HoraryNumberSiderealAsc(1000));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            CalculateKP.ConvertAscToARMC(360, 23.4, 0, Time.Empty));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            Calculate.GenerateTimeListCSV(Time.Empty, Time.Empty, 0));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            Calculate.FindBirthTimeByAnimal(Time.Empty, 0));
    }

    [TestMethod]
    public void CoordinateRecoveryPreservesTheAlreadyValidAxis()
    {
        var location = new GeoLocation("Recovered latitude", 77.575, 129770000);

        Assert.AreEqual(77.575, location.Longitude(), 0.000001);
        Assert.AreEqual(12.977, location.Latitude(), 0.000001);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            new GeoLocation("Unrecoverable", 99999999999, 99999999999));
    }

    [TestMethod]
    public void UpagrahasWithoutCardinalPointsHaveNoDigBala()
    {
        var time = new Time(
            "12:00 01/01/2000 +08:00",
            new GeoLocation("Singapore, Singapore", 103.85, 1.289));

        Assert.AreEqual(Shashtiamsa.Zero, Calculate.PlanetDigBala(PlanetName.Gulika, time));
    }

    [TestMethod]
    public void CompatibilityInputsRoundDeterministicallyAndRejectOverflow()
    {
        Assert.AreEqual(TimeSpan.FromMinutes(1), Calculate.LongitudeToLMTOffset(0.125));
        Assert.AreEqual(TimeSpan.FromMinutes(-1), Calculate.LongitudeToLMTOffset(-0.125));

        var birthTime = new Time(
            "12:00 01/01/2000 +00:00",
            new GeoLocation("Greenwich", 0, 51.4934));
        var result = Calculate.AutoCalculateTimeRange(
            birthTime,
            "999999999999years",
            TimeSpan.Zero);

        Assert.AreEqual(TimeRange.Empty, result);

        Calculate.Ayanamsa = (int)Ayanamsa.LAHIRI;
        var raman = Task.Run(() =>
        {
            Calculate.Ayanamsa = (int)Ayanamsa.RAMAN;
            return Calculate.Ayanamsa;
        });
        var lahiri = Task.Run(() => Calculate.Ayanamsa);
        Task.WaitAll(raman, lahiri);
        Assert.AreEqual((int)Ayanamsa.RAMAN, raman.Result);
        Assert.AreEqual((int)Ayanamsa.LAHIRI, lahiri.Result);
        Assert.AreEqual((int)Ayanamsa.LAHIRI, Calculate.Ayanamsa);
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
