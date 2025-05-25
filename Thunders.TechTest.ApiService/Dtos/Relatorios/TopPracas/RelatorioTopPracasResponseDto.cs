namespace Thunders.TechTest.ApiService.Dtos.Relatorios.TopPracas
{
    public class RelatorioTopPracasResponseDto
    {
        public int PracaId { get; set; }
        public string NomePraca { get; set; } = string.Empty;
        public string Cidade { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public decimal FaturamentoTotal { get; set; }
        public int TotalUtilizacoes { get; set; }
        public int Ranking { get; set; }
    }
}
