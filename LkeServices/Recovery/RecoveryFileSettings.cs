using Common.Log;
using Core.Constants;
using Core.Services.Recovery;

namespace LkeServices.Recovery
{
    public class RecoveryFileSettings : IRecoveryFileSettings
    {
        public int SelfieImageMaxSizeMBytes { get; }

        private readonly ILog _log;

        public RecoveryFileSettings(int? selfieImageMaxSizeMBytes, ILog log)
        {
            _log = log;

            if (selfieImageMaxSizeMBytes == null)
            {
                SelfieImageMaxSizeMBytes = LykkeConstants.SelfieImageMaxSizeMBytes;

                // TODO: Change for ILogFactory when migrating to new logging system.
                _log.WriteWarningAsync(
                    nameof(RecoveryFileSettings),
                    nameof(RecoveryFileSettings),
                    $"Max size for recovery selfie image is not specified in settings! Using default max image size: {LykkeConstants.SelfieImageMaxSizeMBytes}Mb.");
            }
            else
            {
                SelfieImageMaxSizeMBytes = (int) selfieImageMaxSizeMBytes;
            }
        }
    }
}