using System.ComponentModel.DataAnnotations;
using Thunders.TechTest.ApiService.Models.Enums;

namespace Thunders.TechTest.ApiService.Models
{
    public class RelatorioProcessado
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [StringLength(50)]
        public string TipoRelatorio { get; set; } = string.Empty;

        public string? ParametrosJson { get; set; }

        public string? ResultadoJson { get; set; }

        public DateTime DataProcessamento { get; set; } = DateTime.Now;

        public int? TempoProcessamento { get; set; } // em millisegundos

        public Guid IdSolicitacao { get; set; }

        public StatusProcessamento Status { get; set; } = StatusProcessamento.Processando;

        public string? MensagemErro { get; set; }
    }
}
