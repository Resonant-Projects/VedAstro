using System.Linq;
using static VedAstro.Library.HouseName;
using static VedAstro.Library.PlanetName;

namespace VedAstro.Library;

/// <summary>
/// Ashtakavarga rules added to the public catalog after the original
/// CalculateHoroscope source stopped being published. Conditions are restored
/// from Library/XMLData/HoroscopeDataList.xml and the accompanying upstream tests.
/// </summary>
public partial class CalculateHoroscope
{
    [HoroscopeCalculator(HoroscopeName.SunAshtakavargaYoga2)]
    public static CalculatorResult SunAshtakavargaYoga2(Time time)
    {
        var bindus = Bindu(Sun, Sun, time);
        var dignified = Calculate.IsPlanetInOwnSign(Sun, time) || Calculate.IsPlanetExaltedSign(Sun, time);
        return Result(bindus is 3 or 4 && !dignified, time, Sun);
    }

    [HoroscopeCalculator(HoroscopeName.SunAshtakavargaYoga5)]
    public static CalculatorResult SunAshtakavargaYoga5(Time time) =>
        Result(Bindu(Sun, Sun, time) > 5 && InKendraOrTrikona(Sun, time), time, Sun);

    [HoroscopeCalculator(HoroscopeName.SunAshtakavargaYoga6)]
    public static CalculatorResult SunAshtakavargaYoga6(Time time) =>
        Result(Bindu(Sun, Sun, time) is >= 3 and <= 5 && InKendraOrTrikona(Sun, time) &&
               Calculate.IsPlanetInFriendSign(Sun, time), time, Sun);

    [HoroscopeCalculator(HoroscopeName.SunAshtakavargaYoga7)]
    public static CalculatorResult SunAshtakavargaYoga7(Time time) =>
        Result(HouseOf(Sun, time) == House9 && Bindu(Sun, Sun, time) == 3 &&
               Calculate.IsPlanetConjunctWithPlanet(Sun, Rahu, time), time, Sun, Rahu);

    [HoroscopeCalculator(HoroscopeName.SunAshtakavargaYoga8)]
    public static CalculatorResult SunAshtakavargaYoga8(Time time) =>
        Result(HouseOf(Sun, time) == House5 && Bindu(Sun, Sun, time) == 3 &&
               Conjunct(Sun, time, Moon, Mars, Saturn), time, Sun, Moon, Mars, Saturn);

    [HoroscopeCalculator(HoroscopeName.SunAshtakavargaYoga9)]
    public static CalculatorResult SunAshtakavargaYoga9(Time time)
    {
        var maleficFromNinth = Calculate.PlanetsInHouseBasedOnSign(House9, time)
            .Where(planet => Calculate.IsPlanetMalefic(planet, time))
            .Any(planet => Calculate.IsPlanetAspectedByPlanet(Sun, planet, time));
        return Result(HouseOf(Sun, time) == House3 && Bindu(Sun, Sun, time) is 3 or 4 && maleficFromNinth,
            time, Sun);
    }

    [HoroscopeCalculator(HoroscopeName.SunAshtakavargaYoga11)]
    public static CalculatorResult SunAshtakavargaYoga11(Time time) =>
        Result(Bindu(Sun, Sun, time) >= 5 && InKendraOrTrikona(Sun, time), time, Sun);

    [HoroscopeCalculator(HoroscopeName.MoonAshtakavargaYoga4)]
    public static CalculatorResult MoonAshtakavargaYoga4(Time time) =>
        Result(Bindu(Moon, Moon, time) >= 7 && InKendra(Moon, time, includeLagna: true), time, Moon);

    [HoroscopeCalculator(HoroscopeName.MoonAshtakavargaYoga6)]
    public static CalculatorResult MoonAshtakavargaYoga6(Time time) =>
        Result(HouseOf(Rahu, time) == House2 && HouseOf(Moon, time) is House7 or House8 &&
               Bindu(Moon, Moon, time) is >= 1 and <= 3, time, Moon, Rahu);

