using System;
using System.Linq;

namespace VedAstro.Library
{
    /// <summary>
    /// Uniquely identifies a specific call to the method
    /// It holds the method name and the params used to call the method (only hashes, for performance)
    /// Note: Use class over struct for performance
    /// </summary>
    [Serializable()]
    public class CacheKey
    {
        public string Function;
        private readonly object[] _arguments;
        private int _ultimateHash;

        internal int UltimateHash => _ultimateHash;

        //CTOR
        public CacheKey(string function, params object[] args)
        {
            Function = function;
            _arguments = args?.ToArray();

            //get hashes of all values
            var functionNameHash = Tools.GetStringHashCode(function);
            var allArgumentsHash = GetHashCodeForArray(args);

            //combine them together
            _ultimateHash = functionNameHash + allArgumentsHash;
        }

        private CacheKey(string function, int ultimateHash)
        {
            Function = function;
            _ultimateHash = ultimateHash;
            _arguments = null;
        }

        internal static CacheKey FromHash(string function, int ultimateHash)
        {
            return new CacheKey(function, ultimateHash);
        }


        //PUBLIC METHODS
        public override bool Equals(object value)
        {
            if (value is not CacheKey possibleMatch ||
                !string.Equals(Function, possibleMatch.Function, StringComparison.Ordinal))
            {
                return false;
            }

            //Disk cache files historically contain only the deterministic hash.
            //Live keys retain their arguments so hash collisions cannot alias two
            //different calls; loaded legacy keys preserve disk compatibility.
            if (_arguments is null || possibleMatch._arguments is null)
            {
                return _ultimateHash == possibleMatch._ultimateHash;
            }

            return ArgumentsEqual(_arguments, possibleMatch._arguments);
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

        private static bool ArgumentsEqual(object[] left, object[] right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            for (var index = 0; index < left.Length; index++)
            {
                if (!ValuesEqual(left[index], right[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ValuesEqual(object left, object right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left is null || right is null)
            {
                return false;
            }

            if (left is Array leftArray && right is Array rightArray)
            {
                if (leftArray.Rank != rightArray.Rank)
                {
                    return false;
                }

                for (var dimension = 0; dimension < leftArray.Rank; dimension++)
                {
                    if (leftArray.GetLength(dimension) != rightArray.GetLength(dimension))
                    {
                        return false;
                    }
                }

                return leftArray.Cast<object>()
                    .Zip(rightArray.Cast<object>(), ValuesEqual)
                    .All(equal => equal);
            }

            return left.Equals(right);
        }
    }
}
