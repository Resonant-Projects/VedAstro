using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using GeoTimeZone;
using TimeZoneConverter;
using static System.Net.WebRequestMethods;
using static Genso.Astrology.Library.PlanetName;

namespace Genso.Astrology.Library
{
    /// <summary>
    /// A collection of general functions that don't have a home yet, so they live here for now.
    /// You're allowed to move them somewhere you see fit, not copy, move!
    /// </summary>
    public static class Tools
    {
        private const string DefaultNominatimUrl = "https://nominatim.openstreetmap.org";
        private const string NominatimUserAgent = "VedAstro-Resonant/1.0 (+https://github.com/Resonant-Projects/VedAstro)";
        private static readonly HttpClient NominatimClient = CreateNominatimClient();
        private static readonly SemaphoreSlim NominatimRequestGate = new(1, 1);
        private static DateTimeOffset _lastNominatimRequestUtc = DateTimeOffset.MinValue;


        /// <summary>
        /// "H1N1" -> ["H", "1", "N", "1"]
        /// "H" -> ["H"]
        /// "GH1N12" -> ["GH", "1", "N", "12"]
        /// "OS234" -> ["OS", "234"]
        /// </summary>
        public static List<string> SplitAlpha(string input)
        {
            var words = new List<string> { string.Empty };
            for (var i = 0; i < input.Length; i++)
            {
                words[words.Count - 1] += input[i];
                if (i + 1 < input.Length && char.IsLetter(input[i]) != char.IsLetter(input[i + 1]))
                {
                    words.Add(string.Empty);
                }
            }
            return words;
        }

        /// <summary>
        /// Converts xml element instance to string properly
        /// </summary>
        public static string XmlToString(XElement xml)
        {
            //remove all formatting, for clean xml as string
            return xml.ToString(SaveOptions.DisableFormatting);
        }

        /// <summary>
        /// Gets XML file from any URL and parses it into xelement list
        /// </summary>
        public static async Task<List<XElement>> GetXmlFileHttp(string url)
        {
            //get the data sender
            using var client = new HttpClient();

            //load xml event data files before hand to be used quickly later for search
            //get main horoscope prediction file (located in wwwroot)
            var fileStream = await client.GetStreamAsync(url);

            //parse raw file to xml doc
            var document = XDocument.Load(fileStream);

            //get all records in document
            return document.Root.Elements().ToList();
        }

        /// <summary>
        /// Converts any type to XML, it will use Type's own ToXml() converter if available
        /// else ToString is called and placed inside element with Type's full name
        /// Note, used to transfer data via internet Client to API Server
        /// Example:
        /// <TypeName>
        ///     DataValue
        /// </TypeName>
        /// </summary>
        public static XElement AnyTypeToXml<T>(T value)
        {
            //check if type has own ToXml method
            //use the Type's own converter if available
            if (value is IToXml hasToXml)
            {
                var betterXml = hasToXml.ToXml();
                return betterXml;
            }

            //gets enum value as string to place inside XML
            //note: value can be null hence ?, fails quietly
            var enumValueStr = value?.ToString();

            //get the name of the Enum
            //Note: This is the name that will be used
            //later to instantiate the class from string
            var typeName = typeof(T).FullName;

            return new XElement(typeName, enumValueStr);
        }

        /// <summary>
        /// Converts any type that implements IToXml to XML, it will use Type's own ToXml() converter
        /// Note, used to transfer data via internet Client to API Server
        /// Placed inside "Root" xml
        /// Default name for root element is Root
        /// </summary>
        public static XElement AnyTypeToXmlList<T>(List<T> xmlList, string rootElementName = "Root") where T : IToXml
        {
            var rootXml = new XElement(rootElementName);
            foreach (var xmlItem in xmlList)
            {
                rootXml.Add(AnyTypeToXml(xmlItem));
            }
            return rootXml;
        }

