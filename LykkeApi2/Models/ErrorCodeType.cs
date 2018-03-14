namespace LykkeApi2.Models
{
    public enum ErrorCodeType
    {
        InvalidInputField = 0,
        /// <summary>
        /// Returns, when request is being invoked but it should not be invoked acording to the current status
        /// </summary>
        InconsistentData = 1,
        NotAuthenticated = 2,
        InvalidUsernameOrPassword = 3,
        AssetNotFound = 4,
        NotEnoughFunds = 5,
        VersionNotSupported = 6,
        RuntimeProblem = 7,
        WrongConfirmationCode = 8,
        BackupWarning = 9,
        BackupRequired = 10,
        MaintananceMode = 11,

        NoData = 12,
        ShouldOpenNewChannel = 13,
        ShouldProvideNewTempPubKey = 14,
        ShouldProcesOffchainRequest = 15,
        NoOffchainLiquidity = 16,

        GeneralError = 19,

        AddressShouldBeGenerated = 20,

        ExpiredAccessToken = 30,
        BadAccessToken = 31,
        NoEncodedMainKey = 32,
        PreviousTransactionsWereNotCompleted = 33,
        LimitationCheckFailed = 34,
        TransactionAlreadyExists = 40,
        UnknownTrustedTransferDirection = 50,
        InvalidGuidValue = 60,
        BadTempAccessToken = 61,
        NotEnoughLiquidity = 62,
        InvalidCashoutAddress = 63,
        MinVolumeViolation = 64,

        PendingDisclaimer = 70,

        BadRequest = 999,
        NotEnoughGas = 1000
    }
}