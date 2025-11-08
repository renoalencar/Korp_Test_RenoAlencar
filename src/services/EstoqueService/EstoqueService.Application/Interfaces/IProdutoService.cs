using EstoqueService.Application.DTOs;

namespace EstoqueService.Application.Interfaces;

public interface IProdutoService
{
    Task<ProdutoResponseDTO> CriarAsync(CriarProdutoDTO dto);

    Task<ProdutoResponseDTO> AtualizarAsync(int id, AtualizarProdutoDTO dto);

    Task<bool> DeletarAsync(int id);

    Task<ProdutoResponseDTO?> ObterPorIdAsync(int id);

    Task<ProdutosPaginadosDTO> ObterTodosAsync(
        int pageNumber, 
        int pageSize, 
        string? ordenacao, 
        string? busca);

    Task<BaixaEstoqueResultDTO> BaixarEstoqueAsync(BaixaEstoqueDTO dto);
}