    [HoroscopeCalculator(HoroscopeName.MoonAshtakavargaYoga7)]
    public static CalculatorResult MoonAshtakavargaYoga7(Time time)
    {
        var fromMoon = Calculate.SignDistanceFromPlanetToPlanet(Moon, Mars, time);
        return Result(fromMoon is 4 or 8 && Bindu(Moon, Mars, time) < Bindu(Moon, Moon, time),
            time, Moon, Mars);
    }

    [HoroscopeCalculator(HoroscopeName.MoonAshtakavargaYoga8)]
    public static CalculatorResult MoonAshtakavargaYoga8(Time time) =>
        Result((InKendra(Moon, time, includeLagna: true) || HouseOf(Moon, time) == House12) &&
               Bindu(Moon, Moon, time) is >= 1 and <= 3 &&
               Calculate.IsMaleficPlanetInHouse(House4, time), time, Moon);

    [HoroscopeCalculator(HoroscopeName.MoonAshtakavargaYoga9)]
    public static CalculatorResult MoonAshtakavargaYoga9(Time time) =>
        Result(HouseOf(Moon, time) == House1 && !Calculate.IsWaxingMoon(time) &&
               Bindu(Moon, Moon, time) <= 3, time, Moon);

    [HoroscopeCalculator(HoroscopeName.MoonAshtakavargaYoga10)]
    public static CalculatorResult MoonAshtakavargaYoga10(Time time) =>
        Result(HouseOf(Moon, time) == House1 && Bindu(Moon, Moon, time) <= 3 &&
               Calculate.IsMaleficPlanetInHouse(House4, time), time, Moon);

    [HoroscopeCalculator(HoroscopeName.MarsAshtakavargaYoga2)]
    public static CalculatorResult MarsAshtakavargaYoga2(Time time) =>
        Result(Bindu(Mars, Mars, time) == 8 && InHouse(Mars, time, House1, House4, House9, House10) &&
               (Calculate.IsPlanetExaltedSign(Mars, time) || Calculate.IsPlanetInOwnSign(Mars, time)), time, Mars);

    [HoroscopeCalculator(HoroscopeName.MarsAshtakavargaYoga3)]
    public static CalculatorResult MarsAshtakavargaYoga3(Time time)
    {
        var lagna = Calculate.HouseSignName(House1, time);
        var suitableLagna = lagna is ZodiacName.Sagittarius or ZodiacName.Leo or ZodiacName.Aries or
            ZodiacName.Cancer or ZodiacName.Capricorn;
        return Result(suitableLagna && HouseOf(Mars, time) == House1 && Bindu(Mars, Mars, time) == 4,
            time, Mars);
    }

    [HoroscopeCalculator(HoroscopeName.MarsAshtakavargaYoga4)]
    public static CalculatorResult MarsAshtakavargaYoga4(Time time) =>
        Result(Bindu(Mars, Mars, time) == 8 && Calculate.IsPlanetStrongInShadbala(Mars, time), time, Mars);

    [HoroscopeCalculator(HoroscopeName.MarsAshtakavargaYoga5)]
    public static CalculatorResult MarsAshtakavargaYoga5(Time time) =>
        Result(Bindu(Mars, Mars, time) == 8 && InHouse(Mars, time, House1, House2, House10), time, Mars);

    [HoroscopeCalculator(HoroscopeName.MarsAshtakavargaYoga6)]
    public static CalculatorResult MarsAshtakavargaYoga6(Time time) =>
        Result(Bindu(Mars, Mars, time) == 8 && InHouse(Mars, time, House1, House2, House10) &&
               (Calculate.IsPlanetExaltedSign(Mars, time) || Calculate.IsPlanetInOwnSign(Mars, time)), time, Mars);

    [HoroscopeCalculator(HoroscopeName.MarsAshtakavargaYoga8)]
    public static CalculatorResult MarsAshtakavargaYoga8(Time time) =>
        Result(Calculate.LordOfHouse(House2, time) == Mars && HouseOf(Mars, time) == House6 &&
               Bindu(Mars, Mars, time) == 6, time, Mars);

