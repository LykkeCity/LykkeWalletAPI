namespace Core.Partner
{
    public interface IPartnerAccountPolicy
    {
        string PublicId { get; set; }
        bool UseDifferentCredentials { get; set; }
    }
}
