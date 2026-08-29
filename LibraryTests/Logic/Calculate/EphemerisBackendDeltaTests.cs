using Microsoft.VisualStudio.TestTools.UnitTesting;
using SwissEphNet;

namespace VedAstro.Library.Tests;

[TestClass]
public class EphemerisBackendDeltaTests
{
    private static readonly (string Name, int Id)[] Bodies =
    {
        ("Sun", SwissEph.SE_SUN),
        ("Moon", SwissEph.SE_MOON),
        ("Mercury", SwissEph.SE_MERCURY),
        ("Venus", SwissEph.SE_VENUS),
        ("Mars", SwissEph.SE_MARS),
        ("Jupiter", SwissEph.SE_JUPITER),
        ("Saturn", SwissEph.SE_SATURN),
        ("True Node", SwissEph.SE_TRUE_NODE),
        ("Mean Node", SwissEph.SE_MEAN_NODE)
    };

    [TestMethod]
    public void SwissEphemerisFilesAreLoaded()
    {
        EphemerisFactory.ValidateEphemerisFiles();

        using SwissEph swissEph = EphemerisFactory.New();
        double[] positions = new double[6];
        string error = string.Empty;
        int resultFlags = swissEph.swe_calc_ut(
            2451545.0,
            SwissEph.SE_SUN,
            SwissEph.SEFLG_SWIEPH | SwissEph.SEFLG_SPEED,
            positions,
            ref error);

        Assert.AreNotEqual(0, resultFlags & SwissEph.SEFLG_SWIEPH);
        Assert.AreEqual(0, resultFlags & SwissEph.SEFLG_MOSEPH);
    }

    [TestMethod]
    public void UnsupportedEphemerisDateFailsInsteadOfFallingBackToMoshier()
    {
        using SwissEph swissEph = EphemerisFactory.New();
        double julianDay = swissEph.swe_julday(1700, 1, 1, 0, SwissEph.SE_GREG_CAL);
        double[] positions = new double[6];
        string error = string.Empty;

        InvalidOperationException exception = Assert.ThrowsException<InvalidOperationException>(() =>
            swissEph.swe_calc_ut(
                julianDay,
                SwissEph.SE_SUN,
                SwissEph.SEFLG_SWIEPH,
                positions,
                ref error));

        StringAssert.Contains(exception.Message, "not available for the requested date or body");
    }

    [TestMethod]
    public void EphemerisFilePathPreservesAsteroidSubdirectory()
    {
        string rootPath = Path.Combine(Path.GetTempPath(), "ephemeris");

        string filePath = EphemerisFactory.ResolveEphemerisFilePath(rootPath, "ast4/se04179.se1");

        Assert.AreEqual(
            Path.Combine(Path.GetFullPath(rootPath), "ast4", "se04179.se1"),
            filePath);
    }

    [TestMethod]
    public void EphemerisFilePathRejectsTraversalOutsideConfiguredDirectory()
    {
        string rootPath = Path.Combine(Path.GetTempPath(), "ephemeris");

        InvalidOperationException exception = Assert.ThrowsException<InvalidOperationException>(() =>
            EphemerisFactory.ResolveEphemerisFilePath(rootPath, "../secrets.se1"));

        StringAssert.Contains(exception.Message, "escapes the configured directory");
    }

    [TestMethod]
    public void MeasureMoshierVsSwissDeltas()
    {
        DateTime startDate = new(1900, 1, 1);
        DateTime endDate = new(2050, 1, 1);
        const int swissFlags = SwissEph.SEFLG_SWIEPH;
        const int moshierFlags = SwissEph.SEFLG_MOSEPH;

        using SwissEph swissEph = EphemerisFactory.New();
        using SwissEph moshierEph = new SwissEph();

        Console.WriteLine("| body | max delta (arcsec) | mean delta (arcsec) | max-delta date |");
        Console.WriteLine("|---|---:|---:|---|");

        foreach ((string bodyName, int bodyId) in Bodies)
        {
            double maximumDelta = 0;
            double deltaTotal = 0;
            int sampleCount = 0;
            DateTime maximumDeltaDate = startDate;

            for (DateTime date = startDate; date <= endDate; date = date.AddDays(10))
            {
                double julianDay = swissEph.swe_julday(
                    date.Year,
                    date.Month,
                    date.Day,
                    0,
                    SwissEph.SE_GREG_CAL);
                double[] swissPositions = new double[6];
                double[] moshierPositions = new double[6];
                string swissError = string.Empty;
                string moshierError = string.Empty;

                int swissResultFlags = swissEph.swe_calc_ut(
                    julianDay,
                    bodyId,
                    swissFlags,
                    swissPositions,
                    ref swissError);
                int moshierResultFlags = moshierEph.swe_calc_ut(
                    julianDay,
                    bodyId,
                    moshierFlags,
                    moshierPositions,
                    ref moshierError);

                Assert.AreNotEqual(
                    0,
                    swissResultFlags & SwissEph.SEFLG_SWIEPH,
                    $"Swiss Ephemeris files were not used for {bodyName} on {date:yyyy-MM-dd}: {swissError}");
                Assert.AreEqual(
                    0,
                    swissResultFlags & SwissEph.SEFLG_MOSEPH,
                    $"Swiss Ephemeris fell back to Moshier for {bodyName} on {date:yyyy-MM-dd}: {swissError}");
                Assert.AreNotEqual(
                    0,
                    moshierResultFlags & SwissEph.SEFLG_MOSEPH,
                    $"Moshier was not used for {bodyName} on {date:yyyy-MM-dd}: {moshierError}");

                double deltaDegrees = Math.Abs(swissPositions[0] - moshierPositions[0]);
                deltaDegrees = Math.Min(deltaDegrees, 360 - deltaDegrees);
                double deltaArcseconds = deltaDegrees * 3600;

                deltaTotal += deltaArcseconds;
                sampleCount++;
                if (deltaArcseconds > maximumDelta)
                {
                    maximumDelta = deltaArcseconds;
                    maximumDeltaDate = date;
                }
            }

            Console.WriteLine(
                $"| {bodyName} | {maximumDelta:F6} | {deltaTotal / sampleCount:F6} | {maximumDeltaDate:yyyy-MM-dd} |");
        }
    }
}