    [HoroscopeCalculator(HoroscopeName.MarsAshtakavargaYoga9)]
    public static CalculatorResult MarsAshtakavargaYoga9(Time time)
    {
        var marsIsRelevantLord = Calculate.LordOfHouse(House1, time) == Mars ||
                                 Calculate.LordOfHouse(House8, time) == Mars;
        var marsFromMoon = Calculate.SignDistanceFromPlanetToPlanet(Moon, Mars, time);
        var suitablePosition = InHouse(Mars, time, House1, House10) || marsFromMoon is 1 or 9 or 10;
        var joinedWeakPlanet = PlanetName.All9Planets.Except(new[] { Mars })
            .Any(planet => Calculate.IsPlanetConjunctWithPlanet(Mars, planet, time) &&
                           IsDebilitatedOrInimical(planet, time));
        return Result(marsIsRelevantLord && suitablePosition && Bindu(Mars, Mars, time) <= 3 && joinedWeakPlanet,
            time, Mars);
    }

    [HoroscopeCalculator(HoroscopeName.MarsAshtakavargaYoga10A)]
    public static CalculatorResult MarsAshtakavargaYoga10A(Time time) =>
        Result(MarsYoga10Base(time), time, Mars, Moon);

    [HoroscopeCalculator(HoroscopeName.MarsAshtakavargaYoga10B)]
    public static CalculatorResult MarsAshtakavargaYoga10B(Time time) =>
        Result(MarsYoga10Base(time) && Calculate.IsPlanetStrongInShadbala(Mars, time), time, Mars, Moon);

    [HoroscopeCalculator(HoroscopeName.MarsAshtakavargaYoga11)]
    public static CalculatorResult MarsAshtakavargaYoga11(Time time) =>
        Result(Bindu(Mars, Mars, time) == 4 && InKendraOrTrikona(Mars, time), time, Mars);

    [HoroscopeCalculator(HoroscopeName.MarsAshtakavargaYoga12A)]
    public static CalculatorResult MarsAshtakavargaYoga12A(Time time) =>
        Result(MarsYoga12(time, Calculate.IsEvenSign), time, Mars, Saturn);

    [HoroscopeCalculator(HoroscopeName.MarsAshtakavargaYoga12B)]
    public static CalculatorResult MarsAshtakavargaYoga12B(Time time) =>
        Result(MarsYoga12(time, Calculate.IsOddSign), time, Mars, Saturn);

    [HoroscopeCalculator(HoroscopeName.MarsAshtakavargaYoga13A)]
    public static CalculatorResult MarsAshtakavargaYoga13A(Time time)
    {
        var lagna = Calculate.HouseSignName(House1, time);
        var saturnSign = Calculate.PlanetRasiD1Sign(Saturn, time).GetSignName();
        var fewBindus = Bindu(Mars, Saturn, time) is >= 1 and <= 3;
        return Result(fewBindus && ((Calculate.IsMovableSign(lagna) && Calculate.IsCommonSign(saturnSign)) ||
                                   (Calculate.IsCommonSign(lagna) && Calculate.IsMovableSign(saturnSign))),
            time, Mars, Saturn);
    }

    [HoroscopeCalculator(HoroscopeName.MarsAshtakavargaYoga13B)]
    public static CalculatorResult MarsAshtakavargaYoga13B(Time time)
    {
        var lagnaFixed = Calculate.IsFixedSign(Calculate.HouseSignName(House1, time));
        var fewBindus = Bindu(Mars, Saturn, time) is >= 1 and <= 3 ||
                         Bindu(Mars, Mars, time) is >= 1 and <= 3;
        return Result(lagnaFixed && fewBindus, time, Mars, Saturn);
    }

    [HoroscopeCalculator(HoroscopeName.MarsAshtakavargaYoga14)]
    public static CalculatorResult MarsAshtakavargaYoga14(Time time)
    {
        var saturnSign = Calculate.PlanetRasiD1Sign(Saturn, time).GetSignName();
        return Result(Calculate.IsMovableSign(Calculate.HouseSignName(House1, time)) &&
                      Bindu(Mars, Saturn, time) == 5 &&
                      (Calculate.IsMovableSign(saturnSign) || Calculate.IsFixedSign(saturnSign)), time, Mars, Saturn);
    }

