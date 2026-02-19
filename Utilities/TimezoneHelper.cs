using TimeZoneConverter;

namespace HealthyCron.Utilities
{
    public static class TimezoneHelper
    {
        /// <summary>
        /// Converts a UTC DateTime to the given IANA timezone.
        /// Falls back to UTC if the timezone is invalid or null.
        /// </summary>
        public static DateTime ToUserTime(DateTime utcTime, string? ianaTimezone)
        {
            if (string.IsNullOrWhiteSpace(ianaTimezone))
                return utcTime;

            try
            {
                var tzi = TZConvert.GetTimeZoneInfo(ianaTimezone);
                return TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(utcTime, DateTimeKind.Utc), tzi);
            }
            catch
            {
                return utcTime;
            }
        }

        /// <summary>
        /// Returns true if the IANA timezone ID is valid.
        /// </summary>
        public static bool IsValidIana(string? ianaTimezone)
        {
            if (string.IsNullOrWhiteSpace(ianaTimezone)) return false;
            try
            {
                TZConvert.GetTimeZoneInfo(ianaTimezone);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Returns a sorted list of IANA timezone IDs.
        /// </summary>
        public static IReadOnlyList<string> GetAllIanaTimezones()
        {
            return TZConvert.KnownIanaTimeZoneNames
                .OrderBy(z => z)
                .ToList();
        }
    }
}
