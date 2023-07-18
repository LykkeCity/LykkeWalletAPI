namespace LykkeApi2.Models.Features
{
    public class ToggleFeatureRequest
    {
        public string FeatureName { get; set; }
        
        public string ClientId { get; set; }
        
        public bool IsEnabled { get; set; }
    }
}
