namespace Thunders.TechTest.ApiService.Dtos.Relatorios.FaturamentoPorHora
{
    public class RelatorioFaturamentoPorHoraResponseDto
    {
        public string Cidade { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime DataHora { get; set; }
        public decimal ValorTotal { get; set; }
        public int TotalPassagens { get; set; }
    }
}
