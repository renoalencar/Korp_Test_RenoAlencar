using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EstoqueService.Domain.Entities;

public class OperacaoProcessada
{
    [Key]
    [MaxLength(100)]
    public string IdempotencyKey { get; set; } = string.Empty;

    public DateTime DataProcessamento { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    public string TipoOperacao { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Resultado { get; set; } = string.Empty;
}