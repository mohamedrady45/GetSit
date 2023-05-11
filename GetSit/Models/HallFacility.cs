﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GetSit.Models
{
    public class HallFacility
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [ForeignKey("HallId")]
        public int HallId { get; set; }
        [Required]
        public SpaceHall SpaceHall { get; set; }
    }
}