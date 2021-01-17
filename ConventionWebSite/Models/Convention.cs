using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace ConventionWebSite.Models
{
    public class Convention
    {

        [Key]
        public int ConventionId { get; set; }

        [Required(ErrorMessage = "Veuillez remplir ce champ")]
        [Display(Name = "Nom de l'entreprise")]
        public string CompanieName { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date de début de stage")]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date de fin de stage")]
        public DateTime EndDate { get; set; }

        public DateTime? RequestDate { get; set; }
        public DateTime? AcceptanceDate { get; set; }

        public string State { get; set; }

        public int? StudentId { get; set; }
        public int? EmployeeId { get; set; }

        [ForeignKey("StudentId")]
        public virtual User Student { get; set; }
        [ForeignKey("EmployeeId")]
        public virtual User Employee { get; set; }

        [NotMapped]
        public List<User> UserCollection { get; set; }
    }
}