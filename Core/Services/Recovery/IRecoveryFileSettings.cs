namespace Core.Services.Recovery
{
    /// <summary>
    ///     Interface for accessing password recovery selfie image setting.
    /// </summary>
    public interface IRecoveryFileSettings
    {
        /// <summary>
        ///     Max size of recovery selfie image in Megabytes.
        /// </summary>
        int SelfieImageMaxSizeMBytes { get; }
    }
}