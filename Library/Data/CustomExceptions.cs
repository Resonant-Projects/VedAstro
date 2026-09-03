using System;

namespace VedAstro.Library
{
    [Serializable]
    public class ApiCommunicationFailed : Exception
    {
        public ApiCommunicationFailed() : base() { }
        public ApiCommunicationFailed(string message) : base(message) { }
        public ApiCommunicationFailed(string message, Exception inner) : base(message, inner) { }
    }
    public class NoInternetError : Exception
    {
        public NoInternetError() : base() { }
        public NoInternetError(string message) : base(message) { }
        public NoInternetError(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Raised when the Sun does not cross the horizon on the requested day at the requested place,
    /// i.e. polar day (Sun always up) or polar night (Sun always down). Sunrise and sunset are
    /// undefined for that day, so callers must degrade rather than invent a time.
    /// </summary>
    public class PolarSunException : Exception
    {
        public PolarSunException(string message, bool isPolarDay) : base(message)
        {
            IsPolarDay = isPolarDay;
        }

        /// <summary>
        /// True when the Sun stays above the horizon all day; false when it stays below (polar night).
        /// </summary>
        public bool IsPolarDay { get; }
    }
}
