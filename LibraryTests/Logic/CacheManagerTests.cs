using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VedAstro.Library.Tests;

[TestClass]
public class CacheManagerTests
{
    [TestMethod]
    public void SaveCacheToDiskUsesSafeJsonOnNet8()
    {
        var temporaryDirectory = Directory.CreateTempSubdirectory("vedastro-cache-");
        var originalCacheFileName = Syntax.CacheFileName;

        try
        {
            Syntax.CacheFileName = Path.Combine(temporaryDirectory.FullName, "cache");
            var cache = new ConcurrentDictionary<CacheKey, object>();
            cache.TryAdd(new CacheKey("DiskCacheCompatibility", "key"), "value");

            var saveMethod = typeof(CacheManager).GetMethod(
                "saveCacheInNewFile",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.IsNotNull(saveMethod);
            saveMethod.Invoke(null, new object[] { "DiskCacheCompatibility", 1, cache });

            var cacheFile = Directory.GetFiles(temporaryDirectory.FullName).Single();
            Assert.IsTrue(new FileInfo(cacheFile).Length > 0);

            var deserializeMethod = typeof(CacheManager).GetMethod(
                "deserializeCache",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.IsNotNull(deserializeMethod);
            var restoredCache = (ConcurrentDictionary<CacheKey, object>)deserializeMethod.Invoke(
                null,
                new object[] { cacheFile })!;
            Assert.AreEqual("value", restoredCache[new CacheKey("DiskCacheCompatibility", "key")]);
        }
        finally
        {
            Syntax.CacheFileName = originalCacheFileName;
            temporaryDirectory.Delete(recursive: true);
        }
    }

    [TestMethod]
    public async Task SaveCacheToDiskRestoresDomainValuesAndCompletedTasks()
    {
        var temporaryDirectory = Directory.CreateTempSubdirectory("vedastro-cache-domain-");
        var originalCacheFileName = Syntax.CacheFileName;

        try
        {
            Syntax.CacheFileName = Path.Combine(temporaryDirectory.FullName, "cache");
            var location = new GeoLocation("Greenwich", 0, 51.4934);
            var time = new Time(new DateTimeOffset(2000, 1, 1, 12, 30, 0, TimeSpan.Zero), location);
            var cache = new ConcurrentDictionary<CacheKey, object>();
            cache.TryAdd(new CacheKey("DomainRoundTrip", "location"), location);
            cache.TryAdd(new CacheKey("DomainRoundTrip", "time"), time);
            var house = new House(
                HouseName.House1,
                Angle.FromDegrees(350),
                Angle.FromDegrees(5),
                Angle.FromDegrees(20));
            cache.TryAdd(new CacheKey("DomainRoundTrip", "angle"), Angle.FromDegrees(-12.5));
            cache.TryAdd(new CacheKey("DomainRoundTrip", "house"), house);
            cache.TryAdd(new CacheKey("DomainRoundTrip", "house-list"), new List<House> { house });
            cache.TryAdd(
                new CacheKey("DomainRoundTrip", "zodiac"),
                new ZodiacSign(ZodiacName.Aries, Angle.FromDegrees(4.5)));
            cache.TryAdd(
                new CacheKey("DomainRoundTrip", "constellation"),
                new Constellation(1, 2, Angle.FromDegrees(5)));
            cache.TryAdd(new CacheKey("DomainRoundTrip", "shashtiamsa"), new Shashtiamsa(12.25));
            cache.TryAdd(
                new CacheKey("DomainRoundTrip", "house-strength"),
                new HouseSubStrength(new Dictionary<HouseName, double> { [HouseName.House1] = 7.5 }, "Test"));
            cache.TryAdd(new CacheKey("DomainRoundTrip", "task"), Task.FromResult(time));
            cache.TryAdd(new CacheKey("DomainRoundTrip", "null-task"), Task.FromResult<string?>(null));
            cache.TryAdd(new CacheKey("DomainRoundTrip", "nullable-task"), Task.FromResult<int?>(42));
            cache.TryAdd(
                new CacheKey("DomainRoundTrip", "tuple-task"),
                Task.FromResult((true, location)));

            var saveMethod = typeof(CacheManager).GetMethod(
                "saveCacheInNewFile",
                BindingFlags.NonPublic | BindingFlags.Static);
            var deserializeMethod = typeof(CacheManager).GetMethod(
                "deserializeCache",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.IsNotNull(saveMethod);
            Assert.IsNotNull(deserializeMethod);
            saveMethod.Invoke(null, new object[] { "DomainRoundTrip", 1, cache });

            var cacheFile = Directory.GetFiles(temporaryDirectory.FullName).Single();
            var restoredCache = (ConcurrentDictionary<CacheKey, object>)deserializeMethod.Invoke(
                null,
                new object[] { cacheFile })!;

            var restoredLocation = (GeoLocation)restoredCache[new CacheKey("DomainRoundTrip", "location")];
            Assert.AreEqual(location.Name(), restoredLocation.Name());
            Assert.AreEqual(location.Longitude(), restoredLocation.Longitude());
            Assert.AreEqual(location.Latitude(), restoredLocation.Latitude());

            var restoredTime = (Time)restoredCache[new CacheKey("DomainRoundTrip", "time")];
            Assert.AreEqual(time.GetStdDateTimeOffset(), restoredTime.GetStdDateTimeOffset());
            Assert.AreEqual(location, restoredTime.GetGeoLocation());

            var restoredAngle = (Angle)restoredCache[new CacheKey("DomainRoundTrip", "angle")];
            Assert.AreEqual(-12.5, restoredAngle.TotalDegrees);

            var restoredHouse = (House)restoredCache[new CacheKey("DomainRoundTrip", "house")];
            Assert.AreEqual(house, restoredHouse);
            var restoredHouseList =
                (List<House>)restoredCache[new CacheKey("DomainRoundTrip", "house-list")];
            CollectionAssert.AreEqual(new[] { house }, restoredHouseList);

            var restoredZodiac = (ZodiacSign)restoredCache[new CacheKey("DomainRoundTrip", "zodiac")];
            Assert.AreEqual(ZodiacName.Aries, restoredZodiac.GetSignName());
            Assert.AreEqual(4.5, restoredZodiac.GetDegreesInSign().TotalDegrees);

            var restoredConstellation =
                (Constellation)restoredCache[new CacheKey("DomainRoundTrip", "constellation")];
            Assert.AreEqual(1, restoredConstellation.GetConstellationNumber());
            Assert.AreEqual(2, restoredConstellation.GetQuarter());
            Assert.AreEqual(5, restoredConstellation.GetDegreesInConstellation().TotalDegrees);

            var restoredShashtiamsa =
                (Shashtiamsa)restoredCache[new CacheKey("DomainRoundTrip", "shashtiamsa")];
            Assert.AreEqual(12.25, restoredShashtiamsa.ToDouble());

            var restoredStrength =
                (HouseSubStrength)restoredCache[new CacheKey("DomainRoundTrip", "house-strength")];
            Assert.AreEqual("Test", restoredStrength.Name);
            Assert.AreEqual(7.5, restoredStrength.Power[HouseName.House1]);

            var restoredTask = (Task<Time>)restoredCache[new CacheKey("DomainRoundTrip", "task")];
            Assert.AreEqual(time.GetStdDateTimeOffset(), (await restoredTask).GetStdDateTimeOffset());

            var restoredNullTask = (Task<string?>)restoredCache[new CacheKey("DomainRoundTrip", "null-task")];
            Assert.IsNull(await restoredNullTask);

            var restoredNullableTask =
                (Task<int?>)restoredCache[new CacheKey("DomainRoundTrip", "nullable-task")];
            Assert.AreEqual(42, await restoredNullableTask);

            var restoredTupleTask =
                (Task<(bool, GeoLocation)>)restoredCache[new CacheKey("DomainRoundTrip", "tuple-task")];
            var restoredTuple = await restoredTupleTask;
            Assert.IsTrue(restoredTuple.Item1);
            Assert.AreEqual(location, restoredTuple.Item2);
        }
        finally
        {
            Syntax.CacheFileName = originalCacheFileName;
            temporaryDirectory.Delete(recursive: true);
        }
    }

    [TestMethod]
    public void LoadCacheFromDiskRejectsUnapprovedTypes()
    {
        var cacheFile = Path.GetTempFileName();

        try
        {
            File.WriteAllText(
                cacheFile,
                """
                [{"Function":"Unsafe","Hash":1,"ValueType":"System.IO.FileInfo, System.Private.CoreLib","Value":{"OriginalPath":"/tmp/payload"}}]
                """);

            var deserializeMethod = typeof(CacheManager).GetMethod(
                "deserializeCache",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.IsNotNull(deserializeMethod);
            Assert.IsNotNull(
                Type.GetType("System.IO.FileInfo, System.Private.CoreLib", throwOnError: false),
                "Test precondition: the rejected type must resolve so the allowlist is exercised.");
            var restoredCache = (ConcurrentDictionary<CacheKey, object>)deserializeMethod.Invoke(
                null,
                new object[] { cacheFile })!;
            Assert.AreEqual(0, restoredCache.Count);
        }
        finally
        {
            File.Delete(cacheFile);
        }
    }

    [TestMethod]
    public void LoadCacheFromDisk0SkipsMalformedFiles()
    {
        var temporaryDirectory = Directory.CreateTempSubdirectory("vedastro-cache-malformed-");
        var originalCacheFilePath = Syntax.CacheFilePath;

        try
        {
            Syntax.CacheFilePath = temporaryDirectory.FullName;
            File.WriteAllText(Path.Combine(temporaryDirectory.FullName, "cache_Malformed_1.json"), "{");

            CacheManager.LoadCacheFromDisk0();
        }
        finally
        {
            Syntax.CacheFilePath = originalCacheFilePath;
            temporaryDirectory.Delete(recursive: true);
        }
    }

    [TestMethod]
    public void CacheKeyUsesDeterministicStringHashes()
    {
        var expected = Tools.GetStringHashCode("StableFunction") +
                       ((17 * 23) + Tools.GetStringHashCode("StableArgument"));

        Assert.AreEqual(expected, new CacheKey("StableFunction", "StableArgument").GetHashCode());
    }

    [TestMethod]
    [DoNotParallelize]
    public void CacheKeyDistinguishesPreciseTimesWithCollidingLowTickBits()
    {
        var temporaryDirectory = Directory.CreateTempSubdirectory("vedastro-cache-precise-time-");
        var originalCacheFileName = Syntax.CacheFileName;
        var location = new GeoLocation("Greenwich", 0, 51.4934);
        var first = new Time(
            new DateTimeOffset(2000, 1, 1, 12, 0, 0, TimeSpan.Zero),
            location);
        var second = new Time(
            first.GetStdDateTimeOffset().AddTicks((1L << 32) + 1),
            location);

        Assert.AreNotEqual(first, second);
        Assert.AreEqual(first.GetHashCode(), second.GetHashCode(), "Test precondition: hashes must collide.");

        var cache = new ConcurrentDictionary<CacheKey, object>();
        cache.TryAdd(new CacheKey("PreciseTime", first), "first");
        cache.TryAdd(new CacheKey("PreciseTime", second), "second");

        Assert.AreEqual(2, cache.Count);
        Assert.AreEqual("first", cache[new CacheKey("PreciseTime", first)]);
        Assert.AreEqual("second", cache[new CacheKey("PreciseTime", second)]);

        try
        {
            Syntax.CacheFileName = Path.Combine(temporaryDirectory.FullName, "cache");
            var saveMethod = typeof(CacheManager).GetMethod(
                "saveCacheInNewFile",
                BindingFlags.NonPublic | BindingFlags.Static);
            var deserializeMethod = typeof(CacheManager).GetMethod(
                "deserializeCache",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.IsNotNull(saveMethod);
            Assert.IsNotNull(deserializeMethod);
            saveMethod.Invoke(null, new object[] { "PreciseTime", 1, cache });

            var cacheFile = Directory.GetFiles(temporaryDirectory.FullName).Single();
            var restoredCache = (ConcurrentDictionary<CacheKey, object>)deserializeMethod.Invoke(
                null,
                new object[] { cacheFile })!;

            Assert.AreEqual(2, restoredCache.Count);
            Assert.AreEqual("first", restoredCache[new CacheKey("PreciseTime", first)]);
            Assert.AreEqual("second", restoredCache[new CacheKey("PreciseTime", second)]);
        }
        finally
        {
            Syntax.CacheFileName = originalCacheFileName;
            temporaryDirectory.Delete(recursive: true);
        }
    }

    [TestMethod]
    public void CacheRetryFileNamesCannotCollideWithNumberedChunks()
    {
        var buildFileNameMethod = typeof(CacheManager).GetMethod(
            "buildCacheFileName",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.IsNotNull(buildFileNameMethod);
        var retriedFirstChunk = (string)buildFileNameMethod.Invoke(
            null,
            new object[] { "Collision", 1, 1 })!;
        var normalSecondChunk = (string)buildFileNameMethod.Invoke(
            null,
            new object[] { "Collision", 2, 0 })!;

        Assert.AreNotEqual(retriedFirstChunk, normalSecondChunk);
        StringAssert.EndsWith(retriedFirstChunk, "_1_retry1.json");
        StringAssert.EndsWith(normalSecondChunk, "_2.json");
    }

    [TestMethod]
    public void UnsupportedOrNullEntriesDoNotDropValidChunkEntries()
    {
        var temporaryDirectory = Directory.CreateTempSubdirectory("vedastro-cache-partial-");
        var originalCacheFileName = Syntax.CacheFileName;

        try
        {
            Syntax.CacheFileName = Path.Combine(temporaryDirectory.FullName, "cache");
            var cache = new ConcurrentDictionary<CacheKey, object>();
            cache.TryAdd(new CacheKey("PartialChunk", "valid"), "kept");
            cache.TryAdd(new CacheKey("PartialChunk", "multidimensional"), new int[1, 1]);
            cache.TryAdd(new CacheKey("PartialChunk", "null"), null!);

            var saveMethod = typeof(CacheManager).GetMethod(
                "saveCacheInNewFile",
                BindingFlags.NonPublic | BindingFlags.Static);
            var deserializeMethod = typeof(CacheManager).GetMethod(
                "deserializeCache",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.IsNotNull(saveMethod);
            Assert.IsNotNull(deserializeMethod);
            saveMethod.Invoke(null, new object[] { "PartialChunk", 1, cache });

            var cacheFile = Directory.GetFiles(temporaryDirectory.FullName).Single();
            var restoredCache = (ConcurrentDictionary<CacheKey, object>)deserializeMethod.Invoke(
                null,
                new object[] { cacheFile })!;

            Assert.AreEqual(1, restoredCache.Count);
            Assert.AreEqual("kept", restoredCache[new CacheKey("PartialChunk", "valid")]);
        }
        finally
        {
            Syntax.CacheFileName = originalCacheFileName;
            temporaryDirectory.Delete(recursive: true);
        }
    }

    [TestMethod]
    public void CacheLoadSelectsNewestChunkVariant()
    {
        var temporaryDirectory = Directory.CreateTempSubdirectory("vedastro-cache-variants-");
        var originalCacheFilePath = Syntax.CacheFilePath;

        try
        {
            Syntax.CacheFilePath = temporaryDirectory.FullName;
            var canonicalFile = Path.Combine(temporaryDirectory.FullName, "cache_Variant_1.json");
            var retryFile = Path.Combine(temporaryDirectory.FullName, "cache_Variant_1_retry1.json");
            File.WriteAllText(canonicalFile, "[]");
            File.WriteAllText(retryFile, "[]");

            var getFilesMethod = typeof(CacheManager).GetMethod(
                "getCacheFileGroupsForLoad",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(getFilesMethod);

            File.SetLastWriteTimeUtc(canonicalFile, DateTime.UtcNow.AddMinutes(-2));
            File.SetLastWriteTimeUtc(retryFile, DateTime.UtcNow.AddMinutes(-1));
            CollectionAssert.AreEqual(
                new[] { retryFile, canonicalFile },
                ((string[][])getFilesMethod.Invoke(null, null)!)[0]);

            File.SetLastWriteTimeUtc(canonicalFile, DateTime.UtcNow);
            CollectionAssert.AreEqual(
                new[] { canonicalFile, retryFile },
                ((string[][])getFilesMethod.Invoke(null, null)!)[0]);

            File.WriteAllText(canonicalFile, "{");
            File.SetLastWriteTimeUtc(canonicalFile, DateTime.UtcNow.AddMinutes(1));
            File.SetLastWriteTimeUtc(retryFile, DateTime.UtcNow);
            var orderedVariants = ((string[][])getFilesMethod.Invoke(null, null)!)[0];
            var tryDeserializeMethod = typeof(CacheManager).GetMethod(
                "tryDeserializeCacheVariants",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(tryDeserializeMethod);
            object?[] arguments = { orderedVariants, null, null };

            Assert.IsTrue((bool)tryDeserializeMethod.Invoke(null, arguments)!);
            Assert.AreEqual(retryFile, arguments[1]);
            Assert.AreEqual(
                0,
                ((ConcurrentDictionary<CacheKey, object>)arguments[2]!).Count);
        }
        finally
        {
            Syntax.CacheFilePath = originalCacheFilePath;
            temporaryDirectory.Delete(recursive: true);
        }
    }
}
