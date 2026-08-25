using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace VedAstro.Library;

/// <summary>
/// Bridges the public calculation names introduced on the modern source line
/// to the complete calculation engine recovered from upstream commit
/// e1e65d81450e3387115e50cfc6eaf1bdc48939bb.
/// </summary>
public partial class Calculate
{
    /// <summary>
    /// Determines combustion from the classical visibility limits documented by
    /// the modern public API (Moon 12°, Venus 9°, Jupiter 11°, Mercury 13°,
    /// Saturn 15°, and Mars 17° from the Sun).
    /// </summary>
    public static bool IsPlanetCombust(PlanetName planetName, Time time)
    {
        var limit = planetName.Name switch
        {
            PlanetName.PlanetNameEnum.Moon => 12d,
            PlanetName.PlanetNameEnum.Venus => 9d,
            PlanetName.PlanetNameEnum.Jupiter => 11d,
            PlanetName.PlanetNameEnum.Mercury => 13d,
            PlanetName.PlanetNameEnum.Saturn => 15d,
            PlanetName.PlanetNameEnum.Mars => 17d,
            _ => 0d
        };
        if (limit == 0) { return false; }

        var separation = Math.Abs(PlanetNirayanaLongitude(planetName, time).TotalDegrees -
                                  PlanetNirayanaLongitude(PlanetName.Sun, time).TotalDegrees);
        separation = Math.Min(separation, 360d - separation);
        return separation <= limit;
    }

    /// <summary>
    /// A major and sub-period in Jaimini Chara Dasha.
    /// </summary>
    public sealed record DashaPeriod(
        ZodiacName MajorSign,
        ZodiacName SubSign,
        Time MajorStart,
        Time MajorEnd,
        Time SubStart,
        Time SubEnd);

    /// <summary>
    /// Gets a planet's D1 (Rasi) sign and its longitude within that sign.
    /// </summary>
    public static ZodiacSign PlanetRasiD1Sign(PlanetName planetName, Time time) =>
        PlanetZodiacSign(planetName, time);

    /// <summary>
    /// Gets the sign represented by a planet in a Bhava Chalit chart.
    /// </summary>
    public static ZodiacSign PlanetZodiacSignBasedOnHouseLongitudes(PlanetName planetName, Time time) =>
        HouseZodiacSign(HousePlanetOccupiesBasedOnLongitudes(planetName, time), time);

    public static ZodiacSign HouseRasiSign(HouseName houseNumber, Time time) =>
        HouseZodiacSign(houseNumber, time);

    /// <summary>
    /// Gets the D1 signs occupied by all houses.
    /// </summary>
    public static Dictionary<HouseName, ZodiacSign> AllHouseZodiacSigns(Time time) =>
        AllHouseSign(time);

    public static Dictionary<HouseName, ZodiacSign> AllHouseRasiSigns(Time time) =>
        AllHouseSigns(time, HouseRasiSign);

    public static Dictionary<HouseName, ZodiacSign> AllHouseHoraSign(Time time) =>
        AllHouseSigns(time, HouseHoraD2Sign);

    public static Dictionary<HouseName, ZodiacSign> AllHouseDrekkanaSign(Time time) =>
        AllHouseSigns(time, HouseDrekkanaD3Sign);

    public static Dictionary<HouseName, ZodiacSign> AllHouseChaturthamsaSign(Time time) =>
        AllHouseSigns(time, HouseChaturthamshaD4Sign);

    public static Dictionary<HouseName, ZodiacSign> AllHouseSaptamshaSign(Time time) =>
        AllHouseSigns(time, HouseSaptamshaD7Sign);

    public static Dictionary<HouseName, ZodiacSign> AllHouseNavamshaSign(Time time) =>
        AllHouseSigns(time, HouseNavamshaD9Sign);

    public static Dictionary<HouseName, ZodiacSign> AllHouseDashamamshaSign(Time time) =>
        AllHouseSigns(time, HouseDashamamshaD10Sign);

