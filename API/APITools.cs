using System.Net;
using System.Net.Mime;
using System.Xml.Linq;
using Azure;
using Azure.Communication.Email;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json.Linq;
using VedAstro.Library;

namespace API;

/// <summary>
/// HTTP and serialization helpers used by the Azure Functions front desk.
/// Restored from the last first-party implementation before the source file
/// was deleted while its call sites remained.
/// </summary>
public static class APITools
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = Timeout.InfiniteTimeSpan
    };

    /// <summary>Optional note attached to the next OpenAPI response.</summary>
    public static string? ApiExtraNote { get; set; }

    public static async Task<JObject> ExtractDataFromRequestJson(HttpRequestData request)
    {
        var body = await request.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(body) ? new JObject() : JObject.Parse(body);
    }

    public static HttpResponseData FailMessageJson(XElement payload, HttpRequestData request) =>
        MessageJson("Fail", payload, request);

    public static HttpResponseData FailMessageJson(string payload, HttpRequestData request) =>
        MessageJson("Fail", payload, request);

    public static HttpResponseData FailMessageJson(Exception exception, HttpRequestData request) =>
        MessageJson("Fail", Tools.ExceptionToJSON(exception), request);

    public static HttpResponseData PassMessageJson(object? payload, HttpRequestData request) =>
        MessageJson("Pass", payload, request);

    public static HttpResponseData PassMessageJson(HttpRequestData request) =>
        MessageJson("Pass", null, request);

    private static HttpResponseData MessageJson(string status, object? payload, HttpRequestData request)
    {
        var response = request.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", MediaTypeNames.Application.Json);
        response.Headers.Add("Call-Status", status);
        response.Headers.Add("Access-Control-Expose-Headers", "Call-Status");

        var envelope = new JObject { ["Status"] = status };
        if (!string.IsNullOrWhiteSpace(ApiExtraNote)) { envelope["Note"] = ApiExtraNote; }
        if (payload is not null) { envelope["Payload"] = ToJsonToken(payload); }
        response.WriteString(envelope.ToString());
        return response;
    }

    private static JToken ToJsonToken(object payload) => payload switch
    {
        JProperty property => new JObject(property),
        JToken token => token,
        XElement xml => JToken.FromObject(xml),
        IEnumerable<XElement> xmlList => Tools.ListToJson(xmlList.ToList()),
        IEnumerable<OpenAPIMetadata> metadata => Tools.ListToJson(metadata.ToList()),
        string text => new JValue(text),
        _ => JToken.FromObject(payload)
    };

    public static async Task<HttpResponseMessage> GetRequest(string receiverAddress)
    {
        var response = await HttpClient.GetAsync(receiverAddress, HttpCompletionOption.ResponseContentRead);
        response.EnsureSuccessStatusCode();
        return response;
    }

    public static List<Person> GetAllPersonList(bool skipLifeEvents = false) =>
        AzureTable.PersonList.Query<PersonListEntity>()
            .Select(row => Person.FromAzureRow(row, skipLifeEvents))
            .ToList();

    public static HttpResponseData SendTextToCaller(string content, HttpRequestData request)
    {
        var response = request.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
        response.WriteString(content);
        return response;
    }

    public static HttpResponseData SendSvgToCaller(string svg, HttpRequestData request)
    {
        var response = request.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "image/svg+xml; charset=utf-8");
        response.WriteString(svg);
        return response;
    }

    public static HttpResponseData SendAnyToCaller(string calculatorName, object result, HttpRequestData request)
    {
        if (result is byte[] fileData)
        {
            return Tools.SendFileToCaller(fileData, request, MediaTypeNames.Application.Octet);
        }

        var payload = result is JArray array ? array : Tools.AnyToJSON(calculatorName, result);
        return PassMessageJson(payload, request);
    }

    public static void SendEmail(string fileName, string fileFormat, string receiverEmailAddress, Stream file)
    {
        var emailServiceConfiguration = Secrets.Get("AutoEmailerConnectString");
        if (string.IsNullOrWhiteSpace(emailServiceConfiguration))
        {
            throw new InvalidOperationException("Missing AutoEmailerConnectString configuration.");
        }

        var extension = fileFormat.ToLowerInvariant();
        var fullName = $"{fileName}.{extension}";
        var content = new EmailContent($"Shared {fileFormat.ToUpperInvariant()} from VedAstro")
        {
            PlainText = $"Find attached {fullName}, shared from VedAstro.org.",
            Html = "<html><body>Shared file from VedAstro.org</body></html>"
        };
        var message = new EmailMessage("contact@vedastro.org", receiverEmailAddress, content);
        var mimeType = Tools.StringToMimeType(fileFormat) ?? MediaTypeNames.Application.Octet;
        message.Attachments.Add(new EmailAttachment(fullName, mimeType, BinaryData.FromStream(file)));

        var operation = new EmailClient(emailServiceConfiguration).Send(WaitUntil.Completed, message);
        if (operation.Value.Status != EmailSendStatus.Succeeded)
        {
            throw new InvalidOperationException($"Email delivery ended with status {operation.Value.Status}.");
        }
    }
}