        /// <summary>
        /// Simple override for XML, to skip parsing to type before sorting
        /// </summary>
        public static XElement AnyTypeToXmlList(List<XElement> xmlList, string rootElementName = "Root")
        {
            var rootXml = new XElement(rootElementName);
            foreach (var xmlItem in xmlList)
            {
                rootXml.Add(xmlItem);
            }
            return rootXml;
        }

        /// <summary>
        /// Given the URL of a standard VedAstro XML file, like "http://...PersonList.xml",
        /// will convert to the specified type and return in nice list, with time to be home for dinner
        /// </summary>
        public static async Task<List<T>> ConvertXmlListFileToInstanceList<T>(string httpUrl) where T : IToXml, new()
        {
            //get data list from Static Website storage
            //note : done so that any updates to that live file will be instantly reflected in API results
            var eventDataListXml = await Tools.GetXmlFileHttp(httpUrl);

            //parse each raw event data in list
            var eventDataList = new List<T>();
            foreach (var eventDataXml in eventDataListXml)
            {
                //add it to the return list
                var x = new T();
                eventDataList.Add(x.FromXml<T>(eventDataXml));
            }

            return eventDataList;

        }

        /// <summary>
        /// Converts given exception data to XML
        /// </summary>
        public static XElement ExceptionToXml(Exception e)
        {

            var responseMessage = new XElement("Exception");

            responseMessage.Add($"#Message#\n{e.Message}\n");
            responseMessage.Add($"#Data#\n{e.Data}\n");
            responseMessage.Add($"#InnerException#\n{e.InnerException}\n");
            responseMessage.Add($"#Source#\n{e.Source}\n");
            responseMessage.Add($"#Source#\n{e.Source}\n");
            responseMessage.Add($"#StackTrace#\n{e.StackTrace}\n");
            responseMessage.Add($"#StackTrace#\n{e.TargetSite}\n");

            return responseMessage;
        }

        /// <summary>
        /// - Type is a value typ
        /// - Enum
        /// </summary>
        public static dynamic XmlToAnyType<T>(XElement xml) // where T : //IToXml, new()
        {
            //get the name of the Enum
            var typeNameFullName = typeof(T).FullName;
            var typeNameShortName = typeof(T).FullName;

#if DEBUG
            Console.WriteLine(xml.ToString());
#endif

            //type name inside XML
            var xmlElementName = xml?.Name;

            //get the value for parsing later
            var rawVal = xml.Value;


            //make sure the XML enclosing type has the same name
            //check both full class name, and short class name
            var isSameName = xmlElementName == typeNameFullName || xmlElementName == typeof(T).GetShortTypeName();

            //if not same name raise error
            if (!isSameName)
            {
                throw new Exception($"Can't parse XML {xmlElementName} to {typeNameFullName}");
            }

            //implements ToXml()
            var typeImplementsToXml = typeof(T).GetInterfaces().Any(x =>
                x.IsGenericType &&
                x.GetGenericTypeDefinition() == typeof(IToXml));

            //type has owm ToXml method
            if (typeImplementsToXml)
            {
                dynamic inputTypeInstance = GetInstance(typeof(T).FullName);

                return inputTypeInstance.FromXml(xml);

            }

            //if type is an Enum process differently
            if (typeof(T).IsEnum)
            {
                var parsedEnum = (T)Enum.Parse(typeof(T), rawVal);

                return parsedEnum;
            }

            //else it is a value type
            if (typeof(T) == typeof(string))
            {
                return rawVal;
            }

            if (typeof(T) == typeof(double))
            {
                return Double.Parse(rawVal);
            }

            if (typeof(T) == typeof(int))
            {
                return int.Parse(rawVal);
            }

            //raise error since converter not implemented
            throw new NotImplementedException($"XML converter for {typeNameFullName}, not implemented!");
        }