    public static Dictionary<HouseName, ZodiacSign> AllHouseDwadashamshaSign(Time time) =>
        AllHouseSigns(time, HouseDwadashamshaD12Sign);

    public static Dictionary<HouseName, ZodiacSign> AllHouseShodashamshaSign(Time time) =>
        AllHouseSigns(time, HouseShodashamshaD16Sign);

    public static Dictionary<HouseName, ZodiacSign> AllHouseVimshamshaSign(Time time) =>
        AllHouseSigns(time, HouseVimshamshaD20Sign);

    public static Dictionary<HouseName, ZodiacSign> AllHouseChaturvimshamshaSign(Time time) =>
        AllHouseSigns(time, HouseChaturvimshamshaD24Sign);

    public static Dictionary<HouseName, ZodiacSign> AllHouseBhamshaSign(Time time) =>
        AllHouseSigns(time, HouseBhamshaD27Sign);

    public static Dictionary<HouseName, ZodiacSign> AllHouseTrimshamshaSign(Time time) =>
        AllHouseSigns(time, HouseTrimshamshaD30Sign);

    public static Dictionary<HouseName, ZodiacSign> AllHouseKhavedamshaSign(Time time) =>
        AllHouseSigns(time, HouseKhavedamshaD40Sign);

    public static Dictionary<HouseName, ZodiacSign> AllHouseAkshavedamshaSign(Time time) =>
        AllHouseSigns(time, HouseAkshavedamshaD45Sign);

    public static Dictionary<HouseName, ZodiacSign> AllHouseShashtyamshaSign(Time time) =>
        AllHouseSigns(time, HouseShashtyamshaD60Sign);

    public static ZodiacSign PlanetHoraD2Signs(PlanetName planetName, Time time) => PlanetHoraSigns(planetName, time);
    public static ZodiacSign PlanetDrekkanaD3Sign(PlanetName planetName, Time time) => PlanetDrekkanaSign(planetName, time);
    public static ZodiacSign PlanetChaturthamshaD4Sign(PlanetName planetName, Time time) => PlanetChaturthamshaSign(planetName, time);
    public static ZodiacSign PlanetSaptamshaD7Sign(PlanetName planetName, Time time) => VargaPlanetSign(planetName, time, Vargas.SaptamshaTable, 7);
    public static ZodiacSign PlanetNavamshaD9Sign(PlanetName planetName, Time time) => VargaPlanetSign(planetName, time, Vargas.NavamshaTable, 9);
    public static ZodiacSign PlanetDashamamshaD10Sign(PlanetName planetName, Time time) => VargaPlanetSign(planetName, time, Vargas.DashamamshaTable, 10);
    public static ZodiacSign PlanetDwadashamshaD12Sign(PlanetName planetName, Time time) => VargaPlanetSign(planetName, time, Vargas.DwadashamshaTable, 12);
    public static ZodiacSign PlanetShodashamshaD16Sign(PlanetName planetName, Time time) => VargaPlanetSign(planetName, time, Vargas.ShodashamshaTable, 16);
    public static ZodiacSign PlanetVimshamshaD20Sign(PlanetName planetName, Time time) => VargaPlanetSign(planetName, time, Vargas.VimshamshaTable, 20);
    public static ZodiacSign PlanetChaturvimshamshaD24Sign(PlanetName planetName, Time time) => VargaPlanetSign(planetName, time, Vargas.ChaturvimshamshaTable, 24);
    public static ZodiacSign PlanetBhamshaD27Sign(PlanetName planetName, Time time) => VargaPlanetSign(planetName, time, Vargas.BhamshaTable, 27);
    public static ZodiacSign PlanetTrimshamshaD30Sign(PlanetName planetName, Time time) => TrimshamshaSignName(PlanetZodiacSign(planetName, time));
    public static ZodiacSign PlanetKhavedamshaD40Sign(PlanetName planetName, Time time) => VargaPlanetSign(planetName, time, Vargas.KhavedamshaTable, 40);
    public static ZodiacSign PlanetAkshavedamshaD45Sign(PlanetName planetName, Time time) => VargaPlanetSign(planetName, time, Vargas.AkshavedamshaTable, 45);
    public static ZodiacSign PlanetShashtyamshaD60Sign(PlanetName planetName, Time time) => VargaPlanetSign(planetName, time, Vargas.ShashtyamshaTable, 60);

