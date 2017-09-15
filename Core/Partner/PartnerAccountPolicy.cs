namespace Core.Partner
{
    public class PartnerAccountPolicy : IPartnerAccountPolicy
    {
        public string PublicId { get; set; }

        public bool UseDifferentCredentials { get; set; }
    }
}
