using FluentValidation;
using EstoqueService.Application.DTOs;

namespace EstoqueService.Application.Validators;

public class CriarProdutoValidator : AbstractValidator<CriarProdutoDTO>
{
    public CriarProdutoValidator()
    {
        RuleFor(p => p.Codigo)
            .NotEmpty()
            .WithMessage("O código do produto é obrigatório")
            .MaximumLength(50)
            .WithMessage("O código não pode ter mais de 50 caracteres")
            .Matches(@"^[A-Z0-9-]+$")
            .WithMessage("O código deve conter apenas letras maiúsculas, números e hífens")
            .WithName("Código");

        RuleFor(p => p.Descricao)
            .NotEmpty()
            .WithMessage("A descrição é obrigatória")
            .MinimumLength(3)
            .WithMessage("A descrição deve ter pelo menos 3 caracteres")
            .MaximumLength(200)
            .WithMessage("A descrição não pode ter mais de 200 caracteres")
            .WithName("Descrição");

        RuleFor(p => p.Saldo)
            .GreaterThanOrEqualTo(0)
            .WithMessage("O saldo (estoque) não pode ser negativo")
            .LessThanOrEqualTo(999999999999)
            .WithMessage("O saldo (estoque) não pode exceder 999.999.999.999")
            .WithName("Saldo");
    }
}

public class AtualizarProdutoValidator : AbstractValidator<AtualizarProdutoDTO>
{
    public AtualizarProdutoValidator()
    {
        RuleFor(p => p.Descricao)
            .NotEmpty()
            .WithMessage("A descrição é obrigatória")
            .MinimumLength(3)
            .WithMessage("A descrição deve ter pelo menos 3 caracteres")
            .MaximumLength(200)
            .WithMessage("A descrição não pode ter mais de 200 caracteres")
            .WithName("Descrição");

        RuleFor(p => p.Saldo)
            .GreaterThanOrEqualTo(0)
            .WithMessage("O saldo (estoque) não pode ser negativo")
            .LessThanOrEqualTo(999999999999)
            .WithMessage("O saldo (estoque) não pode exceder 999.999.999.999")
            .WithName("Saldo");
    }
}

public class BaixaEstoqueValidator : AbstractValidator<BaixaEstoqueDTO>
{
    public BaixaEstoqueValidator()
    {
        RuleFor(b => b.CodigoProduto)
            .NotEmpty()
            .WithMessage("O código do produto é obrigatório")
            .MaximumLength(50)
            .WithMessage("Código inválido")
            .WithName("CodigoProduto");

        RuleFor(b => b.Quantidade)
            .GreaterThan(0)
            .WithMessage("A quantidade deve ser maior que zero")
            .LessThanOrEqualTo(999999)
            .WithMessage("Quantidade muito alta (máximo: 999.999)")
            .WithName("Quantidade");

        RuleFor(b => b.IdempotencyKey)
            .NotEmpty()
            .WithMessage("A chave de idempotência é obrigatória")
            .MaximumLength(100)
            .WithMessage("Chave de idempotência muito longa")
            .Matches(@"^[a-zA-Z0-9-_]+$")
            .WithMessage("Chave de idempotência contém caracteres inválidos")
            .WithName("IdempotencyKey");
    }
}