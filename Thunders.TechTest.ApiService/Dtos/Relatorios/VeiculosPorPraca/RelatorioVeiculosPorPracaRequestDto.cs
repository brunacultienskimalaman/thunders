namespace Thunders.TechTest.ApiService.Dtos.Relatorios.VeiculosPorPraca
{
    public class RelatorioVeiculosPorPracaRequestDto
    {
        public int? PracaId { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
    }
}
