using ApiService.DTOs.Relatorios;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Thunders.TechTest.ApiService.Dtos.Paginacao;
using Thunders.TechTest.ApiService.Dtos.Relatorios.FaturamentoPorHora;
using Thunders.TechTest.ApiService.Dtos.Relatorios.TopPracas;
using Thunders.TechTest.ApiService.Dtos.Relatorios.VeiculosPorPraca;
using Thunders.TechTest.ApiService.Services.Relatorios;

namespace ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class RelatoriosController : ControllerBase
{
    private readonly IRelatorioService _relatorioService;
    private readonly ILogger<RelatoriosController> _logger;
    private readonly IValidator<RelatorioFaturamentoPorHoraRequestDto> _faturamentoValidator;
    private readonly IValidator<RelatorioTopPracasRequestDto> _topPracasValidator;
    private readonly IValidator<RelatorioVeiculosPorPracaRequestDto> _veiculosValidator;

    // RelatoriosController.cs - SUBSTITUIR O CONSTRUTOR EXISTENTE

    public class RelatoriosController : ControllerBase
    {
        private readonly IRelatorioService _relatorioService;
        private readonly ILogger<RelatoriosController> _logger;

        // Validadores originais
        private readonly IValidator<RelatorioFaturamentoPorHoraRequestDto> _faturamentoValidator;
        private readonly IValidator<RelatorioTopPracasRequestDto> _topPracasValidator;
        private readonly IValidator<RelatorioVeiculosPorPracaRequestDto> _veiculosValidator;

        // Novos validadores paginados
        private readonly IValidator<PagedRelatorioFaturamentoPorHoraRequestDto> _faturamentoPaginadoValidator;
        private readonly IValidator<PagedRelatorioTopPracasRequestDto> _topPracasPaginadoValidator;
        private readonly IValidator<PagedRelatorioVeiculosPorPracaRequestDto> _veiculosPaginadoValidator;

        public RelatoriosController(
            IRelatorioService relatorioService,
            ILogger<RelatoriosController> logger,
            IValidator<RelatorioFaturamentoPorHoraRequestDto> faturamentoValidator,
            IValidator<RelatorioTopPracasRequestDto> topPracasValidator,
            IValidator<RelatorioVeiculosPorPracaRequestDto> veiculosValidator,
            IValidator<PagedRelatorioFaturamentoPorHoraRequestDto> faturamentoPaginadoValidator,
            IValidator<PagedRelatorioTopPracasRequestDto> topPracasPaginadoValidator,
            IValidator<PagedRelatorioVeiculosPorPracaRequestDto> veiculosPaginadoValidator)
        {
            _relatorioService = relatorioService;
            _logger = logger;
            _faturamentoValidator = faturamentoValidator;
            _topPracasValidator = topPracasValidator;
            _veiculosValidator = veiculosValidator;
            _faturamentoPaginadoValidator = faturamentoPaginadoValidator;
            _topPracasPaginadoValidator = topPracasPaginadoValidator;
            _veiculosPaginadoValidator = veiculosPaginadoValidator;
        }

