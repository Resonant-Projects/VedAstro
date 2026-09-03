using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VedAstro.Library.Tests;

/// <summary>
/// Above the polar circles the Sun can stay above or below the horizon for the whole day.
/// Swiss Ephemeris then reports "rise or set not found" and the engine must answer with a
/// clear domain error instead of converting a zero Julian day into an un-representable DateTime.
/// Rows mirror the POLAR-DAY and POLAR-NIGHT entries of the downstream golden corpus.
/// </summary>
[TestClass]
public class PolarSunriseSunsetTests
{
    private static readonly GeoLocation Tromso = new("Tromso, Norway", 18.95, 69.65);

    private static readonly Time PolarDay = new("02:00 21/06/2026 +02:00", Tromso);

    private static readonly Time PolarNight = new("02:00 21/12/2026 +01:00", Tromso);

    private static readonly Time Equinox = new("12:00 21/03/2026 +01:00", Tromso);

    [TestInitialize]
    public void ResetCalculationSettings()
    {
        Calculate.Ayanamsa = (int)Ayanamsa.LAHIRI;
    }

    [TestMethod]
    public void SunriseTimeReportsPolarDayWhenTheSunNeverSets()
    {
        var exception = Assert.ThrowsException<PolarSunException>(() => Calculate.SunriseTime(PolarDay));

        Assert.IsTrue(exception.IsPolarDay);
        StringAssert.Contains(exception.Message, "No sunrise");
        StringAssert.Contains(exception.Message, "Tromso, Norway");
        StringAssert.Contains(exception.Message, "21/06/2026");
        StringAssert.Contains(exception.Message, "polar day");
    }

    [TestMethod]
    public void SunsetTimeReportsPolarDayWhenTheSunNeverSets()
    {
        var exception = Assert.ThrowsException<PolarSunException>(() => Calculate.SunsetTime(PolarDay));

        Assert.IsTrue(exception.IsPolarDay);
        StringAssert.Contains(exception.Message, "No sunset");
        StringAssert.Contains(exception.Message, "polar day");
    }

    [TestMethod]
    public void SunriseTimeReportsPolarNightWhenTheSunNeverRises()
    {
        var exception = Assert.ThrowsException<PolarSunException>(() => Calculate.SunriseTime(PolarNight));

        Assert.IsFalse(exception.IsPolarDay);
        StringAssert.Contains(exception.Message, "No sunrise");
        StringAssert.Contains(exception.Message, "21/12/2026");
        StringAssert.Contains(exception.Message, "polar night");
    }

    [TestMethod]
    public void SunsetTimeReportsPolarNightWhenTheSunNeverRises()
    {
        var exception = Assert.ThrowsException<PolarSunException>(() => Calculate.SunsetTime(PolarNight));

        Assert.IsFalse(exception.IsPolarDay);
        StringAssert.Contains(exception.Message, "No sunset");
        StringAssert.Contains(exception.Message, "polar night");
    }

    [TestMethod]
    public void PolarLatitudeStillHasSunriseAndSunsetOutsidePolarSeasons()
    {
        var sunrise = Calculate.SunriseTime(Equinox).GetStdDateTimeOffset();
        var sunset = Calculate.SunsetTime(Equinox).GetStdDateTimeOffset();

        Assert.AreEqual(new DateTime(2026, 3, 21), sunrise.Date);
        Assert.AreEqual(new DateTime(2026, 3, 21), sunset.Date);
        Assert.IsTrue(sunrise.Hour is >= 5 and <= 7, $"sunrise was {sunrise}");
        Assert.IsTrue(sunset.Hour is >= 17 and <= 19, $"sunset was {sunset}");
    }

    [TestMethod]
    public void PolarSeasonTransitionDoesNotMislabelOneMissingCrossingAsAllDayPolarState()
    {
        var transitionDay = new Time("02:00 22/05/2026 +02:00", Tromso);

        var sunrise = Calculate.SunriseTime(transitionDay).GetStdDateTimeOffset();
        var exception = Assert.ThrowsException<InvalidOperationException>(() => Calculate.SunsetTime(transitionDay));

        Assert.AreEqual(new DateTime(2026, 5, 22), sunrise.Date);
        StringAssert.Contains(exception.Message, "No sunset");
        StringAssert.Contains(exception.Message, "although sunrise was found");
    }
}
