using EstoqueService.Domain.Entities;

namespace EstoqueService.Domain.Interfaces;

public interface IProdutoRepository
{

    Task<Produto?> ObterPorIdAsync(int id);

    Task<Produto?> ObterPorCodigoAsync(string codigo);

    Task<(List<Produto> Items, int Total)> ObterTodosAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? ordenacao = null,
        string? busca = null);

    Task<bool> ExisteCodigoAsync(string codigo, int? excludeId = null);

    Task<bool> ProdutoTemNotasFiscaisAsync(int id);

    Task AdicionarAsync(Produto produto);

    void Atualizar(Produto produto);

    void Deletar(Produto produto);
}