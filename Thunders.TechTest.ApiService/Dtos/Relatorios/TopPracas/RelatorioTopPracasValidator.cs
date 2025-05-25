using FluentValidation;

namespace Thunders.TechTest.ApiService.Dtos.Relatorios.TopPracas
{
    public class RelatorioTopPracasValidator : AbstractValidator<RelatorioTopPracasRequestDto>
    {
        public RelatorioTopPracasValidator()
        {
            RuleFor(x => x.Ano)
                .GreaterThan(2020)
                .LessThanOrEqualTo(DateTime.Now.Year)
                .WithMessage($"Ano deve estar entre 2020 e {DateTime.Now.Year}");

            RuleFor(x => x.Mes)
                .InclusiveBetween(1, 12)
                .WithMessage("Mês deve estar entre 1 e 12");

            RuleFor(x => x.QuantidadeTop)
                .InclusiveBetween(1, 100)
                .WithMessage("Quantidade deve estar entre 1 e 100");

            RuleFor(x => x)
                .Must(x => new DateTime(x.Ano, x.Mes, 1) <= DateTime.Now.Date)
                .WithMessage("Não é possível consultar períodos futuros");
        }
    }
}