    [HoroscopeCalculator(HoroscopeName.MarsAshtakavargaYoga15)]
    public static CalculatorResult MarsAshtakavargaYoga15(Time time)
    {
        var thirdLord = Calculate.LordOfHouse(House3, time);
        var thirdLordSign = Calculate.PlanetRasiD1Sign(thirdLord, time).GetSignName();
        return Result(Calculate.IsFixedSign(Calculate.HouseSignName(House1, time)) &&
                      Calculate.IsCommonSign(thirdLordSign) && Bindu(Mars, thirdLord, time) == 5,
            time, Mars, thirdLord);
    }

    [HoroscopeCalculator(HoroscopeName.MarsAshtakavargaYoga16)]
    public static CalculatorResult MarsAshtakavargaYoga16(Time time) =>
        Result(HouseOf(Mars, time) == House3 && Bindu(Mars, Mars, time) >= 5 &&
               AssociatedWithOrAspectedByBenefic(Mars, time), time, Mars);

    [HoroscopeCalculator(HoroscopeName.MarsAshtakavargaYoga17)]
    public static CalculatorResult MarsAshtakavargaYoga17(Time time)
    {
        var marsBindus = Bindu(Mars, Mars, time);
        var saturnBindus = Bindu(Mars, Saturn, time);
        var seventhLord = Calculate.LordOfHouse(House7, time);
        var clauseA = marsBindus == 0 && Calculate.IsPlanetConjunctWithPlanet(Mars, seventhLord, time) &&
                      (Calculate.IsPlanetDebilitated(Mars, time) || Calculate.IsPlanetCombust(Mars, time) ||
                       AssociatedWithOrAspectedByMalefic(Mars, time));
        var clauseB = HouseOf(Mars, time) == House1 && HouseOf(Saturn, time) == House1 &&
                      marsBindus is >= 1 and <= 3 && saturnBindus is >= 1 and <= 3;
        var eighthApart = Calculate.SignDistanceFromPlanetToPlanet(Saturn, Mars, time) == 8 ||
                          Calculate.SignDistanceFromPlanetToPlanet(Mars, Saturn, time) == 8;
        var clauseC = eighthApart && marsBindus is >= 1 and <= 3 && saturnBindus is >= 1 and <= 3;
        var clauseD = Calculate.SignDistanceFromPlanetToPlanet(Saturn, Mars, time) == 8 &&
                      Calculate.IsMaleficPlanetInHouse(House3, time) &&
                      marsBindus is >= 1 and <= 3;
        return Result(clauseA || clauseB || clauseC || clauseD, time, Mars, Saturn);
    }

    [HoroscopeCalculator(HoroscopeName.MarsAshtakavargaYoga19)]
    public static CalculatorResult MarsAshtakavargaYoga19(Time time) =>
        Result(HouseOf(Mars, time) == House3 && Bindu(Mars, Mars, time) == 3, time, Mars);

    [HoroscopeCalculator(HoroscopeName.MarsAshtakavargaYoga22)]
    public static CalculatorResult MarsAshtakavargaYoga22(Time time)
    {
        var sign = Calculate.PlanetRasiD1Sign(Mars, time).GetSignName();
        return Result(InKendraOrTrikona(Mars, time) &&
                      sign is ZodiacName.Sagittarius or ZodiacName.Aries or ZodiacName.Capricorn &&
                      Bindu(Mars, Mars, time) == 4, time, Mars);
    }

    [HoroscopeCalculator(HoroscopeName.MarsAshtakavargaYoga23)]
    public static CalculatorResult MarsAshtakavargaYoga23(Time time) =>
        Result(Bindu(Mars, Mars, time) >= 5 &&
               (Calculate.IsPlanetConjunctWithPlanet(Mars, Saturn, time) || MutualAspect(Mars, Saturn, time)),
            time, Mars, Saturn);

    [HoroscopeCalculator(HoroscopeName.MarsAshtakavargaYoga24)]
    public static CalculatorResult MarsAshtakavargaYoga24(Time time) =>
        Result(Bindu(Mars, Mars, time) is >= 1 and <= 3 &&
               AssociatedWithOrAspectedBy(Mars, Mercury, time), time, Mars, Mercury);

