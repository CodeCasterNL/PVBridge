namespace CodeCaster.PVBridge
{
    public enum ApiResponseStatus
    {
        /// <summary>
        /// 200 OK. Doesn't always mean the JSON is parseable or present.
        /// </summary>
        Succeeded,

        /// <summary>
        /// Not 200 and not 400.
        /// </summary>
        Failed,

        /// <summary>
        /// 420/429
        /// </summary>
        RateLimited,
        
        /// <summary>
        /// For GET that's OK ("No data"), for POST not (live status date before 14 days, ...).
        /// </summary>
        BadRequest,
    }
}