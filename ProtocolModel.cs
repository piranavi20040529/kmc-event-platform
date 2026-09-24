using System.ComponentModel.DataAnnotations;

namespace KMC.EventClient.Models
{
    public class ProtocolModel
    {
        [Required(ErrorMessage = "Full Name is required to agree to the protocol")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "You must agree to the KMC Protocol rules")]
        public bool AgreeToTerms { get; set; }
    }
}