using EstoqueService.Domain.Entities;
using EstoqueService.Domain.Interfaces;
using EstoqueService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EstoqueService.Infrastructure.Repositories;

public class OperacaoRepository : IOperacaoRepository
{
    private readonly EstoqueDbContext _context;
    private readonly ILogger<OperacaoRepository> _logger;

    public OperacaoRepository(EstoqueDbContext context, ILogger<OperacaoRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AdicionarAsync(OperacaoProcessada operacao)
    {
        _logger.LogDebug("Repository: Adicionando Operacao {Key} ao contexto", operacao.IdempotencyKey);
        await _context.OperacoesProcessadas.AddAsync(operacao);
    }

    public async Task<OperacaoProcessada?> ObterPorChaveAsync(string idempotencyKey)
    {
        _logger.LogDebug("Repository: Buscando Operacao por Chave {Key}", idempotencyKey);
        
        return await _context.OperacoesProcessadas.FindAsync(idempotencyKey);
    }
}