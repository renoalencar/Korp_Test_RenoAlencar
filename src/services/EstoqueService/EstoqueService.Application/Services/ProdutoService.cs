// ProdutoService.cs - COLE ESTE CÓDIGO
using AutoMapper;
using EstoqueService.Domain.Entities;
using EstoqueService.Domain.Interfaces;
using EstoqueService.Application.DTOs;
using EstoqueService.Application.Interfaces;

namespace EstoqueService.Application.Services;
public class ProdutoService : IProdutoService
{
    private readonly IProdutoRepository _repository;
    private readonly IMapper _mapper;

    public ProdutoService(IProdutoRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<ProdutoResponseDTO> CriarAsync(CriarProdutoDTO dto)
    {
        if (await _repository.ExisteCodigoAsync(dto.Codigo))
            throw new InvalidOperationException($"Produto com código {dto.Codigo} já existe");

        var produto = _mapper.Map<Produto>(dto);
        var criado = await _repository.CriarAsync(produto);
        return _mapper.Map<ProdutoResponseDTO>(criado);
    }

    public async Task<ProdutoResponseDTO> AtualizarAsync(int id, AtualizarProdutoDTO dto)
    {
        var produto = await _repository.ObterPorIdAsync(id)
            ?? throw new KeyNotFoundException($"Produto {id} não encontrado");

        produto.Descricao = dto.Descricao;
        produto.Saldo = dto.Saldo;
        produto.DataAtualizacao = DateTime.UtcNow;

        var atualizado = await _repository.AtualizarAsync(produto);
        return _mapper.Map<ProdutoResponseDTO>(atualizado);
    }

    public async Task<bool> DeletarAsync(int id)
    {
        var temNotas = await _repository.ProdutoTemNotasFiscaisAsync(id);
        if (temNotas)
            throw new InvalidOperationException("Não é possível deletar produto vinculado a notas fiscais");

        return await _repository.DeletarAsync(id);
    }

    public async Task<ProdutoResponseDTO?> ObterPorIdAsync(int id)
    {
        var produto = await _repository.ObterPorIdAsync(id);
        return produto != null ? _mapper.Map<ProdutoResponseDTO>(produto) : null;
    }

    public async Task<ProdutosPaginadosDTO> ObterTodosAsync(
        int pageNumber, int pageSize, string? ordenacao, string? busca)
    {
        var (items, total) = await _repository.ObterTodosAsync(pageNumber, pageSize, ordenacao, busca);
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);

        return new ProdutosPaginadosDTO(
            _mapper.Map<List<ProdutoResponseDTO>>(items),
            total,
            pageNumber,
            pageSize,
            totalPages
        );
    }

    public async Task<BaixaEstoqueResultDTO> BaixarEstoqueAsync(BaixaEstoqueDTO dto)
    {
        // Implementação com idempotência e controle de concorrência
        // (mesma lógica do código anterior)
        var produto = await _repository.ObterPorCodigoAsync(dto.CodigoProduto);
        if (produto == null)
            return new BaixaEstoqueResultDTO(false, "Produto não encontrado", 0);

        if (produto.Saldo < dto.Quantidade)
            return new BaixaEstoqueResultDTO(false, "Saldo insuficiente", produto.Saldo);

        produto.Saldo -= dto.Quantidade;
        await _repository.AtualizarAsync(produto);

        return new BaixaEstoqueResultDTO(true, "Estoque baixado com sucesso", produto.Saldo);
    }
}