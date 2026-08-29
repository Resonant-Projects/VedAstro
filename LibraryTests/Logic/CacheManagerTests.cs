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
    public void SaveCacheToDiskRejectsDomainTypesWithoutExplicitConverters()
    {
        var temporaryDirectory = Directory.CreateTempSubdirectory("vedastro-cache-unsupported-");
        var originalCacheFileName = Syntax.CacheFileName;

        try
        {
            Syntax.CacheFileName = Path.Combine(temporaryDirectory.FullName, "cache");
            var cache = new ConcurrentDictionary<CacheKey, object>();
            cache.TryAdd(new CacheKey("UnsupportedDomain", "house"), new House());

            var saveMethod = typeof(CacheManager).GetMethod(
                "saveCacheInNewFile",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.IsNotNull(saveMethod);
            saveMethod.Invoke(null, new object[] { "UnsupportedDomain", 1, cache });

            var cacheFile = Directory.GetFiles(temporaryDirectory.FullName).Single();
            Assert.AreEqual("[]", File.ReadAllText(cacheFile));
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
}
