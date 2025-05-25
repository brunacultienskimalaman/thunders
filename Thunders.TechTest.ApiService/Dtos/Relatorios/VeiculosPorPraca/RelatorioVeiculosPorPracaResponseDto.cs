namespace Thunders.TechTest.ApiService.Dtos.Relatorios.VeiculosPorPraca
{
    public class RelatorioVeiculosPorPracaResponseDto
    {
        public int PracaId { get; set; }
        public string NomePraca { get; set; } = string.Empty;
        public string Cidade { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public List<TipoVeiculoResumoDto> TiposVeiculos { get; set; } = new();
        public int TotalUtilizacoes { get; set; }
        public decimal FaturamentoTotal { get; set; }
    }
}