        /// <summary>
        /// Gets only the name of the Class, without assembly
        /// </summary>
        public static string GetShortTypeName(this Type type)
        {
            var sb = new StringBuilder();
            var name = type.Name;
            if (!type.IsGenericType) return name;
            sb.Append(name.Substring(0, name.IndexOf('`')));
            sb.Append("<");
            sb.Append(string.Join(", ", type.GetGenericArguments()
                .Select(t => t.GetShortTypeName())));
            sb.Append(">");
            return sb.ToString();
        }

        public static bool Implements<I>(this Type type, I @interface) where I : class
        {
            if (((@interface as Type) == null) || !(@interface as Type).IsInterface)
                throw new ArgumentException("Only interfaces can be 'implemented'.");

            return (@interface as Type).IsAssignableFrom(type);
        }

        /// <summary>
        /// For converting value types, String, Double, etc.
        /// </summary>
        //public static dynamic XmlToValueType<T>(XElement xml) 
        //{
        //    //get the name of the Enum
        //    var typeName = nameof(T);


        //    //raise error since not XML type and Input type mismatch
        //    throw new Exception($"Can't parse XML to {typeName}");
        //}


        /// <summary>
        /// Gets an instance of Class from string name
        /// </summary>
        public static object GetInstance(string strFullyQualifiedName)
        {
            Type type = Type.GetType(strFullyQualifiedName);
            if (type != null)
                return Activator.CreateInstance(type);
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(strFullyQualifiedName);
                if (type != null)
                    return Activator.CreateInstance(type);
            }

            return null;
        }


        /// <summary>
        /// Converts days to hours
        /// </summary>
        /// <returns></returns>
        public static double DaysToHours(double days) => days * 24.0;

        public static double MinutesToHours(double minutes) => minutes / 60.0;

        public static double MinutesToYears(double minutes) => minutes / 525600.0;

        public static double MinutesToDays(double minutes) => minutes / 1440.0;

        /// <summary>
        /// Given a date it will count the days to the end of that year
        /// </summary>
        public static double GetDaysToNextYear(Time getBirthDateTime)
        {
            //get start of next year
            var standardTime = getBirthDateTime.GetStdDateTimeOffset();
            var nextYear = standardTime.Year + 1;
            var startOfNextYear = new DateTimeOffset(nextYear, 1, 1, 0, 0, 0, 0, standardTime.Offset);

            //calculate difference of days between 2 dates
            var diffDays = (startOfNextYear - standardTime).TotalDays;

            return diffDays;
        }

        /// <summary>
        /// Gets the time now in the system in text form
        /// formatted with standard style (HH:mm dd/MM/yyyy zzz) 
        /// </summary>
        public static string GetNowSystemTimeText() => DateTimeOffset.Now.ToString(Time.DateTimeFormat);

        /// <summary>
        /// Gets the time now in the system in text form with seconds (HH:mm:ss dd/MM/yyyy zzz) 
        /// </summary>
        public static string GetNowSystemTimeSecondsText() => DateTimeOffset.Now.ToString(Time.DateTimeFormatSeconds);

        /// <summary>
        /// Gets the time now in the Server (+8:00) in text form with seconds (HH:mm:ss dd/MM/yyyy zzz) 
        /// </summary>
        public static string GetNowServerTimeSecondsText() => DateTimeOffset.Now.ToOffset(TimeSpan.FromHours(8)).ToString(Time.DateTimeFormatSeconds);

