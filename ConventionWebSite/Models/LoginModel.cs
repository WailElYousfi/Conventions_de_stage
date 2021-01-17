using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ConventionWebSite.Models
{
    public class LoginModel
    {
        public LoginModel()
        {
        }

        [EmailAddress]
        [Required]
        [Display(Name = "Adresse électronique")]
        public string UserName { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "La chaîne {0} doit comporter au moins {2} caractères.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe")]
        public string Password { get; set; }
    }
}