using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using EstoqueService.Application.DTOs;
using EstoqueService.Application.Interfaces;

namespace EstoqueService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProdutosController : ControllerBase
{
    private readonly IProdutoService _service;
    private readonly IValidator<AdicionarProdutoDTO> _criarValidator;
    private readonly IValidator<AtualizarProdutoDTO> _atualizarValidator;
    private readonly IValidator<BaixaEstoqueDTO> _baixaValidator;
    private readonly ILogger<ProdutosController> _logger;

    public ProdutosController(
        IProdutoService service,
        IValidator<AdicionarProdutoDTO> criarValidator,
        IValidator<AtualizarProdutoDTO> atualizarValidator,
        IValidator<BaixaEstoqueDTO> baixaValidator,
        ILogger<ProdutosController> logger)
    {
        _service = service;
        _criarValidator = criarValidator;
        _atualizarValidator = atualizarValidator;
        _baixaValidator = baixaValidator;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ProdutosPaginadosDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProdutosPaginadosDTO>> ObterTodos(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? ordenacao = null,
        [FromQuery] string? busca = null)
    {
        try
        {
            if (pageNumber < 1)
            {
                return BadRequest(new { mensagem = "Número da página deve ser maior ou igual a 1" });
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { mensagem = "Tamanho da página deve estar entre 1 e 100" });
            }

            _logger.LogInformation(
                "GET /api/produtos - Page: {Page}, Size: {Size}, Order: {Order}, Search: {Search}",
                pageNumber, pageSize, ordenacao ?? "padrão", busca ?? "sem filtro");

            var result = await _service.ObterTodosAsync(pageNumber, pageSize, ordenacao, busca);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter produtos");
            return StatusCode(500, new { mensagem = "Erro interno ao processar requisição" });
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProdutoResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProdutoResponseDTO>> ObterPorId(int id)
    {
        try
        {
            _logger.LogInformation("GET /api/produtos/{Id}", id);

            var produto = await _service.ObterPorIdAsync(id);

            if (produto == null)
            {
                _logger.LogWarning("Produto {Id} não encontrado", id);
                return NotFound(new { mensagem = $"Produto com ID {id} não encontrado" });
            }

            return Ok(produto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter produto {Id}", id);
            return StatusCode(500, new { mensagem = "Erro interno ao processar requisição" });
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProdutoResponseDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProdutoResponseDTO>> Adicionar([FromBody] AdicionarProdutoDTO dto)
    {
        try
        {
            _logger.LogInformation("POST /api/produtos - Código: {Codigo}", dto.Codigo);

            var validationResult = await _criarValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var erros = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("Validação falhou ao criar produto: {Erros}", string.Join(", ", erros));
                return BadRequest(new { mensagem = "Dados inválidos", erros });
            }

            var produto = await _service.AdicionarAsync(dto);

            _logger.LogInformation("Produto {Id} criado com sucesso", produto.Id);

            return CreatedAtAction(
                nameof(ObterPorId),
                new { id = produto.Id },
                produto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Erro de negócio ao criar produto");
            return BadRequest(new { mensagem = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar produto");
            return StatusCode(500, new { mensagem = "Erro interno ao processar requisição" });
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProdutoResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProdutoResponseDTO>> Atualizar(
        int id,
        [FromBody] AtualizarProdutoDTO dto)
    {
        try
        {
            _logger.LogInformation("PUT /api/produtos/{Id}", id);

            var validationResult = await _atualizarValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var erros = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(new { mensagem = "Dados inválidos", erros });
            }

            var produto = await _service.AtualizarAsync(id, dto);

            _logger.LogInformation("Produto {Id} atualizado com sucesso", id);

            return Ok(produto);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Produto {Id} não encontrado", id);
            return NotFound(new { mensagem = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("concorrência"))
        {
            _logger.LogWarning(ex, "Conflito de concorrência ao atualizar produto {Id}", id);
            return Conflict(new { mensagem = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar produto {Id}", id);
            return StatusCode(500, new { mensagem = "Erro interno ao processar requisição" });
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deletar(int id)
    {
        try
        {
            _logger.LogInformation("DELETE /api/produtos/{Id}", id);

            var deletado = await _service.DeletarAsync(id);

            if (!deletado)
            {
                return NotFound(new { mensagem = $"Produto com ID {id} não encontrado" });
            }

            _logger.LogInformation("Produto {Id} deletado com sucesso", id);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Não foi possível deletar produto {Id}", id);
            return BadRequest(new { mensagem = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar produto {Id}", id);
            return StatusCode(500, new { mensagem = "Erro interno ao processar requisição" });
        }
    }

    [HttpPost("baixar-estoque")]
    [ProducesResponseType(typeof(BaixaEstoqueResultDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BaixaEstoqueResultDTO>> BaixarEstoque(
        [FromBody] BaixaEstoqueDTO dto)
    {
        try
        {
            _logger.LogInformation(
                "POST /api/produtos/baixar-estoque - Produto: {Codigo}, Qtd: {Qtd}, Key: {Key}",
                dto.CodigoProduto, dto.Quantidade, dto.IdempotencyKey);

            var validationResult = await _baixaValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var erros = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(new { mensagem = "Dados inválidos", erros });
            }

            var resultado = await _service.BaixarEstoqueAsync(dto);

            if (!resultado.Sucesso)
            {
                _logger.LogWarning(
                    "Falha ao baixar estoque: {Mensagem} (Produto: {Codigo})",
                    resultado.Mensagem, dto.CodigoProduto);
                
                return BadRequest(resultado);
            }

            return Ok(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao baixar estoque");
            return StatusCode(500, new BaixaEstoqueResultDTO(
                false,
                "Erro interno ao processar requisição",
                0));
        }
    }
}