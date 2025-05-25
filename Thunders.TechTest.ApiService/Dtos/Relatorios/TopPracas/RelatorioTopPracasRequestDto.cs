namespace Thunders.TechTest.ApiService.Dtos.Relatorios.TopPracas
{
    public class RelatorioTopPracasRequestDto
    {
        public int Ano { get; set; }
        public int Mes { get; set; }
        public int QuantidadeTop { get; set; } = 10;
    }
}
