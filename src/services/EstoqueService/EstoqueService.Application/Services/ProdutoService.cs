using AutoMapper;
using EstoqueService.Domain.Entities;
using EstoqueService.Domain.Interfaces;
using EstoqueService.Application.DTOs;
using EstoqueService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace EstoqueService.Application.Services;
public class ProdutoService : IProdutoService
{
    private readonly IUnitOfWork _unitOfWork; 
    private readonly IMapper _mapper;
    private readonly ILogger<ProdutoService> _logger;

    public ProdutoService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<ProdutoService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    // --- MÉTODO ATUALIZADO (BaixarEstoqueAsync) ---

    public async Task<BaixaEstoqueResultDTO> BaixarEstoqueAsync(BaixaEstoqueDTO dto)
    {
        // PASSO 1: Verificação de Idempotência (fora da transação)
        var operacaoExistente = await _unitOfWork.Operacoes.ObterPorChaveAsync(dto.IdempotencyKey);
        if (operacaoExistente != null)
        {
            _logger.LogWarning("Requisição duplicada (Idempotência): {Key}", dto.IdempotencyKey);
            var produtoAtual = await _unitOfWork.Produtos.ObterPorCodigoAsync(dto.CodigoProduto);
            return new BaixaEstoqueResultDTO(true, "Requisição já processada", produtoAtual?.Saldo ?? 0);
        }

        // PASSO 2: Executar a lógica de negócio dentro da transação atômica
        try
        {
            // O UoW agora gerencia a transação E a estratégia de retry
            // Removemos as chamadas BeginTransaction, Commit e Rollback daqui
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // --- Toda a lógica de negócio vai aqui dentro ---
                
                var produto = await _unitOfWork.Produtos.ObterPorCodigoAsync(dto.CodigoProduto);

                if (produto == null)
                {
                    // Lança uma exceção para acionar o Rollback
                    throw new KeyNotFoundException("Produto não encontrado");
                }

                if (produto.Saldo < dto.Quantidade)
                {
                    // Lança uma exceção para acionar o Rollback
                    throw new InvalidOperationException("Saldo insuficiente");
                }

                produto.Saldo -= dto.Quantidade;
                _unitOfWork.Produtos.Atualizar(produto);

                var operacao = new OperacaoProcessada
                {
                    IdempotencyKey = dto.IdempotencyKey,
                    TipoOperacao = "BaixaEstoque",
                    Resultado = $"Sucesso: Saldo anterior: {produto.Saldo + dto.Quantidade}, Saldo atual: {produto.Saldo}"
                };
                await _unitOfWork.Operacoes.AdicionarAsync(operacao);
                
                // O 'CompleteAsync' (SaveChanges) será chamado pelo UnitOfWork
            });

            // Se chegou aqui, a transação foi commitada com sucesso
            _logger.LogInformation("Estoque baixado para {Codigo} (Chave: {Key})", dto.CodigoProduto, dto.IdempotencyKey);
            var produtoFinal = await _unitOfWork.Produtos.ObterPorCodigoAsync(dto.CodigoProduto);
            return new BaixaEstoqueResultDTO(true, "Estoque baixado com sucesso", produtoFinal!.Saldo);
        }
        // PASSO 3: Capturar exceções de negócio para retornar DTOs amigáveis
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Falha na baixa de estoque (Chave: {Key})", dto.IdempotencyKey);
            return new BaixaEstoqueResultDTO(false, ex.Message, 0);
        }
        catch (InvalidOperationException ex) // Captura "Saldo insuficiente"
        {
            _logger.LogWarning(ex, "Falha na baixa de estoque (Chave: {Key})", dto.IdempotencyKey);
            var produtoAtual = await _unitOfWork.Produtos.ObterPorCodigoAsync(dto.CodigoProduto);
            return new BaixaEstoqueResultDTO(false, ex.Message, produtoAtual?.Saldo ?? 0);
        }
        catch (Exception ex)
        {
            // Erro inesperado (captura o 'throw' do UoW)
            _logger.LogError(ex, "Erro na transação de baixa de estoque: {Key}", dto.IdempotencyKey);
            // Lança para o controller capturar e retornar 500
            throw; 
        }
    }

    // --- MÉTODOS NÃO TRANSACIONAIS (Permanecem iguais) ---
    // (Eles não usam BeginTransaction, então não causam o erro)

    public async Task<ProdutoResponseDTO> AdicionarAsync(AdicionarProdutoDTO dto)
    {
        if (await _unitOfWork.Produtos.ExisteCodigoAsync(dto.Codigo))
            throw new InvalidOperationException($"Produto com código {dto.Codigo} já existe");

        var produto = _mapper.Map<Produto>(dto);
        await _unitOfWork.Produtos.AdicionarAsync(produto); 
        await _unitOfWork.CompleteAsync(); 
        return _mapper.Map<ProdutoResponseDTO>(produto);
    }

    public async Task<ProdutoResponseDTO> AtualizarAsync(int id, AtualizarProdutoDTO dto)
    {
        var produto = await _unitOfWork.Produtos.ObterPorIdAsync(id)
            ?? throw new KeyNotFoundException($"Produto {id} não encontrado");

        produto.Descricao = dto.Descricao;
        produto.Saldo = dto.Saldo;
        _unitOfWork.Produtos.Atualizar(produto);
        await _unitOfWork.CompleteAsync(); 
        return _mapper.Map<ProdutoResponseDTO>(produto);
    }

    public async Task<bool> DeletarAsync(int id)
    {
        var produto = await _unitOfWork.Produtos.ObterPorIdAsync(id);
        if (produto == null)
            return false;

        var temNotas = await _unitOfWork.Produtos.ProdutoTemNotasFiscaisAsync(id);
        if (temNotas)
            throw new InvalidOperationException("Não é possível deletar produto vinculado a notas fiscais");

        _unitOfWork.Produtos.Deletar(produto);
        var changes = await _unitOfWork.CompleteAsync(); 
        return changes > 0;
    }

    public async Task<ProdutoResponseDTO?> ObterPorIdAsync(int id)
    {
        var produto = await _unitOfWork.Produtos.ObterPorIdAsync(id);
        return produto != null ? _mapper.Map<ProdutoResponseDTO>(produto) : null;
    }

    public async Task<ProdutosPaginadosDTO> ObterTodosAsync(
        int pageNumber, int pageSize, string? ordenacao, string? busca)
    {
        var (items, total) = await _unitOfWork.Produtos.ObterTodosAsync(pageNumber, pageSize, ordenacao, busca);
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);

        return new ProdutosPaginadosDTO(
            _mapper.Map<List<ProdutoResponseDTO>>(items),
            total,
            pageNumber,
            pageSize,
            totalPages
        );
    }
}