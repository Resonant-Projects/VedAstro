using Microsoft.VisualStudio.TestTools.UnitTesting;
using SwissEphNet;

namespace VedAstro.Library.Tests;

[TestClass]
[DoNotParallelize]
public class PlanetSignTransitTests
{
    private static Time At(string value) => new(value, new GeoLocation("Greenwich", 0, 0));

    [DataTestMethod]
    [DataRow("Sun")]
    [DataRow("Moon")]
    [DataRow("Mars")]
    [DataRow("Mercury")]
    [DataRow("Jupiter")]
    [DataRow("Venus")]
    [DataRow("Saturn")]
    [DataRow("Rahu")]
    [DataRow("Ketu")]
    [DataRow("Uranus")]
    [DataRow("Neptune")]
    [DataRow("Pluto")]
    public void StableWindowIncludesItsFinalInterval(string name)
    {
        var planet = PlanetName.Parse(name);
        Assert.AreNotEqual(PlanetName.Empty, planet);
        var start = At("00:00 01/06/2026 +00:00");
        var end = start.AddHours(1);
        var rows = Calculate.PlanetSignTransit(start, end, planet);
        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual(start, rows[0].Item1);
        Assert.AreEqual(end, rows[0].Item2);
        Assert.AreEqual(Calculate.PlanetZodiacSign(planet, start).GetSignName(), rows[0].Item3);
        Assert.AreEqual(planet, rows[0].Item4);
    }

    [TestMethod]
    public void CrossingWindowRetainsBothIntervalsAndRequestedEnd()
    {
        var start = At("00:00 17/03/2025 +00:00");
        var end = At("00:00 20/03/2025 +00:00");
        var rows = Calculate.PlanetSignTransit(start, end, PlanetName.Parse("Uranus"));
        Assert.AreEqual(2, rows.Count);
        Assert.AreEqual(start, rows[0].Item1);
        Assert.AreEqual(rows[0].Item2, rows[1].Item1);
        Assert.AreEqual(end, rows[1].Item2);
        Assert.AreNotEqual(rows[0].Item3, rows[1].Item3);
        Assert.AreEqual(Calculate.PlanetZodiacSign(PlanetName.Parse("Uranus"), end).GetSignName(), rows[1].Item3);
        foreach (var row in rows) Assert.IsTrue(row.Item1 < row.Item2);

        // An interval ending at the detected transition must not gain a zero-length tail.
        var endingAtBoundary = Calculate.PlanetSignTransit(start, rows[0].Item2, PlanetName.Parse("Uranus"));
        Assert.AreEqual(1, endingAtBoundary.Count);
        Assert.AreEqual(rows[0].Item2, endingAtBoundary[0].Item2);
    }

    [DataTestMethod]
    [DataRow("Uranus", SwissEph.SE_URANUS)]
    [DataRow("Neptune", SwissEph.SE_NEPTUNE)]
    [DataRow("Pluto", SwissEph.SE_PLUTO)]
    public void OuterPlanetNamesUseTheirOwnSwissLongitude(string name, int swissBody)
    {
        var planet = PlanetName.Parse(name);
        Assert.IsTrue(PlanetName.TryParse(name.ToLowerInvariant(), out var parsed));
        Assert.AreEqual(planet, parsed);
        Assert.AreEqual(swissBody, Tools.VedAstroToSwissEph(planet));
        var time = At("00:00 01/06/2026 +00:00");
        using var ephemeris = EphemerisFactory.New();
        var expected = new double[6];
        string error = "";
        int flags = ephemeris.swe_calc(Calculate.TimeToEphemerisTime(time), swissBody,
            SwissEph.SEFLG_SWIEPH, expected, ref error);
        Assert.IsTrue(flags >= 0, error);
        Assert.AreEqual(expected[0], Calculate.PlanetSayanaLongitude(planet, time).TotalDegrees, 1d / 3600);
        var sidereal = (expected[0] - Calculate.AyanamsaDegree(time).TotalDegrees + 360) % 360;
        Assert.AreEqual(Calculate.ZodiacSignAtLongitude(Angle.FromDegrees(sidereal)).GetSignName(),
            Calculate.PlanetZodiacSign(planet, time).GetSignName());
        Assert.IsFalse(PlanetName.All9Planets.Contains(planet));
        Assert.AreEqual(9, PlanetName.All9Planets.Count);
    }

    [TestMethod]
    public void UnknownPlanetAndReversedWindowAreRejected()
    {
        var time = At("00:00 01/06/2026 +00:00");
        Assert.ThrowsException<ArgumentException>(() =>
            Calculate.PlanetSignTransit(time, time.AddHours(1), PlanetName.Parse("not-a-planet")));
        Assert.ThrowsException<ArgumentException>(() =>
            Calculate.PlanetSignTransit(time.AddHours(1), time, PlanetName.Saturn));
    }

    [TestMethod]
    public void EmptyWindowHasNoIntervals()
    {
        var instant = At("00:00 01/06/2026 +00:00");
        Assert.AreEqual(0, Calculate.PlanetSignTransit(instant, instant, PlanetName.Parse("Uranus")).Count);
    }
}
