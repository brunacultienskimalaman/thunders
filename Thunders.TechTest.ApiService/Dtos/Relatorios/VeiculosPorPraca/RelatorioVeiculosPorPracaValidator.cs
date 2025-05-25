using FluentValidation;

namespace Thunders.TechTest.ApiService.Dtos.Relatorios.VeiculosPorPraca
{
    public class RelatorioVeiculosPorPracaValidator : AbstractValidator<RelatorioVeiculosPorPracaRequestDto>
    {
        public RelatorioVeiculosPorPracaValidator()
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

            When(x => x.PracaId.HasValue, () => {
                RuleFor(x => x.PracaId)
                    .GreaterThan(0)
                    .WithMessage("ID da praça deve ser maior que zero");
            });
        }
    }
}