        // ... resto dos métodos permanecem iguais ...
        /// <summary>
        /// Relatório de faturamento por hora por cidade - COM PAGINAÇÃO
        /// </summary>
        [HttpPost("faturamento-por-hora/paginado")]
    [ProducesResponseType(typeof(RelatorioResponseWrapperDto<PagedResponseDto<RelatorioFaturamentoPorHoraResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RelatorioResponseWrapperDto<object>), StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(typeof(RelatorioResponseWrapperDto<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RelatorioResponseWrapperDto<PagedResponseDto<RelatorioFaturamentoPorHoraResponseDto>>>>
        ObterFaturamentoPorHoraPaginado([FromBody] PagedRelatorioFaturamentoPorHoraRequestDto request)
    {
        _logger.LogInformation("Solicitação de relatório de faturamento PAGINADO - Página: {Page}, Tamanho: {PageSize}, DataInicio: {DataInicio}, DataFim: {DataFim}",
            request.Page, request.PageSize, request.DataInicio, request.DataFim);

        // Validação
        var validationResult = await _faturamentoPaginadoValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(CreateValidationProblem(validationResult));
        }

        // Timeout de 10 segundos
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            HttpContext.RequestAborted, timeoutCts.Token);

        try
        {
            var resultado = await _relatorioService.ObterFaturamentoPorHoraPaginadoAsync(request, combinedCts.Token);

            if (!resultado.Sucesso)
            {
                if (resultado.Mensagem.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(StatusCodes.Status408RequestTimeout, resultado);
                }
                return StatusCode(StatusCodes.Status500InternalServerError, resultado);
            }

            return Ok(resultado);
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            _logger.LogWarning("Timeout no relatório de faturamento PAGINADO");
            return StatusCode(StatusCodes.Status408RequestTimeout,
                RelatorioResponseWrapperDto<object>.ComErro("Relatório cancelado por timeout (10 segundos)"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado no relatório de faturamento PAGINADO");
            return StatusCode(StatusCodes.Status500InternalServerError,
                RelatorioResponseWrapperDto<object>.ComErro("Erro interno do servidor"));
        }
    }

    /// <summary>
    /// Relatório das top praças que mais faturaram no mês - COM PAGINAÇÃO
    /// </summary>
    [HttpPost("top-pracas-mes/paginado")]
    [ProducesResponseType(typeof(RelatorioResponseWrapperDto<PagedResponseDto<RelatorioTopPracasResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RelatorioResponseWrapperDto<object>), StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(typeof(RelatorioResponseWrapperDto<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RelatorioResponseWrapperDto<PagedResponseDto<RelatorioTopPracasResponseDto>>>>
        ObterTopPracasPorMesPaginado([FromBody] PagedRelatorioTopPracasRequestDto request)
    {
        _logger.LogInformation("Solicitação de relatório de top praças PAGINADO - Página: {Page}, Tamanho: {PageSize}, Ano: {Ano}, Mês: {Mes}",
            request.Page, request.PageSize, request.Ano, request.Mes);

        // Validação
        var validationResult = await _topPracasPaginadoValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(CreateValidationProblem(validationResult));
        }

        // Timeout de 10 segundos
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            HttpContext.RequestAborted, timeoutCts.Token);

        try
        {
            var resultado = await _relatorioService.ObterTopPracasPorMesPaginadoAsync(request, combinedCts.Token);

            if (!resultado.Sucesso)
            {
                if (resultado.Mensagem.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(StatusCodes.Status408RequestTimeout, resultado);
                }
                return StatusCode(StatusCodes.Status500InternalServerError, resultado);
            }

            return Ok(resultado);
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            _logger.LogWarning("Timeout no relatório de top praças PAGINADO");
            return StatusCode(StatusCodes.Status408RequestTimeout,
                RelatorioResponseWrapperDto<object>.ComErro("Relatório cancelado por timeout (10 segundos)"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado no relatório de top praças PAGINADO");
            return StatusCode(StatusCodes.Status500InternalServerError,
                RelatorioResponseWrapperDto<object>.ComErro("Erro interno do servidor"));
        }
    }

    /// <summary>
    /// Relatório de tipos de veículos por praça - COM PAGINAÇÃO
    /// </summary>
    [HttpPost("veiculos-por-praca/paginado")]
    [ProducesResponseType(typeof(RelatorioResponseWrapperDto<PagedResponseDto<RelatorioVeiculosPorPracaResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RelatorioResponseWrapperDto<object>), StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(typeof(RelatorioResponseWrapperDto<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RelatorioResponseWrapperDto<PagedResponseDto<RelatorioVeiculosPorPracaResponseDto>>>>
        ObterVeiculosPorPracaPaginado([FromBody] PagedRelatorioVeiculosPorPracaRequestDto request)
    {
        _logger.LogInformation("Solicitação de relatório de veículos PAGINADO - Página: {Page}, Tamanho: {PageSize}, PracaId: {PracaId}",
            request.Page, request.PageSize, request.PracaId ?? 0);

        // Validação
        var validationResult = await _veiculosPaginadoValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(CreateValidationProblem(validationResult));
        }

        // Timeout de 10 segundos
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            HttpContext.RequestAborted, timeoutCts.Token);

        try
        {
            var resultado = await _relatorioService.ObterVeiculosPorPracaPaginadoAsync(request, combinedCts.Token);

            if (!resultado.Sucesso)
            {
                if (resultado.Mensagem.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(StatusCodes.Status408RequestTimeout, resultado);
                }
                return StatusCode(StatusCodes.Status500InternalServerError, resultado);
            }

            return Ok(resultado);
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            _logger.LogWarning("Timeout no relatório de veículos PAGINADO");
            return StatusCode(StatusCodes.Status408RequestTimeout,
                RelatorioResponseWrapperDto<object>.ComErro("Relatório cancelado por timeout (10 segundos)"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado no relatório de veículos PAGINADO");
            return StatusCode(StatusCodes.Status500InternalServerError,
                RelatorioResponseWrapperDto<object>.ComErro("Erro interno do servidor"));
        }
    }

    /// <summary>
    /// Relatório das top N praças que mais faturaram no mês
    /// </summary>
    [HttpPost("top-pracas-mes")]
    [ProducesResponseType(typeof(RelatorioResponseWrapperDto<List<RelatorioTopPracasResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RelatorioResponseWrapperDto<object>), StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(typeof(RelatorioResponseWrapperDto<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RelatorioResponseWrapperDto<List<RelatorioTopPracasResponseDto>>>>
        ObterTopPracasPorMes([FromBody] RelatorioTopPracasRequestDto request)
    {
        _logger.LogInformation("Solicitação de relatório de top praças - Ano: {Ano}, Mês: {Mes}, Top: {Top}",
            request.Ano, request.Mes, request.QuantidadeTop);

        // Validação
        var validationResult = await _topPracasValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(CreateValidationProblem(validationResult));
        }

        // Timeout de 10 segundos
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            HttpContext.RequestAborted, timeoutCts.Token);

        try
        {
            var resultado = await _relatorioService.ObterTopPracasPorMesAsync(request, combinedCts.Token);

            if (!resultado.Sucesso)
            {
                if (resultado.Mensagem.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(StatusCodes.Status408RequestTimeout, resultado);
                }
                return StatusCode(StatusCodes.Status500InternalServerError, resultado);
            }

            return Ok(resultado);
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            _logger.LogWarning("Timeout no relatório de top praças");
            return StatusCode(StatusCodes.Status408RequestTimeout,
                RelatorioResponseWrapperDto<object>.ComErro("Relatório cancelado por timeout (10 segundos)"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado no relatório de top praças");
            return StatusCode(StatusCodes.Status500InternalServerError,
                RelatorioResponseWrapperDto<object>.ComErro("Erro interno do servidor"));
        }
    }

    /// <summary>
    /// Relatório de tipos de veículos por praça
    /// </summary>
    [HttpPost("veiculos-por-praca")]
    [ProducesResponseType(typeof(RelatorioResponseWrapperDto<List<RelatorioVeiculosPorPracaResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RelatorioResponseWrapperDto<object>), StatusCodes.Status408RequestTimeout)]
    [ProducesResponseType(typeof(RelatorioResponseWrapperDto<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RelatorioResponseWrapperDto<List<RelatorioVeiculosPorPracaResponseDto>>>>
        ObterVeiculosPorPraca([FromBody] RelatorioVeiculosPorPracaRequestDto request)
    {
        _logger.LogInformation("Solicitação de relatório de veículos por praça - PracaId: {PracaId}, DataInicio: {DataInicio}, DataFim: {DataFim}",
            request.PracaId ?? 0, request.DataInicio, request.DataFim);

        // Validação
        var validationResult = await _veiculosValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(CreateValidationProblem(validationResult));
        }

        // Timeout de 10 segundos
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            HttpContext.RequestAborted, timeoutCts.Token);

        try
        {
            var resultado = await _relatorioService.ObterVeiculosPorPracaAsync(request, combinedCts.Token);

            if (!resultado.Sucesso)
            {
                if (resultado.Mensagem.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(StatusCodes.Status408RequestTimeout, resultado);
                }
                return StatusCode(StatusCodes.Status500InternalServerError, resultado);
            }

            return Ok(resultado);
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            _logger.LogWarning("Timeout no relatório de veículos por praça");
            return StatusCode(StatusCodes.Status408RequestTimeout,
                RelatorioResponseWrapperDto<object>.ComErro("Relatório cancelado por timeout (10 segundos)"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado no relatório de veículos por praça");
            return StatusCode(StatusCodes.Status500InternalServerError,
                RelatorioResponseWrapperDto<object>.ComErro("Erro interno do servidor"));
        }
    }

    /// <summary>
    /// Endpoints de teste rápido para validar os relatórios
    /// </summary>
    [HttpGet("teste/faturamento-hoje")]
    [ProducesResponseType(typeof(RelatorioResponseWrapperDto<List<RelatorioFaturamentoPorHoraResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<RelatorioResponseWrapperDto<List<RelatorioFaturamentoPorHoraResponseDto>>>> TestarFaturamentoHoje()
    {
        var request = new RelatorioFaturamentoPorHoraRequestDto
        {
            DataInicio = DateTime.Today.AddDays(-7),
            DataFim = DateTime.Today.AddDays(1)
        };

        return await ObterFaturamentoPorHora(request);
    }

    [HttpGet("teste/top-pracas-mes-atual")]
    [ProducesResponseType(typeof(RelatorioResponseWrapperDto<List<RelatorioTopPracasResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<RelatorioResponseWrapperDto<List<RelatorioTopPracasResponseDto>>>> TestarTopPracasMesAtual()
    {
        var agora = DateTime.Now;
        var request = new RelatorioTopPracasRequestDto
        {
            Ano = agora.Year,
            Mes = agora.Month,
            QuantidadeTop = 5
        };

        return await ObterTopPracasPorMes(request);
    }

    [HttpGet("teste/veiculos-semana")]
    [ProducesResponseType(typeof(RelatorioResponseWrapperDto<List<RelatorioVeiculosPorPracaResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<RelatorioResponseWrapperDto<List<RelatorioVeiculosPorPracaResponseDto>>>> TestarVeiculosUltimaSemana()
    {
        var request = new RelatorioVeiculosPorPracaRequestDto
        {
            DataInicio = DateTime.Today.AddDays(-7),
            DataFim = DateTime.Today.AddDays(1)
        };

        return await ObterVeiculosPorPraca(request);
    }
    private ValidationProblemDetails CreateValidationProblem(FluentValidation.Results.ValidationResult validationResult)
    {
        var problemDetails = new ValidationProblemDetails();

        foreach (var error in validationResult.Errors)
        {
            if (problemDetails.Errors.ContainsKey(error.PropertyName))
            {
                problemDetails.Errors[error.PropertyName] = problemDetails.Errors[error.PropertyName]
                    .Concat(new[] { error.ErrorMessage }).ToArray();
            }
            else
            {
                problemDetails.Errors.Add(error.PropertyName, new[] { error.ErrorMessage });
            }
        }

        problemDetails.Title = "Erro de validação";
        problemDetails.Status = StatusCodes.Status400BadRequest;
        problemDetails.Detail = "Um ou mais campos contêm erros de validação";

        return problemDetails;
    }

} // Final da classe RelatoriosController
