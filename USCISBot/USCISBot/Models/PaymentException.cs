namespace USCISBot.Models
{
    using System;

    public class PaymentException : ApplicationException
    {
        public PaymentException(string message) : base(message)
        {
        }

        public PaymentException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}