        /// <summary>
        /// Custom hash generator for Strings. Returns consistent/deterministic values
        /// If null returns 0
        /// Note: MD5 (System.Security.Cryptography) not used because not supported in Blazor WASM
        /// </summary>
        public static int GetStringHashCode(string stringToHash)
        {
            if (stringToHash == null)
            {
                return 0;
            }

            unchecked
            {
                int hash1 = (5381 << 16) + 5381;
                int hash2 = hash1;

                for (int i = 0; i < stringToHash.Length; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ stringToHash[i];
                    if (i == stringToHash.Length - 1)
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ stringToHash[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }


            //MD5 md5Hasher = MD5.Create();
            //var hashedByte = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(stringToHash));
            //return BitConverter.ToInt32(hashedByte, 0);

        }

        /// <summary>
        /// Gets random unique ID
        /// </summary>
        public static string GenerateId() => Guid.NewGuid().ToString("N");


        /// <summary>
        /// Converts any list to comma separated string
        /// Note: calls ToString();
        /// </summary>
        public static string ListToString<T>(List<T> list)
        {
            var combinedNames = "";
            foreach (var item in list)
            {
                combinedNames += item.ToString() + ", ";
            }

            return combinedNames;
        }







        //█▀▀ █░█ ▀▀█▀▀ █▀▀ █▀▀▄ █▀▀ ░▀░ █▀▀█ █▀▀▄ 　 █▀▄▀█ █▀▀ ▀▀█▀▀ █░░█ █▀▀█ █▀▀▄ █▀▀ 
        //█▀▀ ▄▀▄ ░░█░░ █▀▀ █░░█ ▀▀█ ▀█▀ █░░█ █░░█ 　 █░▀░█ █▀▀ ░░█░░ █▀▀█ █░░█ █░░█ ▀▀█ 
        //▀▀▀ ▀░▀ ░░▀░░ ▀▀▀ ▀░░▀ ▀▀▀ ▀▀▀ ▀▀▀▀ ▀░░▀ 　 ▀░░░▀ ▀▀▀ ░░▀░░ ▀░░▀ ▀▀▀▀ ▀▀▀░ ▀▀▀


        /// <summary>
        /// Find the first offset in the string that might contain the characters
        /// in `needle`, in any order. Returns -1 if not found.
        /// <para>This function can return false positives</para>
        /// </summary>
        public static bool FindCluster(this string haystack, string needle)
        {
            if (haystack == null) return false;
            if (needle == null) return false;

            if (haystack.Length < needle.Length) return false;

            long sum = needle.ToCharArray().Sum(c => c);
            long rolling = haystack.ToCharArray().Take(needle.Length).Sum(c => c);

            var idx = 0;
            var head = needle.Length;
            while (rolling != sum)
            {
                if (head >= haystack.Length) return false;
                rolling -= haystack[idx];
                rolling += haystack[head];
                head++;
                idx++;
            }

            return true;
        }

        /// <summary>
        /// Remap from 1 range to another
        /// </summary>
        public static float Remap(this float from, float fromMin, float fromMax, float toMin, float toMax)
        {
            var fromAbs = from - fromMin;
            var fromMaxAbs = fromMax - fromMin;

            var normal = fromAbs / fromMaxAbs;

            var toMaxAbs = toMax - toMin;
            var toAbs = toMaxAbs * normal;

            var to = toAbs + toMin;

            return to;
        }

        /// <summary>
        /// Remap from 1 range to another
        /// </summary>
        public static double Remap(this double from, double fromMin, double fromMax, double toMin, double toMax)
        {
            var fromAbs = from - fromMin;
            var fromMaxAbs = fromMax - fromMin;

            var normal = fromAbs / fromMaxAbs;

            var toMaxAbs = toMax - toMin;
            var toAbs = toMaxAbs * normal;

            var to = toAbs + toMin;

            return to;
        }

        public static string StreamToString(Stream stream)
        {
            StreamReader reader = new StreamReader(stream);
            string text = reader.ReadToEnd();

            return text;
        }

        /// <summary>
        /// Converts a timezone (+08:00) in string form to parsed timespan 
        /// </summary>
        public static TimeSpan StringToTimezone(string timezoneRaw)
        {
            return DateTimeOffset.ParseExact(timezoneRaw, "zzz", CultureInfo.InvariantCulture).Offset;
        }

        /// <summary>
        /// Returns system timezone offset as TimeSpan
        /// </summary>
        public static string GetSystemTimezoneStr() => DateTimeOffset.Now.ToString("zzz");

        /// <summary>
        /// Returns system timezone offset as TimeSpan
        /// </summary>
        public static TimeSpan GetSystemTimezone() => DateTimeOffset.Now.Offset;

        public static async Task<WebResult<GeoLocation>> AddressToGeoLocation(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) { return FailedGeoLocationResult(); }

            var url = $"{GetNominatimBaseUrl()}/search?q={Uri.EscapeDataString(address)}&format=jsonv2&limit=1";

            try
            {
                using var response = await SendNominatimRequest(url);
                if (!response.IsSuccessStatusCode) { return FailedGeoLocationResult(); }

                var rawReply = await response.Content.ReadAsStringAsync();
                var result = JArray.Parse(rawReply).FirstOrDefault() as JObject;
                if (result == null) { return FailedGeoLocationResult(); }

                return ParseNominatimGeoLocation(result);
            }
            catch (Exception)
            {
                return FailedGeoLocationResult();
            }
        }

        /// <summary>
        /// Gets the name of a place from its coordinates using Nominatim.
        /// </summary>
        public static async Task<WebResult<GeoLocation>> CoordinateToGeoLocation(double longitude, double latitude)
        {
            if (!AreValidCoordinates(latitude, longitude)) { return FailedGeoLocationResult(); }

            var latitudeText = latitude.ToString(CultureInfo.InvariantCulture);
            var longitudeText = longitude.ToString(CultureInfo.InvariantCulture);
            var url = $"{GetNominatimBaseUrl()}/reverse?lat={latitudeText}&lon={longitudeText}&format=jsonv2";

            try
            {
                using var response = await SendNominatimRequest(url);
                if (!response.IsSuccessStatusCode) { return FailedGeoLocationResult(); }

                var rawReply = await response.Content.ReadAsStringAsync();
                var result = JObject.Parse(rawReply);
                return ParseNominatimGeoLocation(result);
            }
            catch (Exception)
            {
                return FailedGeoLocationResult();
            }

        }

        /// <summary>
        /// Gets the historical UTC offset for a named location at the supplied instant.
        /// </summary>
        public static async Task<TimeSpan> GetTimezoneOffset(string locationName, DateTimeOffset timeAtLocation)
        {
            var geoLocation = await GeoLocation.FromName(locationName);
            var result = await GetTimezoneOffsetApi(geoLocation, timeAtLocation);
            if (!result.IsPass) { throw new InvalidOperationException($"Timezone lookup failed for location '{locationName}'."); }

            return Tools.StringToTimezone(result.Payload);
        }

        public static async Task<string> GetTimezoneOffsetString(string locationName, DateTime timeAtLocation)
        {
            var geoLocation = await GeoLocation.FromName(locationName);
            var result = GetTimezoneOffsetForLocalTime(geoLocation, timeAtLocation);
            if (!result.IsPass) { throw new InvalidOperationException($"Timezone lookup failed for location '{locationName}'."); }

            return result.Payload;
        }

        public static async Task<string> GetTimezoneOffsetString(string location, string dateTime)
        {
            var lifeEvtTimeNoTimezone = DateTime.ParseExact(dateTime, Time.DateTimeFormatNoTimezone, null);
            return await Tools.GetTimezoneOffsetString(location, lifeEvtTimeNoTimezone);
        }

        /// <summary>
        /// Gets the historical UTC offset for coordinates at the supplied instant.
        /// </summary>
        public static Task<WebResult<string>> GetTimezoneOffsetApi(GeoLocation geoLocation, DateTimeOffset timeAtLocation)
        {
            var longitude = geoLocation.GetLongitude();
            var latitude = geoLocation.GetLatitude();

            try
            {
                if (!AreValidCoordinates(latitude, longitude)) { return Task.FromResult(FailedTimezoneResult()); }

                var timeZone = GetTimeZoneInfo(latitude, longitude);
                var offset = timeZone.GetUtcOffset(timeAtLocation);
                return Task.FromResult(new WebResult<string>(true, TimeSpanToUTCTimezoneString(offset)));
            }
            catch (Exception)
            {
                return Task.FromResult(FailedTimezoneResult());
            }
        }

        /// <summary>
        /// Given a timespan instance converts to string timezone +08:00
        /// </summary>
        private static string TimeSpanToUTCTimezoneString(TimeSpan offsetMinutes)
        {
            return new DateTimeOffset(2000, 1, 1, 0, 0, 0, offsetMinutes).ToString("zzz");
        }

        private static WebResult<string> GetTimezoneOffsetForLocalTime(GeoLocation geoLocation, DateTime timeAtLocation)
        {
            var longitude = geoLocation.GetLongitude();
            var latitude = geoLocation.GetLatitude();

            try
            {
                if (!AreValidCoordinates(latitude, longitude)) { return FailedTimezoneResult(); }

                var timeZone = GetTimeZoneInfo(latitude, longitude);
                var localTime = DateTime.SpecifyKind(timeAtLocation, DateTimeKind.Unspecified);
                if (timeZone.IsInvalidTime(localTime) || timeZone.IsAmbiguousTime(localTime)) { return FailedTimezoneResult(); }

                var offset = timeZone.GetUtcOffset(localTime);
                return new WebResult<string>(true, TimeSpanToUTCTimezoneString(offset));
            }
            catch (Exception)
            {
                return FailedTimezoneResult();
            }
        }

        private static TimeZoneInfo GetTimeZoneInfo(double latitude, double longitude)
        {
            var ianaTimeZoneId = TimeZoneLookup.GetTimeZone(latitude, longitude).Result;
            if (string.IsNullOrWhiteSpace(ianaTimeZoneId)) { throw new TimeZoneNotFoundException(); }

            return TZConvert.GetTimeZoneInfo(ianaTimeZoneId);
        }

        private static HttpClient CreateNominatimClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd(NominatimUserAgent);
            return client;
        }

