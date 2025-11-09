using EstoqueService.Domain.Entities;

namespace EstoqueService.Domain.Interfaces;

public interface IOperacaoRepository
{
    Task<OperacaoProcessada?> ObterPorChaveAsync(string idempotencyKey);
    Task AdicionarAsync(OperacaoProcessada operacao);
}