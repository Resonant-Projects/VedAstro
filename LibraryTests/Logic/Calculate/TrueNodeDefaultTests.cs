using Microsoft.VisualStudio.TestTools.UnitTesting;
using SwissEphNet;

namespace VedAstro.Library.Tests;

[TestClass]
[DoNotParallelize]
public class TrueNodeDefaultTests
{
    [TestMethod]
    public void DefaultNodeLongitudesMatchSwissTrueNodeAndRemainOpposite()
    {
        var time = new Time("12:00 01/01/2000 +00:00", new GeoLocation("Greenwich", 0, 0));
        using SwissEph ephemeris = EphemerisFactory.New();
        double[] trueNode = new double[6];
        double[] meanNode = new double[6];
        string error = string.Empty;
        double ephemerisTime = Calculate.TimeToEphemerisTime(time);

        int trueFlags = ephemeris.swe_calc(ephemerisTime, SwissEph.SE_TRUE_NODE, SwissEph.SEFLG_SWIEPH, trueNode, ref error);
        Assert.IsTrue(trueFlags >= 0 && (trueFlags & SwissEph.SEFLG_SWIEPH) != 0, error);
        int meanFlags = ephemeris.swe_calc(ephemerisTime, SwissEph.SE_MEAN_NODE, SwissEph.SEFLG_SWIEPH, meanNode, ref error);
        Assert.IsTrue(meanFlags >= 0 && (meanFlags & SwissEph.SEFLG_SWIEPH) != 0, error);

        double rahu = Calculate.PlanetSayanaLongitude(PlanetName.Rahu, time).TotalDegrees;
        double ketu = Calculate.PlanetSayanaLongitude(PlanetName.Ketu, time).TotalDegrees;
        Assert.IsTrue(Math.Abs(trueNode[0] - meanNode[0]) > 0.5, "Fixture must distinguish node conventions.");
        Assert.AreEqual(trueNode[0], rahu, 1d / 3600);
        Assert.AreEqual((rahu + 180) % 360, ketu, 1d / 3600);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void LongitudeCachesDistinguishBothNodeModes(bool meanFirst)
    {
        bool originalMode = Calculate.UseMeanRahuKetu;
        var time = new Time("12:00 02/01/2000 +00:00", new GeoLocation("Greenwich", 0, 0));
        using SwissEph ephemeris = EphemerisFactory.New();
        try
        {
            foreach (bool mean in new[] { meanFirst, !meanFirst, meanFirst })
            {
                Calculate.UseMeanRahuKetu = mean;
                double[] positions = new double[6];
                string error = string.Empty;
                int flags = ephemeris.swe_calc(Calculate.TimeToEphemerisTime(time),
                    mean ? SwissEph.SE_MEAN_NODE : SwissEph.SE_TRUE_NODE,
                    SwissEph.SEFLG_SWIEPH, positions, ref error);
                Assert.IsTrue(flags >= 0 && (flags & SwissEph.SEFLG_SWIEPH) != 0, error);
                foreach (var planet in new[] { PlanetName.Rahu, PlanetName.Ketu })
                {
                    double expected = (positions[0] + (planet == PlanetName.Ketu ? 180 : 0)) % 360;
                    Assert.AreEqual(expected, Calculate.PlanetSayanaLongitude(planet, time).TotalDegrees, 1d / 3600);
                    Assert.AreEqual(expected, Calculate.PlanetEphemerisLongitude(planet, time).TotalDegrees, 1d / 3600);
                    double sidereal = (expected - Calculate.AyanamsaDegree(time).TotalDegrees + 360) % 360;
                    Assert.AreEqual(sidereal, Calculate.PlanetNirayanaLongitude(planet, time).TotalDegrees, 2d / 3600);
                }
            }
        }
        finally
        {
            Calculate.UseMeanRahuKetu = originalMode;
        }
    }
}
