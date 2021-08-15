using System;

namespace DynamicQuery.Models
{
    public class Person
    {
        public int Id { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Employment { get; set; }
        public bool HasLicense { get; set; }
        public Address Address { get; set; }
    }
}