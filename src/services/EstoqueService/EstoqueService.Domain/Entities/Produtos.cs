using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EstoqueService.Domain.Entities;

public class Produto
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Codigo { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Descricao { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "bigint")]
    public long Saldo { get; set; }

    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public DateTime? DataAtualizacao { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public bool Deletado { get; set; } = false;

    public DateTime? DataDelecao { get; set; }
}