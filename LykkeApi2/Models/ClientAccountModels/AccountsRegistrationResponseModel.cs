namespace LykkeApi2.Models.ClientAccountModels
{
    public class AccountsRegistrationResponseModel
    {
        public string Token { get; set; }
        public string NotificationsId { get; set; }
        public ApiPersonalDataModel PersonalData { get; set; }
        public bool CanCashInViaBankCard { get; set; }
        public bool SwiftDepositEnabled { get; set; }
    }
}