        private static string GetNominatimBaseUrl()
        {
            var configuredUrl = Environment.GetEnvironmentVariable("VEDASTRO_NOMINATIM_URL");
            return (string.IsNullOrWhiteSpace(configuredUrl) ? DefaultNominatimUrl : configuredUrl).TrimEnd('/');
        }

        private static async Task<HttpResponseMessage> SendNominatimRequest(string url)
        {
            await NominatimRequestGate.WaitAsync();
            try
            {
                var elapsed = DateTimeOffset.UtcNow - _lastNominatimRequestUtc;
                var delay = TimeSpan.FromSeconds(1) - elapsed;
                if (delay > TimeSpan.Zero) { await Task.Delay(delay); }

                _lastNominatimRequestUtc = DateTimeOffset.UtcNow;
                return await NominatimClient.GetAsync(url, HttpCompletionOption.ResponseContentRead);
            }
            finally
            {
                NominatimRequestGate.Release();
            }
        }

        private static WebResult<GeoLocation> ParseNominatimGeoLocation(JObject result)
        {
            var name = result.Value<string>("display_name");
            var latitudeText = result.Value<string>("lat");
            var longitudeText = result.Value<string>("lon");

            var latitudeParsed = double.TryParse(latitudeText, NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude);
            var longitudeParsed = double.TryParse(longitudeText, NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude);
            if (string.IsNullOrWhiteSpace(name) || !latitudeParsed || !longitudeParsed || !AreValidCoordinates(latitude, longitude))
            {
                return FailedGeoLocationResult();
            }

            return new WebResult<GeoLocation>(true, new GeoLocation(name, longitude, latitude));
        }

