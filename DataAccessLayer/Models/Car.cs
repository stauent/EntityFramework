using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace DataAccessLayer.Models
{
    public partial class Car
    {
        [Key]
        public int CarId { get; set; }
        [Required]
        [StringLength(128)]
        public string Make { get; set; }
        [Required]
        [StringLength(128)]
        public string Model { get; set; }
        public int Year { get; set; }
        public int Mileage { get; set; }
    }
}