    public static ZodiacSign SaptamshaSignName(ZodiacSign sign) => Vargas.VargasCoreCalculator(sign, Vargas.SaptamshaTable, 7);
    public static ZodiacSign NavamshaSignName(ZodiacSign sign) => Vargas.VargasCoreCalculator(sign, Vargas.NavamshaTable, 9);
    public static ZodiacSign DashamamshaSignName(ZodiacSign sign) => Vargas.VargasCoreCalculator(sign, Vargas.DashamamshaTable, 10);
    public static ZodiacSign DwadashamshaSignName(ZodiacSign sign) => Vargas.VargasCoreCalculator(sign, Vargas.DwadashamshaTable, 12);
    public static ZodiacSign ShodashamshaSignName(ZodiacSign sign) => Vargas.VargasCoreCalculator(sign, Vargas.ShodashamshaTable, 16);
    public static ZodiacSign VimshamshaSignName(ZodiacSign sign) => Vargas.VargasCoreCalculator(sign, Vargas.VimshamshaTable, 20);
    public static ZodiacSign ChaturvimshamshaSignName(ZodiacSign sign) => Vargas.VargasCoreCalculator(sign, Vargas.ChaturvimshamshaTable, 24);
    public static ZodiacSign BhamshaSignName(ZodiacSign sign) => Vargas.VargasCoreCalculator(sign, Vargas.BhamshaTable, 27);
    public static ZodiacSign KhavedamshaSignName(ZodiacSign sign) => Vargas.VargasCoreCalculator(sign, Vargas.KhavedamshaTable, 40);
    public static ZodiacSign AkshavedamshaSignName(ZodiacSign sign) => Vargas.VargasCoreCalculator(sign, Vargas.AkshavedamshaTable, 45);
    public static ZodiacSign ShashtyamshaSignName(ZodiacSign sign) => Vargas.VargasCoreCalculator(sign, Vargas.ShashtyamshaTable, 60);

    public static ZodiacSign TrimshamshaSignName(ZodiacSign sign)
    {
        var degrees = sign.GetDegreesInSign().TotalDegrees;
        var odd = IsOddSign(sign.GetSignName());
        var segments = odd
            ? new[] { (0d, 5d, ZodiacName.Aries), (5d, 10d, ZodiacName.Aquarius), (10d, 18d, ZodiacName.Sagittarius), (18d, 25d, ZodiacName.Gemini), (25d, 30d, ZodiacName.Libra) }
            : new[] { (0d, 5d, ZodiacName.Taurus), (5d, 12d, ZodiacName.Virgo), (12d, 20d, ZodiacName.Pisces), (20d, 25d, ZodiacName.Capricorn), (25d, 30d, ZodiacName.Scorpio) };

        var segment = segments.First(item =>
            degrees >= item.Item1 && (degrees < item.Item2 || (degrees == 30 && item.Item2 == 30)));
        var degreesInDivision = (degrees - segment.Item1) * 30d / (segment.Item2 - segment.Item1);
        return new ZodiacSign(segment.Item3, Angle.FromDegrees(degreesInDivision));
    }

