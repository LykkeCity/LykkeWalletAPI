namespace Core.PaymentSystem
{
    public class OtherPaymentInfo
    {

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string City { get; set; }

        public string Zip { get; set; }

        public string Address { get; set; }

        public string Country { get; set; }

        public string Email { get; set; }

        public string ContactPhone { get; set; }

        public string DateOfBirth { get; set; }

        public static OtherPaymentInfo Create(
            string firstName,
            string lastName,
            string city,
            string zip,
            string address,
            string country,
            string email,
            string contactPhone,
            string dateOfBirth)
        {
            return new OtherPaymentInfo
            {
                FirstName = firstName,
                LastName = lastName,
                City = city,
                Zip = zip,
                Address = address,
                Country = country,
                Email = email,
                ContactPhone = contactPhone,
                DateOfBirth = dateOfBirth,
            };
        }
    }
}