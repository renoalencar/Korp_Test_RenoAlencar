namespace EstoqueService.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    // Repositórios que a UoW gerencia
    IProdutoRepository Produtos { get; }
    IOperacaoRepository Operacoes { get; }

    // Método para salvar mudanças (usado pelos métodos que NÃO são transacionais)
    Task<int> CompleteAsync();

    // Métodos de transação manual (que estão causando o problema)
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();

    // --- NOVO MÉTODO (A SOLUÇÃO) ---
    /// <summary>
    /// Executa uma operação complexa dentro de uma única transação
    /// que é compatível com a estratégia de "Retry on Failure" do EF Core.
    /// </summary>
    /// <param name="operation">A lógica de negócio a ser executada.</param>
    Task ExecuteInTransactionAsync(Func<Task> operation);
}