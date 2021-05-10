using System;
using System.ComponentModel.DataAnnotations;

namespace Toy.BlazorWasm
{
    public class NewToyRequest
    {
        public int ToyId { get; set; }
        [Required]
        [MinLength(10, ErrorMessage = "Yaratıcı düşün. Güzel bir oyuncak adı ver")]
        [MaxLength(30, ErrorMessage = "O kadar da uzun bir isim olmasın")]
        public string Nickname { get; set; }
        [Required]
        [MinLength(20, ErrorMessage = "Yaratıcı düşün. Onun hakkında daha fazla şey söyle")]
        [MaxLength(250, ErrorMessage = "O kadar da uzun bir açıklama olmasın")]
        public string Description { get; set; }
        public DateTime LastUpdated { get; set; }
        public int Like { get; set; }
        public string Photo { get; set; }
    }
}
