namespace CodeCaster.PVBridge
{
    public enum ApiResponseStatus
    {
        Succeeded,
        Failed,
        RateLimited,
        
        /// <summary>
        /// For GET that's OK ("No data"), for POST not (live status date before 14 days, ...).
        /// </summary>
        BadRequest,
    }
}