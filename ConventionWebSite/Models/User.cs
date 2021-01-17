using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace ConventionWebSite.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Veuillez remplir ce champ")]
        [Display(Name = "Prénom")]
        public string Firstname { get; set; }

        [Required(ErrorMessage = "Veuillez remplir ce champ")]
        [Display(Name = "Nom")]
        public string Lastname { get; set; }

        [StringLength(50)]
        [Required(ErrorMessage = "Veuillez remplir ce champ")]
        [Display(Name = "Adresse électronique")]
        [EmailAddress(ErrorMessage = "Adresse e-mail invalide")]
        public string Email { get; set; }

        [StringLength(8)]
        [Required(ErrorMessage = "Veuillez remplir ce champ")]
        [Display(Name = "Code apogée")]
        public string Code { get; set; }

        [Phone]
        [Display(Name = "Numéro de téléphone")]
        public string Phone { get; set; }

        [StringLength(60, MinimumLength = 6, ErrorMessage = "Le mot de passe doit avoir une longueur minimale de 6 caractères")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Cet utilisateur est-il un administrateur ?")]
        public bool IsAdmin { get; set; }       

        [InverseProperty("Student")]
        public virtual List<Convention> ConventionsRequested { get; set; }
        [InverseProperty("Employee")]
        public virtual List<Convention> ConventionsProcessed { get; set; }

    }
}