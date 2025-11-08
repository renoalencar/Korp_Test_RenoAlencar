namespace EstoqueService.Application.DTOs;

public record CriarProdutoDTO(
    string Codigo,
    string Descricao,
    long Saldo
);

public record AtualizarProdutoDTO(
    string Descricao,
    long Saldo
);

public record ProdutoResponseDTO(
    int Id,
    string Codigo,
    string Descricao,
    long Saldo,
    DateTime DataCriacao,
    DateTime? DataAtualizacao
);

public record ProdutosPaginadosDTO(
    List<ProdutoResponseDTO> Items,
    int TotalItems,
    int PageNumber,
    int PageSize,
    int TotalPages
);

public record BaixaEstoqueDTO(
    string CodigoProduto,
    long Quantidade,
    string IdempotencyKey
);

public record BaixaEstoqueResultDTO(
    bool Sucesso,
    string Mensagem,
    long SaldoAtual
);