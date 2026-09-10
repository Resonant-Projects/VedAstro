using Microsoft.VisualStudio.TestTools.UnitTesting;
using SwissEphNet;

namespace VedAstro.Library.Tests;

[TestClass]
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

        ephemeris.swe_calc(ephemerisTime, SwissEph.SE_TRUE_NODE, SwissEph.SEFLG_SWIEPH, trueNode, ref error);
        ephemeris.swe_calc(ephemerisTime, SwissEph.SE_MEAN_NODE, SwissEph.SEFLG_SWIEPH, meanNode, ref error);

        double rahu = Calculate.PlanetSayanaLongitude(PlanetName.Rahu, time).TotalDegrees;
        double ketu = Calculate.PlanetSayanaLongitude(PlanetName.Ketu, time).TotalDegrees;
        Assert.IsTrue(Math.Abs(trueNode[0] - meanNode[0]) > 0.5, "Fixture must distinguish node conventions.");
        Assert.AreEqual(trueNode[0], rahu, 1d / 3600);
        Assert.AreEqual((rahu + 180) % 360, ketu, 1d / 3600);
    }
}
