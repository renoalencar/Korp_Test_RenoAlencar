using EstoqueService.Domain.Interfaces;
// using EstoqueService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;

namespace EstoqueService.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly EstoqueDbContext _context;
    private IDbContextTransaction? _transaction;

    public IProdutoRepository Produtos { get; }
    public IOperacaoRepository Operacoes { get; }

    public UnitOfWork(
        EstoqueDbContext context,
        IProdutoRepository produtoRepository,
        IOperacaoRepository operacaoRepository)
    {
        _context = context;
        Produtos = produtoRepository;
        Operacoes = operacaoRepository;
    }

    // --- IMPLEMENTAÇÃO DO NOVO MÉTODO (A SOLUÇÃO) ---
    public async Task ExecuteInTransactionAsync(Func<Task> operation)
    {
        // 1. Cria a estratégia de execução (como o erro pediu)
        var strategy = _context.Database.CreateExecutionStrategy();

        // 2. Executa a estratégia
        await strategy.ExecuteAsync(async () =>
        {
            // 3. Inicia a transação manual DENTRO da estratégia
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 4. Executa a lógica de negócio (passada pelo ProdutoService)
                await operation();
                
                // 5. Salva as mudanças no DbContext (chama o SaveChanges)
                await CompleteAsync();

                // 6. Commita a transação
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                // 7. Reverte em caso de erro
                await transaction.RollbackAsync();
                throw; // Lança a exceção para o service/controller
            }
        });
    }

    // --- MÉTODOS ANTIGOS (Permanecem) ---

    public async Task<int> CompleteAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        try
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
            }
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null; 
            }
        }
    }

    public async Task RollbackAsync()
    {
        try
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
            }
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}