    public static ZodiacSign SaptamshaSignAtLongitude(Angle longitude) => SaptamshaSignName(ZodiacSignAtLongitude(longitude));
    public static ZodiacSign NavamshaSignAtLongitude(Angle longitude) => NavamshaSignName(ZodiacSignAtLongitude(longitude));
    public static ZodiacSign DashamamshaSignAtLongitude(Angle longitude) => DashamamshaSignName(ZodiacSignAtLongitude(longitude));
    public static ZodiacSign DwadashamshaSignAtLongitude(Angle longitude) => DwadashamshaSignName(ZodiacSignAtLongitude(longitude));
    public static ZodiacSign ShodashamshaSignAtLongitude(Angle longitude) => ShodashamshaSignName(ZodiacSignAtLongitude(longitude));
    public static ZodiacSign VimshamshaSignAtLongitude(Angle longitude) => VimshamshaSignName(ZodiacSignAtLongitude(longitude));
    public static ZodiacSign ChaturvimshamshaSignAtLongitude(Angle longitude) => ChaturvimshamshaSignName(ZodiacSignAtLongitude(longitude));
    public static ZodiacSign BhamshaSignAtLongitude(Angle longitude) => BhamshaSignName(ZodiacSignAtLongitude(longitude));
    public static ZodiacSign TrimshamshaSignAtLongitude(Angle longitude) => TrimshamshaSignName(ZodiacSignAtLongitude(longitude));
    public static ZodiacSign KhavedamshaSignAtLongitude(Angle longitude) => KhavedamshaSignName(ZodiacSignAtLongitude(longitude));
    public static ZodiacSign AkshavedamshaSignAtLongitude(Angle longitude) => AkshavedamshaSignName(ZodiacSignAtLongitude(longitude));
    public static ZodiacSign ShashtyamshaSignAtLongitude(Angle longitude) => ShashtyamshaSignName(ZodiacSignAtLongitude(longitude));

    public static ZodiacSign HouseHoraD2Sign(HouseName house, Time time) => VargaHouseSign(house, time, HoraSignName);
    public static ZodiacSign HouseDrekkanaD3Sign(HouseName house, Time time) => VargaHouseSign(house, time, DrekkanaSignName);
    public static ZodiacSign HouseChaturthamshaD4Sign(HouseName house, Time time) => VargaHouseSign(house, time, ChaturthamshaSignName);
    public static ZodiacSign HouseSaptamshaD7Sign(HouseName house, Time time) => VargaHouseSign(house, time, SaptamshaSignName);
    public static ZodiacSign HouseNavamshaD9Sign(HouseName house, Time time) => VargaHouseSign(house, time, NavamshaSignName);
    public static ZodiacSign HouseDashamamshaD10Sign(HouseName house, Time time) => VargaHouseSign(house, time, DashamamshaSignName);
    public static ZodiacSign HouseDwadashamshaD12Sign(HouseName house, Time time) => VargaHouseSign(house, time, DwadashamshaSignName);
    public static ZodiacSign HouseShodashamshaD16Sign(HouseName house, Time time) => VargaHouseSign(house, time, ShodashamshaSignName);
    public static ZodiacSign HouseVimshamshaD20Sign(HouseName house, Time time) => VargaHouseSign(house, time, VimshamshaSignName);
    public static ZodiacSign HouseChaturvimshamshaD24Sign(HouseName house, Time time) => VargaHouseSign(house, time, ChaturvimshamshaSignName);
    public static ZodiacSign HouseBhamshaD27Sign(HouseName house, Time time) => VargaHouseSign(house, time, BhamshaSignName);
    public static ZodiacSign HouseTrimshamshaD30Sign(HouseName house, Time time) => VargaHouseSign(house, time, TrimshamshaSignName);
    public static ZodiacSign HouseKhavedamshaD40Sign(HouseName house, Time time) => VargaHouseSign(house, time, KhavedamshaSignName);
    public static ZodiacSign HouseAkshavedamshaD45Sign(HouseName house, Time time) => VargaHouseSign(house, time, AkshavedamshaSignName);
    public static ZodiacSign HouseShashtyamshaD60Sign(HouseName house, Time time) => VargaHouseSign(house, time, ShashtyamshaSignName);

    public static Dictionary<PlanetName, ZodiacSign> AllPlanetSignsBasedOnHouseLongitudes(Time time) =>
        PlanetName.All9Planets.ToDictionary(planet => planet, planet => PlanetZodiacSignBasedOnHouseLongitudes(planet, time));

