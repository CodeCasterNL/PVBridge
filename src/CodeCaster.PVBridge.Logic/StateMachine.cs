namespace CodeCaster.PVBridge.Logic
{
    /// <summary>
    /// Poor man's
    /// </summary>
    public enum StateMachine
    {
        Undefined = 0,

        /// <summary>
        /// Snoozing (long period of zero-outputs), API error or rate limit, wait until next loop and then assume backlog.
        /// </summary>
        Wait,

        /// <summary>
        /// Only sync current status, backlog up-to-date.
        /// </summary>
        SyncLiveStatus,
        
        /// <summary>
        /// Implies SyncLiveStatus.
        /// </summary>
        SyncBacklog,
    }
}
