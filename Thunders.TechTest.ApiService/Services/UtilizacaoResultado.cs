﻿namespace Thunders.TechTest.ApiService.Services
{
    public class UtilizacaoResultado
    {
        public Guid Id { get; set; }
        public DateTime DataProcessamento { get; set; }
        public bool Sucesso { get; set; }
        public string? MensagemErro { get; set; }
    }
}
