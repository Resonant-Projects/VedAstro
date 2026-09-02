using System.Net;
using System.Security.Claims;
using System.Text;
using API;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VedAstro.Library.Tests;

[TestClass]
public class OpenAPIPostGatewayTests
{
    [DataTestMethod]
    [DataRow(
        "MoonSignName",
        "Location/40.7128,-74.0060/Time/12:00/01/12/1985/-05:00/Ayanamsa/LAHIRI")]
    [DataRow(
        "PlanetNirayanaLongitude",
        "PlanetName/Sun/Location/12.9716,77.5946/Time/00:05/29/02/2000/+05:30/Ayanamsa/LAHIRI")]
    [DataRow(
        "MoonSignName",
        "Ayanamsa/RAMAN/Location/40.7128,-74.0060/Time/12:00/01/12/1985/-05:00")]
    public async Task PostReturnsTheExactGetEnvelopeForTheSameSegments(
        string calculatorName,
        string parameterPath)
    {
        var baseUrl = "http://localhost:7071/api/Calculate/";
        var getRequest = TestHttpRequestData.Create(
            "GET",
            new Uri(baseUrl + calculatorName + "/" + parameterPath));
        var postRequest = TestHttpRequestData.Create(
            "POST",
            new Uri(baseUrl + calculatorName),
            PostBody(parameterPath));

        var getResponse = await OpenAPI.Calculate(getRequest, calculatorName, parameterPath);
        var postResponse = await OpenAPI.CalculatePost(postRequest, calculatorName);
        var getBody = await ResponseBody(getResponse);
        var postBody = await ResponseBody(postResponse);

        Assert.AreEqual(getBody, postBody);
        Assert.AreEqual(HttpStatusCode.OK, postResponse.StatusCode);
        Assert.AreEqual(string.Empty, postRequest.Url.Query);
        Assert.AreEqual($"/api/Calculate/{calculatorName}", postRequest.Url.AbsolutePath);

        var envelope = JObject.Parse(postBody);
        Assert.AreEqual("Pass", envelope.Value<string>("Status"));
        Assert.IsNotNull(envelope["Payload"]?[calculatorName]);
    }

    private static string PostBody(string parameterPath)
    {
        var segments = parameterPath.Split('/');
        return new JObject
        {
            ["parameters"] = new JArray(segments)
        }.ToString(Formatting.None);
    }

    private static async Task<string> ResponseBody(HttpResponseData response)
    {
        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    private sealed class TestHttpRequestData : HttpRequestData
    {
        private TestHttpRequestData(string method, Uri url, string body)
            : base(new TestFunctionContext())
        {
            Method = method;
            Url = url;
            Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
            Headers = new HttpHeadersCollection();
            Headers.Add("User-Agent", "Mozilla/5.0 Chrome/120.0");
        }

        public static TestHttpRequestData Create(string method, Uri url, string body = "") =>
            new(method, url, body);

        public override Stream Body { get; }

        public override HttpHeadersCollection Headers { get; }

        public override IReadOnlyCollection<IHttpCookie> Cookies => Array.Empty<IHttpCookie>();

        public override Uri Url { get; }

        public override IEnumerable<ClaimsIdentity> Identities => Array.Empty<ClaimsIdentity>();

        public override string Method { get; }

        public override HttpResponseData CreateResponse() => new TestHttpResponseData(FunctionContext);
    }

    private sealed class TestHttpResponseData : HttpResponseData
    {
        public TestHttpResponseData(FunctionContext functionContext)
            : base(functionContext)
        {
        }

        public override HttpStatusCode StatusCode { get; set; }

        public override HttpHeadersCollection Headers { get; set; } = new();

        public override Stream Body { get; set; } = new MemoryStream();

        public override HttpCookies Cookies => throw new NotSupportedException();
    }

    private sealed class TestFunctionContext : FunctionContext
    {
        public override string InvocationId => "test-invocation";

        public override string FunctionId => "test-function";

        public override TraceContext TraceContext => throw new NotSupportedException();

        public override BindingContext BindingContext => throw new NotSupportedException();

        public override RetryContext RetryContext => throw new NotSupportedException();

        public override IServiceProvider InstanceServices { get; set; } = new EmptyServiceProvider();

        public override FunctionDefinition FunctionDefinition => throw new NotSupportedException();

        public override IDictionary<object, object> Items { get; set; } = new Dictionary<object, object>();

        public override IInvocationFeatures Features => throw new NotSupportedException();
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
