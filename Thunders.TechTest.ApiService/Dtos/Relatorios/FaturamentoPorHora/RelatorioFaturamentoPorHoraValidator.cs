using FluentValidation;

namespace Thunders.TechTest.ApiService.Dtos.Relatorios.FaturamentoPorHora
{
    public class RelatorioFaturamentoPorHoraValidator : AbstractValidator<RelatorioFaturamentoPorHoraRequestDto>
    {
        public RelatorioFaturamentoPorHoraValidator()
        {
            RuleFor(x => x.DataInicio)
                .NotEmpty()
                .WithMessage("Data de início é obrigatória");

            RuleFor(x => x.DataFim)
                .NotEmpty()
                .GreaterThan(x => x.DataInicio)
                .WithMessage("Data fim deve ser maior que data início");

            RuleFor(x => x.DataFim)
                .LessThanOrEqualTo(DateTime.Now.AddDays(1))
                .WithMessage("Data fim não pode ser futura");

            When(x => !string.IsNullOrWhiteSpace(x.Cidade), () =>
            {
                RuleFor(x => x.Cidade)
                    .MinimumLength(2)
                    .MaximumLength(100)
                    .WithMessage("Cidade deve ter entre 2 e 100 caracteres");
            });
        }
    }

}
