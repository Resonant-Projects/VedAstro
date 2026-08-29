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
}
