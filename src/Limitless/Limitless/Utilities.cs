namespace Limitless
{
    internal static class Utilities
    {
        internal static DateTime EasternToUTC(DateTime easternTime)
        {
            TimeZoneInfo eastern = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            return TimeZoneInfo.ConvertTimeToUtc(easternTime, eastern);
        }

        public static DateTime ConvertUtcToEastern(DateTime utcTime)
        {
            // Ensure input is treated as UTC
            if (utcTime.Kind != DateTimeKind.Utc)
            {
                utcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
            }

            // Get Eastern Timezone (handles both EST and EDT automatically)
            TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

            // Convert to Eastern Time
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, easternZone);
        }
    }
}