    public static Dictionary<PlanetName, ZodiacSign> AllPlanetRasiSigns(Time time) => AllPlanetSignsBy(time, PlanetRasiD1Sign);
    public static Dictionary<PlanetName, ZodiacSign> AllPlanetSaptamshaSign(Time time) => AllPlanetSignsBy(time, PlanetSaptamshaD7Sign);
    public static Dictionary<PlanetName, ZodiacSign> AllPlanetNavamshaSign(Time time) => AllPlanetSignsBy(time, PlanetNavamshaD9Sign);
    public static Dictionary<PlanetName, ZodiacSign> AllPlanetDashamamshaSign(Time time) => AllPlanetSignsBy(time, PlanetDashamamshaD10Sign);
    public static Dictionary<PlanetName, ZodiacSign> AllPlanetDwadashamshaSign(Time time) => AllPlanetSignsBy(time, PlanetDwadashamshaD12Sign);
    public static Dictionary<PlanetName, ZodiacSign> AllPlanetShodashamshaSign(Time time) => AllPlanetSignsBy(time, PlanetShodashamshaD16Sign);
    public static Dictionary<PlanetName, ZodiacSign> AllPlanetVimshamshaSign(Time time) => AllPlanetSignsBy(time, PlanetVimshamshaD20Sign);
    public static Dictionary<PlanetName, ZodiacSign> AllPlanetChaturvimshamshaSign(Time time) => AllPlanetSignsBy(time, PlanetChaturvimshamshaD24Sign);
    public static Dictionary<PlanetName, ZodiacSign> AllPlanetBhamshaSign(Time time) => AllPlanetSignsBy(time, PlanetBhamshaD27Sign);
    public static Dictionary<PlanetName, ZodiacSign> AllPlanetTrimshamshaSign(Time time) => AllPlanetSignsBy(time, PlanetTrimshamshaD30Sign);
    public static Dictionary<PlanetName, ZodiacSign> AllPlanetKhavedamshaSign(Time time) => AllPlanetSignsBy(time, PlanetKhavedamshaD40Sign);
    public static Dictionary<PlanetName, ZodiacSign> AllPlanetAkshavedamshaSign(Time time) => AllPlanetSignsBy(time, PlanetAkshavedamshaD45Sign);
    public static Dictionary<PlanetName, ZodiacSign> AllPlanetShashtyamshaSign(Time time) => AllPlanetSignsBy(time, PlanetShashtyamshaD60Sign);

    private static ZodiacSign VargaPlanetSign(PlanetName planet, Time time,
        Dictionary<ZodiacName, Dictionary<DegreeRange, ZodiacName>> table, int division) =>
        Vargas.VargasCoreCalculator(PlanetZodiacSign(planet, time), table, division);

    private static ZodiacSign VargaHouseSign(HouseName house, Time time, Func<ZodiacSign, ZodiacSign> converter) =>
        converter(HouseZodiacSign(house, time));

    private static Dictionary<PlanetName, ZodiacSign> AllPlanetSignsBy(Time time,
        Func<PlanetName, Time, ZodiacSign> calculator) =>
        PlanetName.All9Planets.ToDictionary(planet => planet, planet => calculator(planet, time));

    private static Dictionary<HouseName, ZodiacSign> AllHouseSigns(Time time,
        Func<HouseName, Time, ZodiacSign> calculator) =>
        House.AllHouses.ToDictionary(house => house, house => calculator(house, time));