    [HoroscopeCalculator(HoroscopeName.MarsAshtakavargaYoga25)]
    public static CalculatorResult MarsAshtakavargaYoga25(Time time) =>
        Result(Bindu(Mars, Mars, time) >= 5 && AssociatedWithOrAspectedBy(Mars, Moon, time), time, Mars, Moon);

    [HoroscopeCalculator(HoroscopeName.MercuryAshtakavargaYoga3)]
    public static CalculatorResult MercuryAshtakavargaYoga3(Time time) =>
        Result(Bindu(Mercury, Mercury, time) is >= 1 and <= 3 && InHouse(Mercury, time, House6, House8) &&
               !Calculate.IsPlanetAspectedByBeneficPlanets(Mercury, time), time, Mercury);

    [HoroscopeCalculator(HoroscopeName.MercuryAshtakavargaYoga4)]
    public static CalculatorResult MercuryAshtakavargaYoga4(Time time) =>
        Result(Bindu(Mercury, Mercury, time) is >= 1 and <= 3 && InHouse(Mercury, time, House6, House8, House12) &&
               Calculate.IsPlanetConjunctWithPlanet(Mercury, Venus, time), time, Mercury, Venus);

    [HoroscopeCalculator(HoroscopeName.MercuryAshtakavargaYoga5)]
    public static CalculatorResult MercuryAshtakavargaYoga5(Time time) =>
        Result(Bindu(Mercury, Mercury, time) == 5 && InKendraOrTrikona(Mercury, time) &&
               (AssociatedWithOrAspectedBy(Mercury, Jupiter, time) ||
                AssociatedWithOrAspectedBy(Mercury, Saturn, time)), time, Mercury, Jupiter, Saturn);

    [HoroscopeCalculator(HoroscopeName.MercuryAshtakavargaYoga6)]
    public static CalculatorResult MercuryAshtakavargaYoga6(Time time)
    {
        var mercuryFromSaturn = Calculate.SignDistanceFromPlanetToPlanet(Saturn, Mercury, time);
        var jupiterInfluencesSecond = HouseOf(Jupiter, time) == House2 ||
                                      Calculate.PlanetsInHouseBasedOnSign(House2, time)
                                          .Any(planet => Calculate.IsPlanetAspectedByPlanet(planet, Jupiter, time));
        return Result(Bindu(Mercury, Mercury, time) == 5 && mercuryFromSaturn is 4 or 6 &&
                      jupiterInfluencesSecond, time, Mercury, Saturn, Jupiter);
    }

    [HoroscopeCalculator(HoroscopeName.MercuryAshtakavargaYoga7)]
    public static CalculatorResult MercuryAshtakavargaYoga7(Time time) =>
        Result(Bindu(Mercury, Mercury, time) == 5 &&
               (Calculate.IsPlanetConjunctWithPlanet(Mercury, Jupiter, time) ||
                AssociatedWithOrAspectedBy(Mercury, Mars, time)), time, Mercury, Jupiter, Mars);

    [HoroscopeCalculator(HoroscopeName.MercuryAshtakavargaYoga9)]
    public static CalculatorResult MercuryAshtakavargaYoga9(Time time)
    {
        var rasiLord = Calculate.LordOfZodiacSign(Calculate.PlanetRasiD1Sign(Mercury, time).GetSignName());
        var navamshaLord = Calculate.LordOfZodiacSign(Calculate.PlanetNavamshaD9Sign(Mercury, time).GetSignName());
        return Result(Bindu(Mercury, Mercury, time) == 4 && rasiLord == Mars && navamshaLord == Venus &&
                      Calculate.IsPlanetAspectedByPlanet(Mercury, Jupiter, time), time, Mercury, Mars, Venus, Jupiter);
    }

    [HoroscopeCalculator(HoroscopeName.MercuryAshtakavargaYoga12B)]
    public static CalculatorResult MercuryAshtakavargaYoga12B(Time time)
    {
        var mercurySignLord = Calculate.LordOfZodiacSign(Calculate.PlanetRasiD1Sign(Mercury, time).GetSignName());
        return Result(InHouse(mercurySignLord, time, House6, House8, House12), time, Mercury, mercurySignLord);
    }

