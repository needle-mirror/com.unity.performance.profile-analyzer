namespace UnityEditor.Performance.ProfileAnalyzer
{
    internal class TimingOptions
    {
        public enum TimingOption
        {
            Time,
            Self,
        };

        public static readonly string[] TimingOptionNames =
        {
            "Total",
            "Self",
        };

        public const string Tooltip = "Marker timings :\n\nTotal : \tIncluding children\nSelf : \tExcluding children";
    }
}
