using System;

namespace Genso.Astrology.Library
{
    // Historical releases generated this ignored file on a publisher's
    // workstation. The maintained container build supplies it explicitly so
    // every image is reproducible from the fork and its recorded source SHA.
    public static class ThisAssembly
    {
        public static readonly string CommitHash =
            Environment.GetEnvironmentVariable("VEDASTRO_SOURCE_REVISION") ?? "unknown";
        public const string CommitNumber = "managed";
        public const string BranchName = "resonant-managed";
        public static readonly string Version = $"{CommitHash}-{CommitNumber}-{BranchName}";
    }
}
