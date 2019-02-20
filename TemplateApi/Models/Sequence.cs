using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TemplateApi.Models
{
    public class Sequence
    {
        [Key]
        public int SequenceId { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Prefix { get; set; }
        [Required]
        public int Counter { get; set; }
        [Required]
        public int Length { get; set; }
        [Required]
        public string UserId { get; set; }
        public DateTime Date { get; set; }
        public string MUserId { get; set; }
        public DateTime? MDate { get; set; }
    }
}
