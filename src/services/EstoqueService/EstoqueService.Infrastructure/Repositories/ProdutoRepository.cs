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

    public async Task<Produto> CriarAsync(Produto produto)
    {
        _logger.LogDebug("Repository: Criando produto {Codigo}", produto.Codigo);

        _context.Produtos.Add(produto);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Repository: Produto {Id} criado", produto.Id);

        return produto;
    }

    public async Task<Produto> AtualizarAsync(Produto produto)
    {
        _logger.LogDebug("Repository: Atualizando produto {Id}", produto.Id);

        _context.Produtos.Update(produto);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Repository: Produto {Id} atualizado", produto.Id);

        return produto;
    }

    public async Task<bool> DeletarAsync(int id)
    {
        _logger.LogDebug("Repository: Deletando produto {Id} (soft delete)", id);

        var produto = await ObterPorIdAsync(id);
        
        if (produto == null)
        {
            _logger.LogWarning("Repository: Produto {Id} não encontrado para deleção", id);
            return false;
        }

        produto.Deletado = true;
        produto.DataDelecao = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Repository: Produto {Id} deletado (soft delete)", id);

        return true;
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
}