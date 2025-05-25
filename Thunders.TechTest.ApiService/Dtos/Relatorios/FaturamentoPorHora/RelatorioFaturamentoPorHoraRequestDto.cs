namespace Thunders.TechTest.ApiService.Dtos.Relatorios.FaturamentoPorHora
{
    public class RelatorioFaturamentoPorHoraRequestDto
    {
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public string? Cidade { get; set; }
    }
}