        private static bool AreValidCoordinates(double latitude, double longitude) =>
            !double.IsNaN(latitude) && !double.IsInfinity(latitude) && latitude is >= -90 and <= 90 &&
            !double.IsNaN(longitude) && !double.IsInfinity(longitude) && longitude is >= -180 and <= 180;

        private static WebResult<GeoLocation> FailedGeoLocationResult() => new(false, GeoLocation.Empty);

        private static WebResult<string> FailedTimezoneResult() => new(false, string.Empty);

        /// <summary>
        /// Calls a URL and returns the content of the result as XML
        /// Even if content is returned as JSON, it is converted to XML
        /// Note:
        /// - if JSON auto adds "Root" as first element, unless specified
        /// for XML data root element name is ignored
        /// </summary>
        public static async Task<WebResult<XElement>> ReadFromServerXmlReply(string apiUrl, string rootElementName = "Root")
        {
            var returnResult = new WebResult<XElement>();
            string rawMessage = "";

            try
            {
                //send request to API server
                using var result = await RequestServerPost(apiUrl);
                if (!result.IsSuccessStatusCode)
                {
                    returnResult.IsPass = false;
                    return returnResult;
                }

                //parse data reply
                rawMessage = result.Content.ReadAsStringAsync().Result;

                //raw message can be JSON or XML
                //try parse as XML if fail then as JSON
                var readFromServerXmlReply = XElement.Parse(rawMessage);
                returnResult.Payload = readFromServerXmlReply;
                returnResult.IsPass = true; //pass

            }
            catch (Exception)
            {
                //try to parse data as JSON
                try
                {
                    var rawXml = JsonConvert.DeserializeXmlNode(rawMessage, rootElementName);
                    if (rawXml == null) { throw new JsonSerializationException("Server returned an empty JSON payload."); }

                    var readFromServerXmlReply = XElement.Parse(rawXml.InnerXml);

                    returnResult.Payload = readFromServerXmlReply;
                    returnResult.IsPass = true; //pass

                }
                //unparseable data, let user know
                catch (Exception)
                {
                    //todo log it
                    var logData = $"ReadFromServerXmlReply()\n{rawMessage}";

                    returnResult.IsPass = false; //fail
                }
            }

            //send the prepared result caller
            return returnResult;


            //--------------------
            // FUNCTIONS

            async Task<HttpResponseMessage> RequestServerPost(string receiverAddress)
            {
                //prepare the data to be sent
                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, receiverAddress);

                //get the data sender 
                using var client = new HttpClient() { Timeout = new TimeSpan(0, 0, 0, 0, Timeout.Infinite) }; //no timeout

                //tell sender to wait for complete reply before exiting
                var waitForContent = HttpCompletionOption.ResponseContentRead;

                //send the data on its way
                var response = await client.SendAsync(httpRequestMessage, waitForContent);

                //return the raw reply to caller
                return response;
            }
        }

        /// <summary>
        /// Given a list of strings will return one by random
        /// Used to make dynamic user error & info messages
        /// </summary>
        public static string RandomSelect(string[] msgList)
        {
            // Create a Random object  
            Random rand = new Random();

            // Generate a random index less than the size of the array.  
            int randomIndexNumber = rand.Next(msgList.Length);

            //return random text from list to caller
            return msgList[randomIndexNumber];
        }

        /// <summary>
        /// Split string by character count
        /// </summary>
        public static IEnumerable<string> SplitByCharCount(string str, int maxChunkSize)
        {
            for (int i = 0; i < str.Length; i += maxChunkSize)
                yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
        }

        /// <summary>
        /// Inputed event name has be space separated
        /// </summary>
        public static List<PlanetName> GetPlanetFromName(string eventName)
        {
            var returnList = new List<PlanetName>();

            //lower case it
            var lowerCased = eventName.ToLower();

            //split into words
            var splited = lowerCased.Split(' ');

            //check if any be parsed into planet name
            foreach (var word in splited)
            {
                var result = PlanetName.TryParse(word, out var planetParsed);
                if (result)
                {
                    //add list if parsed
                    returnList.Add(planetParsed);
                }
            }


            //return list to caller
            return returnList;
        }

        /// <summary>
        /// Packages the data into ready form for the HTTP client to use in final sending stage
        /// </summary>
        public static StringContent XmLtoHttpContent(XElement data)
        {
            //gets the main XML data as a string
            var dataString = Tools.XmlToString(data);

            //specify the data encoding
            var encoding = Encoding.UTF8;

            //specify the type of the data sent
            //plain text, stops auto formatting
            var mediaType = "plain/text";

            //return packaged data to caller
            return new StringContent(dataString, encoding, mediaType);
        }

        /// <summary>
        /// Extracts data from an Exception puts it in a nice XML
        /// </summary>
        public static XElement ExtractDataFromException(Exception e)
        {
            //place to store the exception data
            string fileName;
            string methodName;
            int line;
            int columnNumber;
            string message;
            string source;

            //get the exception that started it all
            var originalException = e.GetBaseException();

            //extract the data from the error
            StackTrace st = new StackTrace(e, true);

            //Get the first stack frame
            StackFrame frame = st.GetFrame(st.FrameCount - 1);

            //Get the file name
            fileName = frame?.GetFileName();

            //Get the method name
            methodName = frame.GetMethod()?.Name;

            //Get the line number from the stack frame
            line = frame.GetFileLineNumber();

            //Get the column number
            columnNumber = frame.GetFileColumnNumber();

            message = originalException.ToString();

            source = originalException.Source;
            //todo include inner exception data
            var stackTrace = originalException.StackTrace;


            //put together the new error record
            var newRecord = new XElement("Error",
                new XElement("Message", message),
                new XElement("Source", source),
                new XElement("FileName", fileName),
                new XElement("SourceLineNumber", line),
                new XElement("SourceColNumber", columnNumber),
                new XElement("MethodName", methodName),
                new XElement("MethodName", methodName)
            );


            return newRecord;
        }

        /// <summary>
        /// Gets now time with seconds in wrapped in xml element
        /// used for logging
        /// </summary>
        public static XElement TimeStampSystemXml => new("TimeStamp", Tools.GetNowSystemTimeSecondsText());

        /// <summary>
        /// Gets now time at server location (+8:00) with seconds in wrapped in xml element
        /// used for logging
        /// </summary>
        public static XElement TimeStampServerXml => new("TimeStampServer", Tools.GetNowServerTimeSecondsText());

        /// <summary>
        /// Gets now time in UTC +8:00
        /// Because server time is uncertain, all change to UTC8
        /// </summary>
        public static string GetNow()
        {
            //create utc 8
            var utc8 = new TimeSpan(8, 0, 0);
            //get now time in utc 0
            var nowTime = DateTimeOffset.Now.ToUniversalTime();
            //convert time utc 0 to utc 8
            var utc8Time = nowTime.ToOffset(utc8);

            //return converted time to caller
            return utc8Time.ToString(Time.DateTimeFormatSeconds);
        }

        /// <summary>
        /// Removes all invalid characters for an person name
        /// used to clean name field user input
        /// allowed chars : periods (.) and hyphens (-), space ( )
        /// SRC:https://learn.microsoft.com/en-us/dotnet/standard/base-types/how-to-strip-invalid-characters-from-a-string
        /// </summary>
        public static string CleanNameText(string nameInput)
        {
            // Replace invalid characters with empty strings.
            try
            {
                var cleanText = Regex.Replace(nameInput, @"[^\w\.\s*-]", "", RegexOptions.None, TimeSpan.FromSeconds(2));
                return cleanText;
            }
            // If we timeout when replacing invalid characters,
            // we should return Empty.
            catch (RegexMatchTimeoutException)
            {
                return string.Empty;
            }
        }

    }

}
