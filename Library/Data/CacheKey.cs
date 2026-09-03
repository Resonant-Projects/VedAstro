using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace VedAstro.Library
{
    /// <summary>
    /// Uniquely identifies a specific call to the method
    /// It holds a deterministic bucket hash plus a collision-safe argument fingerprint.
    /// Note: Use class over struct for performance
    /// </summary>
    [Serializable()]
    public class CacheKey
    {
        public readonly string Function;
        private readonly string _argumentFingerprint;
        private readonly int _ultimateHash;

        internal int UltimateHash => _ultimateHash;
        internal string ArgumentFingerprint => _argumentFingerprint;

        //CTOR
        public CacheKey(string function, params object[] args)
        {
            Function = function;
            _argumentFingerprint = GetArgumentFingerprint(args);

            //get hashes of all values
            var functionNameHash = Tools.GetStringHashCode(function);
            var allArgumentsHash = GetHashCodeForArray(args);

            //combine them together
            _ultimateHash = functionNameHash + allArgumentsHash;
        }

        private CacheKey(string function, int ultimateHash, string argumentFingerprint)
        {
            Function = function;
            _ultimateHash = ultimateHash;
            _argumentFingerprint = argumentFingerprint;
        }

        internal static CacheKey FromHash(
            string function,
            int ultimateHash,
            string argumentFingerprint)
        {
            return new CacheKey(function, ultimateHash, argumentFingerprint);
        }


        //PUBLIC METHODS
        public override bool Equals(object value)
        {
            if (value is not CacheKey possibleMatch ||
                !string.Equals(Function, possibleMatch.Function, StringComparison.Ordinal))
            {
                return false;
            }

            return _ultimateHash == possibleMatch._ultimateHash &&
                   string.Equals(
                       _argumentFingerprint,
                       possibleMatch._argumentFingerprint,
                       StringComparison.Ordinal);
        }

        public override int GetHashCode() => _ultimateHash;
        //{
        //    //get hash of all the fields & combine them
        //    var hash1 = Function.GetHashCode();

        //    //get the hash for each param in args & add it together
        //    var hash2 = 0;
        //    foreach (var arg in Args)
        //    {
        //        hash2 += arg.GetHashCode();
        //    }

        //    return hash1 + hash2;
        //}


        //PRIVARE METHODS

        /// <summary>
        /// Gets the hash code for the contents of the array since the default hash code
        /// for an array is unique even if the contents are the same.
        /// </summary>
        /// <remarks>
        /// See Jon Skeet (C# MVP) response in the StackOverflow thread 
        /// http://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-an-overridden-system-object-gethashcode
        /// </remarks>
        /// <param name="array">The array to generate a hash code for.</param>
        /// <returns>The hash code for the values in the array.</returns>
        private int GetHashCodeForArray(object[] array)
        {
            // if non-null array then go into unchecked block to avoid overflow
            if (array != null)
            {
                unchecked
                {
                    int hash = 17;

                    // get hash code for all items in array
                    foreach (var item in array)
                    {
                        hash = hash * 23 + GetStableHashCode(item);
                    }

                    return hash;
                }
            }

            // if null, hash code is zero
            return 0;
        }

        private static int GetStableHashCode(object value)
        {
            if (value == null)
            {
                return 0;
            }

            if (value is string text)
            {
                return Tools.GetStringHashCode(text);
            }

            if (value is Array array)
            {
                unchecked
                {
                    var hash = 17;
                    foreach (var item in array)
                    {
                        hash = (hash * 23) + GetStableHashCode(item);
                    }

                    return hash;
                }
            }

            return value.GetHashCode();
        }

        private static string GetArgumentFingerprint(object[] arguments)
        {
            var canonical = new StringBuilder();
            AppendValue(canonical, arguments);
            return Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
        }

        private static void AppendValue(StringBuilder canonical, object value)
        {
            if (value is null)
            {
                canonical.Append("N;");
                return;
            }

            AppendText(
                canonical,
                value.GetType().AssemblyQualifiedName ?? value.GetType().FullName ?? string.Empty);

            switch (value)
            {
                case string text:
                    AppendText(canonical, text);
                    break;
                case Time time:
                    var standardTime = time.GetStdDateTimeOffset();
                    canonical.Append(standardTime.Ticks).Append(';')
                        .Append(standardTime.Offset.Ticks).Append(';');
                    AppendValue(canonical, time.GetGeoLocation());
                    break;
                case GeoLocation location:
                    AppendText(canonical, location.Name());
                    canonical.Append(BitConverter.DoubleToInt64Bits(location.Longitude())).Append(';')
                        .Append(BitConverter.DoubleToInt64Bits(location.Latitude())).Append(';');
                    break;
                case DateTimeOffset dateTimeOffset:
                    canonical.Append(dateTimeOffset.Ticks).Append(';')
                        .Append(dateTimeOffset.Offset.Ticks).Append(';');
                    break;
                case DateTime dateTime:
                    canonical.Append(dateTime.Ticks).Append(';')
                        .Append((int)dateTime.Kind).Append(';');
                    break;
                case TimeSpan timeSpan:
                    canonical.Append(timeSpan.Ticks).Append(';');
                    break;
                case double doubleValue:
                    canonical.Append(BitConverter.DoubleToInt64Bits(doubleValue)).Append(';');
                    break;
                case float floatValue:
                    canonical.Append(BitConverter.SingleToInt32Bits(floatValue)).Append(';');
                    break;
                case decimal decimalValue:
                    foreach (var part in decimal.GetBits(decimalValue))
                    {
                        canonical.Append(part).Append(';');
                    }
                    break;
                case Guid guid:
                    canonical.Append(guid.ToString("N")).Append(';');
                    break;
                case Array array:
                    canonical.Append(array.Rank).Append(';');
                    for (var dimension = 0; dimension < array.Rank; dimension++)
                    {
                        canonical.Append(array.GetLength(dimension)).Append(';');
                    }
                    foreach (var item in array)
                    {
                        AppendValue(canonical, item);
                    }
                    break;
                default:
                    AppendText(canonical, Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
                    canonical.Append(GetStableHashCode(value)).Append(';');
                    break;
            }
        }

        private static void AppendText(StringBuilder canonical, string value)
        {
            canonical.Append(value.Length).Append(':').Append(value).Append(';');
        }
    }
}