    private static CalculatorResult Result(bool occurring, Time time, params PlanetName[] planets) =>
        CalculatorResult.New(occurring, planets, time);

    private static int Bindu(PlanetName chartPlanet, PlanetName placedPlanet, Time time) =>
        Calculate.PlanetAshtakvargaBindu(chartPlanet,
            Calculate.PlanetRasiD1Sign(placedPlanet, time).GetSignName(), time);

    private static HouseName HouseOf(PlanetName planet, Time time) =>
        Calculate.HousePlanetOccupiesBasedOnSign(planet, time);

    private static bool InKendra(PlanetName planet, Time time, bool includeLagna)
    {
        var house = HouseOf(planet, time);
        return house is House4 or House7 or House10 || (includeLagna && house == House1);
    }

    private static bool InKendraOrTrikona(PlanetName planet, Time time) =>
        HouseOf(planet, time) is House1 or House4 or House5 or House7 or House9 or House10;

    private static bool Conjunct(PlanetName subject, Time time, params PlanetName[] planets) =>
        planets.All(planet => Calculate.IsPlanetConjunctWithPlanet(subject, planet, time));

    private static bool InHouse(PlanetName planet, Time time, params HouseName[] houses) =>
        houses.Contains(HouseOf(planet, time));

    private static bool MarsYoga10Base(Time time) =>
        Bindu(Mars, Mars, time) == 6 && InHouse(Mars, time, House6, House8, House12) &&
        (Calculate.IsPlanetDebilitated(Mars, time) || Calculate.IsPlanetCombust(Mars, time)) &&
        Calculate.IsWaxingMoon(time) && Calculate.IsPlanetConjunctWithPlanet(Mars, Moon, time);

    private static bool MarsYoga12(Time time, System.Func<ZodiacName, bool> parity)
    {
        var occupantQualifies = new[] { Mars, Saturn }.Any(planet =>
            HouseOf(planet, time) == House3 && Bindu(Mars, planet, time) is >= 1 and <= 3);
        var thirdLord = Calculate.LordOfHouse(House3, time);
        var marsSignLord = Calculate.LordOfZodiacSign(Calculate.PlanetRasiD1Sign(Mars, time).GetSignName());
        return occupantQualifies && (parity(Calculate.PlanetRasiD1Sign(thirdLord, time).GetSignName()) ||
                                     parity(Calculate.PlanetRasiD1Sign(marsSignLord, time).GetSignName()));
    }

    private static bool IsDebilitatedOrInimical(PlanetName planet, Time time)
    {
        var relationship = Calculate.PlanetRelationshipWithSign(
            planet, Calculate.PlanetRasiD1Sign(planet, time).GetSignName(), time);
        return Calculate.IsPlanetDebilitated(planet, time) ||
               relationship is PlanetToSignRelationship.EnemyVarga or PlanetToSignRelationship.BitterEnemyVarga;
    }

    private static bool AssociatedWithOrAspectedBy(PlanetName subject, PlanetName other, Time time) =>
        Calculate.IsPlanetConjunctWithPlanet(subject, other, time) ||
        Calculate.IsPlanetAspectedByPlanet(subject, other, time);

    private static bool MutualAspect(PlanetName first, PlanetName second, Time time) =>
        Calculate.IsPlanetAspectedByPlanet(first, second, time) &&
        Calculate.IsPlanetAspectedByPlanet(second, first, time);

    private static bool AssociatedWithOrAspectedByBenefic(PlanetName subject, Time time) =>
        PlanetName.All9Planets.Except(new[] { subject })
            .Any(planet => Calculate.IsPlanetBenefic(planet, time) && AssociatedWithOrAspectedBy(subject, planet, time));

    private static bool AssociatedWithOrAspectedByMalefic(PlanetName subject, Time time) =>
        PlanetName.All9Planets.Except(new[] { subject })
            .Any(planet => Calculate.IsPlanetMalefic(planet, time) && AssociatedWithOrAspectedBy(subject, planet, time));
}
