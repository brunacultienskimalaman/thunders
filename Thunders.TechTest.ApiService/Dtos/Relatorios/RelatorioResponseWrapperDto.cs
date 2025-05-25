namespace ApiService.DTOs.Relatorios;

public class RelatorioResponseWrapperDto<T>
{
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public T? Dados { get; set; }
    public DateTime DataProcessamento { get; set; }
    public int TempoProcessamentoMs { get; set; }
    public int TotalRegistros { get; set; }
    public Guid IdSolicitacao { get; set; }

    public static RelatorioResponseWrapperDto<T> ComSucesso(T dados, int tempoMs, int totalRegistros, string mensagem = "Relatório processado com sucesso")
    {
        return new RelatorioResponseWrapperDto<T>
        {
            Sucesso = true,
            Mensagem = mensagem,
            Dados = dados,
            DataProcessamento = DateTime.Now,
            TempoProcessamentoMs = tempoMs,
            TotalRegistros = totalRegistros,
            IdSolicitacao = Guid.NewGuid()
        };
    }

    public static RelatorioResponseWrapperDto<T> ComErro(string mensagem, int tempoMs = 0)
    {
        return new RelatorioResponseWrapperDto<T>
        {
            Sucesso = false,
            Mensagem = mensagem,
            DataProcessamento = DateTime.Now,
            TempoProcessamentoMs = tempoMs,
            IdSolicitacao = Guid.NewGuid()
        };
    }
}