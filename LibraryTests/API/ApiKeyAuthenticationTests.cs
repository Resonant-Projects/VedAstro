using API.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VedAstro.Library.Tests.API;

[TestClass]
public class ApiKeyAuthenticationTests
{
    [TestMethod]
    public void MatchingKeyIsAuthorized() =>
        Assert.IsTrue(ApiKeyAuthentication.IsAuthorized("server-secret", "server-secret"));

    [TestMethod]
    public void IncorrectKeyIsRejected() =>
        Assert.IsFalse(ApiKeyAuthentication.IsAuthorized("server-secret", "wrong-secret"));

    [DataTestMethod]
    [DataRow(null, "server-secret")]
    [DataRow("", "server-secret")]
    [DataRow("server-secret", null)]
    [DataRow("server-secret", "")]
    public void MissingKeyIsRejected(string? configuredKey, string? suppliedKey) =>
        Assert.IsFalse(ApiKeyAuthentication.IsAuthorized(configuredKey, suppliedKey));

    [DataTestMethod]
    [DataRow("/api/version")]
    [DataRow("/API/VERSION")]
    [DataRow("/api/version/")]
    public void VersionHealthcheckDoesNotRequireApiKey(string path) =>
        Assert.IsTrue(ApiKeyAuthentication.IsExemptPath(path));

    [DataTestMethod]
    [DataRow("/api/version/extra")]
    [DataRow("/api/versions")]
    [DataRow("/api/calculate")]
    [DataRow("/")]
    public void EveryOtherPathStillRequiresApiKey(string path) =>
        Assert.IsFalse(ApiKeyAuthentication.IsExemptPath(path));
}
