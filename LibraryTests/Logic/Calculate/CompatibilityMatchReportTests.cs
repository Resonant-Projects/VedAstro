using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VedAstro.Library.Tests;

[TestClass]
public class CompatibilityMatchReportTests
{
    [TestInitialize]
    public void ResetCalculationSettings()
    {
        Calculate.Ayanamsa = (int)Ayanamsa.LAHIRI;
    }

    [DataTestMethod]
    [DataRow(
        "Pair 3 New York-Denver",
        "12:00 01/12/1985 -05:00", -74.006, 40.7128,
        "03:20 09/04/1987 -06:00", -104.9903, 39.7392)]
    [DataRow(
        "Pair 5 Bengaluru-Kiritimati",
        "00:05 29/02/2000 +05:30", 77.5946, 12.9716,
        "21:40 04/07/2001 +14:00", -157.4278, 1.8721)]
    public void MatchReportContainsEveryPredictionWithoutEmptyPlaceholders(
        string label,
        string maleTimestamp,
        double maleLongitude,
        double maleLatitude,
        string femaleTimestamp,
        double femaleLongitude,
        double femaleLatitude)
    {
        var maleTime = new Time(
            maleTimestamp,
            new GeoLocation($"{label} male", maleLongitude, maleLatitude));
        var femaleTime = new Time(
            femaleTimestamp,
            new GeoLocation($"{label} female", femaleLongitude, femaleLatitude));

        var report = Calculate.MatchReport(maleTime, femaleTime);
        var grahaMaitram = report.PredictionList
            .Single(prediction => prediction.Name == MatchPredictionName.GrahaMaitram);
        var lagnaAndHouse7Good = report.PredictionList
            .Single(prediction => prediction.Name == MatchPredictionName.LagnaAnd7thGood);
        var maleLord = Calculate.LordOfZodiacSign(
            Calculate.PlanetRasiD1Sign(PlanetName.Moon, maleTime).GetSignName());
        var femaleLord = Calculate.LordOfZodiacSign(
            Calculate.PlanetRasiD1Sign(PlanetName.Moon, femaleTime).GetSignName());
        var emptyPredictionIndexes = report.PredictionList
            .Select((prediction, index) => (prediction, index))
            .Where(item =>
                item.prediction.Name == MatchPredictionName.Empty ||
                item.prediction.Nature == EventNature.Empty)
            .Select(item => item.index)
            .ToArray();

        Assert.AreEqual(16, report.PredictionList.Count, label);
        Assert.AreEqual(
            0,
            emptyPredictionIndexes.Length,
            $"{label}; empty prediction indexes: {string.Join(", ", emptyPredictionIndexes)}");
        Assert.AreEqual(
            1,
            report.PredictionList.Count(prediction => prediction.Name == MatchPredictionName.GrahaMaitram),
            label);
        Assert.AreEqual(maleLord, femaleLord, $"{label}; expected equal Moon-sign lords");
        Assert.AreEqual(EventNature.Good, grahaMaitram.Nature, label);
        Assert.AreEqual(EventNature.Neutral, lagnaAndHouse7Good.Nature, label);
    }
}
