namespace CodeCaster.PVBridge.Output
{
    public abstract class OutputBase
    {
        /// <summary>
        /// Total power in Watt hours generated for the day.
        /// </summary>
        public double? DailyGeneration { get; set; }
    }
}