    /// <summary>
    /// Converts geographic longitude into its local-mean-time offset.
    /// </summary>
    public static TimeSpan LongitudeToLMTOffset(double longitudeDeg)
    {
        if (longitudeDeg is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitudeDeg), longitudeDeg,
                "Longitude must be between -180 and 180 degrees.");
        }

        // DateTimeOffset requires offsets in whole minutes. One degree of
        // longitude is four minutes of local mean time.
        return TimeSpan.FromMinutes(Math.Round(longitudeDeg * 4.0));
    }

    /// <summary>
    /// Converts local mean time into standard time at the requested offset.
    /// </summary>
    public static DateTimeOffset LmtToStd(LocalMeanTime localMeanTime, TimeSpan standardOffset)
    {
        ArgumentNullException.ThrowIfNull(localMeanTime);
        var localOffset = LongitudeToLMTOffset(localMeanTime.Longitude);
        return new DateTimeOffset(localMeanTime.Date, localOffset).ToOffset(standardOffset);
    }

    /// <summary>
    /// Converts a time to Julian Day in Universal Time.
    /// </summary>
    public static double TimeToJulianUniversalTime(Time time) => TimeToJulianDay(time);

    /// <summary>
    /// Converts a time to Julian Ephemeris Day, including ΔT.
    /// </summary>
    public static double TimeToJulianEphemerisTime(Time time) => TimeToEphemerisTime(time);

    /// <summary>
    /// Calculates the active Jaimini Chara Dasha major and sub-period.
    /// </summary>
    public static DashaPeriod GetCharaDasaAtTime(Time birthTime, Time checkTime)
    {
        ArgumentNullException.ThrowIfNull(birthTime);
        ArgumentNullException.ThrowIfNull(checkTime);
        if (checkTime < birthTime)
        {
            throw new ArgumentOutOfRangeException(nameof(checkTime), "Check time cannot precede birth time.");
        }

        var lagna = HouseRasiSign(HouseName.House1, birthTime).GetSignName();
        var majorSigns = SignsInDirection(lagna, IsOddSign(lagna));
        var majorStart = birthTime;

        // Chara Dasha repeats after its twelve sign periods. Iterating also keeps
        // the method useful for check times beyond the first nominal cycle.
        for (var cycle = 0; cycle < 20; cycle++)
        {
            foreach (var majorSign in majorSigns)
            {
                var years = CharaDashaYears(majorSign, birthTime);
                var majorEnd = majorStart.AddYears(years);
                if (checkTime < majorEnd)
                {
                    var subSigns = SignsInDirection(majorSign, IsOddSign(majorSign));
                    var majorHours = majorEnd.Subtract(majorStart).TotalHours;
                    var subHours = majorHours / 12d;

                    for (var index = 0; index < subSigns.Count; index++)
                    {
                        var subStart = majorStart.AddHours(subHours * index);
                        var subEnd = majorStart.AddHours(subHours * (index + 1));
                        if (checkTime < subEnd || index == subSigns.Count - 1)
                        {
                            return new DashaPeriod(majorSign, subSigns[index], majorStart, majorEnd, subStart, subEnd);
                        }
                    }
                }

                majorStart = majorEnd;
            }
        }

        throw new InvalidOperationException("Chara Dasha period was not found within twenty cycles.");

        static List<ZodiacName> SignsInDirection(ZodiacName start, bool forward)
        {
            var signs = new List<ZodiacName>(12);
            var current = (int)start;
            for (var index = 0; index < 12; index++)
            {
                var normalized = ((current - 1) % 12 + 12) % 12 + 1;
                signs.Add((ZodiacName)normalized);
                current += forward ? 1 : -1;
            }
            return signs;
        }

        static int CharaDashaYears(ZodiacName sign, Time time)
        {
            var lord = LordOfZodiacSign(sign);
            var lordSign = PlanetRasiD1Sign(lord, time).GetSignName();
            if (lordSign == sign) return 12;

            var forward = IsOddSign(sign);
            var distance = forward
                ? ((int)lordSign - (int)sign + 12) % 12
                : ((int)sign - (int)lordSign + 12) % 12;
            return Math.Max(1, distance);
        }
    }

    /// <summary>
    /// Expands the event-chart presets used by the website into an explicit time range.
    /// Supported forms include 3days, 2weeks, 6months, 3years, age10to35,
    /// 1990-2000, and fulllife.
    /// </summary>
    public static TimeRange AutoCalculateTimeRange(Time inputBirthTime, string timePreset, TimeSpan outputTimezone)
    {
        ArgumentNullException.ThrowIfNull(inputBirthTime);
        if (string.IsNullOrWhiteSpace(timePreset)) return TimeRange.Empty;

        var preset = Regex.Replace(timePreset.Trim().ToLowerInvariant(), @"\s+", string.Empty);
        var location = inputBirthTime.GetGeoLocation();
        var birth = inputBirthTime.GetStdDateTimeOffset().ToOffset(outputTimezone);
        var birthAtMidnight = new Time(new DateTimeOffset(birth.Year, birth.Month, birth.Day, 0, 0, 0, outputTimezone), location);
        var now = DateTimeOffset.Now.ToOffset(outputTimezone);

        var yearRange = Regex.Match(preset, @"^(?<start>\d{4})-(?<end>\d{4})$");
        if (yearRange.Success)
        {
            if (!int.TryParse(yearRange.Groups["start"].Value, out var startYear) ||
                !int.TryParse(yearRange.Groups["end"].Value, out var endYear) ||
                startYear < 1 || endYear > 9999 || endYear < startYear)
            {
                return TimeRange.Empty;
            }

            return new TimeRange(
                new Time(new DateTimeOffset(startYear, 1, 1, 0, 0, 0, outputTimezone), location),
                new Time(new DateTimeOffset(endYear, 12, 31, 0, 0, 0, outputTimezone), location));
        }

        var ageRange = Regex.Match(preset, @"^age(?<start>\d+)to(?<end>\d+)$");
        if (ageRange.Success)
        {
            const int maximumSupportedAge = 150;
            if (!int.TryParse(ageRange.Groups["start"].Value, out var startAge) ||
                !int.TryParse(ageRange.Groups["end"].Value, out var endAge) ||
                startAge < 0 || endAge > maximumSupportedAge || endAge < startAge ||
                birthAtMidnight.GetStdDateTimeOffset().Year + endAge > 9999)
            {
                return TimeRange.Empty;
            }

            // In the website's user-facing convention, "age 1" begins at birth.
            if (startAge == 1) startAge = 0;
            return new TimeRange(birthAtMidnight.AddYears(startAge), birthAtMidnight.AddYears(endAge));
        }

        if (preset == "fulllife")
        {
            return new TimeRange(birthAtMidnight, birthAtMidnight.AddYears(75));
        }

        var relative = Regex.Match(preset, @"^(?<count>\d+)?(?<unit>hour|hours|today|day|days|week|weeks|month|months|year|years|decade|decades)$");
        if (!relative.Success) return TimeRange.Empty;

        var count = relative.Groups["count"].Success ? int.Parse(relative.Groups["count"].Value) : 1;
        count = Math.Max(1, count);
        var unit = relative.Groups["unit"].Value;
        var currentTime = new Time(now, location);

        return unit switch
        {
            "hour" or "hours" => new TimeRange(new Time(now.AddHours(-1), location), new Time(now.AddHours(count), location)),
            "today" or "day" or "days" => new TimeRange(StartOfDay(now.AddDays(-1)), currentTime.AddHours(Tools.DaysToHours(count))),
            "week" or "weeks" => new TimeRange(StartOfDay(now.AddDays(-1)), currentTime.AddHours(Tools.DaysToHours(count * 7))),
            "month" or "months" => new TimeRange(StartOfDay(now.AddDays(-7)), currentTime.AddHours(Tools.DaysToHours(count * 30))),
            "year" or "years" => new TimeRange(StartOfDay(now.AddDays(-182)), currentTime.AddHours(Tools.DaysToHours(count * 365))),
            "decade" or "decades" => new TimeRange(StartOfDay(now.AddDays(-365)), currentTime.AddHours(Tools.DaysToHours(count * 3652))),
            _ => TimeRange.Empty
        };

        Time StartOfDay(DateTimeOffset value) =>
            new(new DateTimeOffset(value.Year, value.Month, value.Day, 0, 0, 0, outputTimezone), location);
    }
}
