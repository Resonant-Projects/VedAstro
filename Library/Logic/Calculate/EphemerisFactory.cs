using System;
using System.IO;
using System.Linq;
using SwissEphNet;

namespace VedAstro.Library
{
    public static class EphemerisFactory
    {
        private static readonly (string FileName, int Body)[] RequiredFiles =
        {
            ("sepl_18.se1", SwissEph.SE_SUN),
            ("semo_18.se1", SwissEph.SE_MOON),
            ("seas_18.se1", SwissEph.SE_CERES)
        };

        public static string EphemerisFilesPath
        {
            get
            {
                string configuredPath = Environment.GetEnvironmentVariable("VEDASTRO_EPHEMERIS_PATH");
                return string.IsNullOrEmpty(configuredPath)
                    ? Path.Combine(AppContext.BaseDirectory, "ephemeris")
                    : configuredPath;
            }
        }

        public static SwissEph New()
        {
            string ephemerisFilesPath = EphemerisFilesPath;
            SwissEph swissEph = new SwissEph();
            swissEph.swe_set_ephe_path(ephemerisFilesPath);
            swissEph.OnLoadFile += (_, e) =>
            {
                string normalizedFileName = e.FileName.Replace('\\', '/');
                int lastSeparator = normalizedFileName.LastIndexOf('/');
                string fileName = normalizedFileName.Substring(lastSeparator + 1);
                string filePath = Path.Combine(ephemerisFilesPath, fileName);

                if (!File.Exists(filePath))
                {
                    if (string.Equals(Path.GetExtension(fileName), ".se1", StringComparison.OrdinalIgnoreCase))
                    {
                        throw CreateValidationException(
                            ephemerisFilesPath,
                            $"required ephemeris file '{fileName}' is not available for the requested date or body");
                    }

                    return;
                }

                e.File = File.OpenRead(filePath);
            };

            return swissEph;
        }

        public static void ValidateEphemerisFiles()
        {
            string ephemerisFilesPath = EphemerisFilesPath;
            string[] missingFiles = RequiredFiles
                .Where(requiredFile => !File.Exists(Path.Combine(ephemerisFilesPath, requiredFile.FileName)))
                .Select(requiredFile => requiredFile.FileName)
                .ToArray();

            if (missingFiles.Length > 0)
            {
                throw CreateValidationException(
                    ephemerisFilesPath,
                    $"missing required files: {string.Join(", ", missingFiles)}");
            }

            try
            {
                using SwissEph swissEph = New();
                foreach ((string fileName, int body) in RequiredFiles)
                {
                    double[] positions = new double[6];
                    string error = string.Empty;
                    int resultFlags = swissEph.swe_calc_ut(
                        2451545.0,
                        body,
                        SwissEph.SEFLG_SWIEPH | SwissEph.SEFLG_SPEED,
                        positions,
                        ref error);

                    bool usedSwissEphemeris = (resultFlags & SwissEph.SEFLG_SWIEPH) != 0;
                    bool usedMoshier = (resultFlags & SwissEph.SEFLG_MOSEPH) != 0;
                    bool errorReportsMoshier = error?.Contains("Moshier", StringComparison.OrdinalIgnoreCase) == true;
                    if (!usedSwissEphemeris || usedMoshier || errorReportsMoshier)
                    {
                        string cause = string.IsNullOrWhiteSpace(error)
                            ? $"SwissEphNet returned flags {resultFlags} instead of the Swiss Ephemeris backend while loading '{fileName}'"
                            : error;
                        throw CreateValidationException(ephemerisFilesPath, cause);
                    }
                }
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw CreateValidationException(ephemerisFilesPath, exception.Message);
            }
        }

        private static InvalidOperationException CreateValidationException(string path, string cause)
        {
            return new InvalidOperationException(
                $"Swiss Ephemeris data could not be loaded from '{path}' ({cause}). " +
                "The service is refusing to start without real ephemeris data; verify that the required .se1 files exist and are readable.");
        }
    }
}
