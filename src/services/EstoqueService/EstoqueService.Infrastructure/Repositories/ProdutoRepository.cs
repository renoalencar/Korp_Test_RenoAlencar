using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EstoqueService.Domain.Entities;
using EstoqueService.Domain.Interfaces;
using EstoqueService.Infrastructure.Data;

namespace EstoqueService.Infrastructure.Repositories;

public class ProdutoRepository : IProdutoRepository
{
    private readonly EstoqueDbContext _context;
    private readonly ILogger<ProdutoRepository> _logger;

    public ProdutoRepository(
        EstoqueDbContext context,
        ILogger<ProdutoRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    // --- MÉTODOS DE LEITURA (PERMANECEM IGUAIS) ---

    public async Task<Produto?> ObterPorIdAsync(int id)
    {
        _logger.LogDebug("Repository: Buscando produto por ID {Id}", id);

        return await _context.Produtos.FindAsync(id);
    }

    public async Task<Produto?> ObterPorCodigoAsync(string codigo)
    {
        _logger.LogDebug("Repository: Buscando produto por código {Codigo}", codigo);

        return await _context.Produtos
            .FirstOrDefaultAsync(p => p.Codigo == codigo);
    }

    public async Task<(List<Produto> Items, int Total)> ObterTodosAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? ordenacao = null,
        string? busca = null)
    {
        _logger.LogDebug(
            "Repository: Buscando produtos - Página {Page}, Tamanho {Size}, Ordenação {Order}, Busca {Search}",
            pageNumber, pageSize, ordenacao ?? "padrão", busca ?? "sem filtro");

        IQueryable<Produto> query = _context.Produtos;

        if (!string.IsNullOrWhiteSpace(busca))
        {
            query = query.Where(p =>
                EF.Functions.Like(p.Codigo, $"%{busca}%") ||
                EF.Functions.Like(p.Descricao, $"%{busca}%"));
        }

        query = ordenacao?.ToLower() switch
        {
            "alfabetico" => query.OrderBy(p => p.Descricao),
            "recente" => query.OrderByDescending(p => p.DataCriacao),
            "atualizado" => query.OrderByDescending(p => p.DataAtualizacao ?? p.DataCriacao),
            _ => query.OrderBy(p => p.Descricao)
        };

        var total = await query.CountAsync();

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

        _logger.LogDebug("Repository: Retornando {Count} produtos de {Total} totais", 
            items.Count, total);

        return (items, total);
    }

    public async Task<bool> ExisteCodigoAsync(string codigo, int? excludeId = null)
    {
        _logger.LogDebug("Repository: Verificando existência do código {Codigo}", codigo);

        var query = _context.Produtos.Where(p => p.Codigo == codigo);

        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        var existe = await query.AnyAsync();

        _logger.LogDebug("Repository: Código {Codigo} existe? {Existe}", codigo, existe);

        return existe;
    }

    public async Task<bool> ProdutoTemNotasFiscaisAsync(int id)
    {
        _logger.LogDebug("Repository: Verificando se produto {Id} tem notas fiscais", id);
        
        // TODO: Implementar comunicação HTTP com FaturamentoService
        // Ou consultar banco compartilhado se houver

        // Por ora, retorna false (permite deletar qualquer produto)
        // Em produção, fazer chamada HTTP:
        // var response = await _httpClient.GetAsync($"api/notasfiscais/produto/{id}/tem-notas");
        // return response.StatusCode == HttpStatusCode.OK;
        await Task.CompletedTask;
        
        _logger.LogDebug("Repository: Verificação de notas fiscais não implementada (retornando false)");
        return false;
    }


    public async Task AdicionarAsync(Produto produto)
    {
        _logger.LogDebug("Repository: Adicionando produto {Codigo} ao contexto", produto.Codigo);
        
        await _context.Produtos.AddAsync(produto);

        // REMOVIDO: await _context.SaveChangesAsync();
    }

    public void Atualizar(Produto produto)
    {
        _logger.LogDebug("Repository: Marcando produto {Id} como modificado", produto.Id);

        _context.Produtos.Update(produto);
    }

    public void Deletar(Produto produto)
    {
        _logger.LogDebug("Repository: Marcando produto {Id} como deletado (soft delete)", produto.Id);

        // O produto já foi buscado pelo Service,
        // apenas aplicamos a lógica de soft delete.
        produto.Deletado = true;
        produto.DataDelecao = DateTime.UtcNow;

        _context.Produtos.Update(produto);

        // REMOVIDO: await _context.SaveChangesAsync();
    }
}