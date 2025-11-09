using Xunit;
using Moq;
using AutoMapper;
using Microsoft.Extensions.Logging;
using EstoqueService.Domain.Entities;
using EstoqueService.Domain.Interfaces;
using EstoqueService.Application.DTOs;
using EstoqueService.Application.Services;
using EstoqueService.Application.Mappings;

namespace EstoqueService.Tests.Unit;


public class ProdutoServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IProdutoRepository> _mockProdutoRepository;
    private readonly Mock<IOperacaoRepository> _mockOperacaoRepository;
    
    private readonly IMapper _mapper;
    private readonly Mock<ILogger<ProdutoService>> _mockLogger;
    private readonly ProdutoService _service;

    public ProdutoServiceTests()
    {
        _mockProdutoRepository = new Mock<IProdutoRepository>();
        _mockOperacaoRepository = new Mock<IOperacaoRepository>();

        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _mockUnitOfWork.Setup(u => u.Produtos).Returns(_mockProdutoRepository.Object);
        _mockUnitOfWork.Setup(u => u.Operacoes).Returns(_mockOperacaoRepository.Object);

        var configExpression = new MapperConfigurationExpression();
        configExpression.AddProfile<ProdutoProfile>();

        var config = new MapperConfiguration(configExpression, null); 
        _mapper = config.CreateMapper(); // Inicializa o _mapper corretamente

        _mockLogger = new Mock<ILogger<ProdutoService>>();

        _service = new ProdutoService(_mockUnitOfWork.Object, _mapper, _mockLogger.Object);
    }

    [Fact]
    public async Task AdicionarAsync_ComCodigoValido_DeveAdicionarProdutoESalvar()
    {
        var dto = new AdicionarProdutoDTO("PROD-001", "Notebook", 10);
        var produto = new Produto { Codigo = "PROD-001", Descricao = "Notebook", Saldo = 10 };

        _mockProdutoRepository
            .Setup(r => r.ExisteCodigoAsync(dto.Codigo, null))
            .ReturnsAsync(false);

        _mockProdutoRepository
            .Setup(r => r.AdicionarAsync(It.IsAny<Produto>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.CompleteAsync())
            .ReturnsAsync(1);

        var result = await _service.AdicionarAsync(dto);

        Assert.NotNull(result);
        Assert.Equal(dto.Codigo, result.Codigo);
        Assert.Equal(dto.Descricao, result.Descricao);

        _mockProdutoRepository.Verify(r => r.ExisteCodigoAsync(dto.Codigo, null), Times.Once);
        _mockProdutoRepository.Verify(r => r.AdicionarAsync(It.Is<Produto>(p => p.Codigo == dto.Codigo)), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task AdicionarAsync_ComCodigoDuplicado_DeveLancarExcecaoENaoSalvar()
    {
        var dto = new AdicionarProdutoDTO("PROD-001", "Notebook", 10);

        _mockProdutoRepository
            .Setup(r => r.ExisteCodigoAsync(dto.Codigo, null))
            .ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AdicionarAsync(dto));

        Assert.Contains("já existe", exception.Message);

        _mockProdutoRepository.Verify(r => r.AdicionarAsync(It.IsAny<Produto>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
    }

    [Fact]
    public async Task AtualizarAsync_ComIdValido_DeveAtualizarProdutoESalvar()
    {
        var produtoExistente = new Produto
        {
            Id = 1, Codigo = "PROD-001", Descricao = "Notebook", Saldo = 10
        };
        var dto = new AtualizarProdutoDTO("Notebook Dell", 8);

        _mockProdutoRepository
            .Setup(r => r.ObterPorIdAsync(1))
            .ReturnsAsync(produtoExistente);

        _mockProdutoRepository
            .Setup(r => r.Atualizar(It.IsAny<Produto>()));

        _mockUnitOfWork
            .Setup(u => u.CompleteAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _service.AtualizarAsync(1, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Notebook Dell", result.Descricao);
        Assert.Equal(8, result.Saldo);

        _mockProdutoRepository.Verify(r => r.ObterPorIdAsync(1), Times.Once);
        _mockProdutoRepository.Verify(r => r.Atualizar(It.Is<Produto>(p => p.Descricao == "Notebook Dell")), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task DeletarAsync_ProdutoSemNotas_DeveDeletarESalvar()
    {
        var produtoExistente = new Produto { Id = 1 };

        _mockProdutoRepository
            .Setup(r => r.ObterPorIdAsync(1))
            .ReturnsAsync(produtoExistente);

        _mockProdutoRepository
            .Setup(r => r.ProdutoTemNotasFiscaisAsync(1))
            .ReturnsAsync(false);

        _mockProdutoRepository
            .Setup(r => r.Deletar(It.IsAny<Produto>()));

        _mockUnitOfWork
            .Setup(u => u.CompleteAsync())
            .ReturnsAsync(1); // Simula 1 linha afetada

        var result = await _service.DeletarAsync(1);

        Assert.True(result);
        _mockProdutoRepository.Verify(r => r.ObterPorIdAsync(1), Times.Once);
        _mockProdutoRepository.Verify(r => r.ProdutoTemNotasFiscaisAsync(1), Times.Once);
        _mockProdutoRepository.Verify(r => r.Deletar(produtoExistente), Times.Once);
        _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task BaixarEstoqueAsync_ComSaldoSuficiente_DeveExecutarNaTransicao()
    {
        var produto = new Produto { Id = 1, Codigo = "PROD-001", Descricao = "Notebook", Saldo = 10 };
        var dto = new BaixaEstoqueDTO("PROD-001", 3, "test-key-123");

        _mockOperacaoRepository
            .Setup(o => o.ObterPorChaveAsync(dto.IdempotencyKey))
            .ReturnsAsync((OperacaoProcessada?)null);

        _mockUnitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Callback<Func<Task>>(async (operation) => await operation());

        _mockProdutoRepository
            .Setup(r => r.ObterPorCodigoAsync(dto.CodigoProduto))
            .ReturnsAsync(produto);

        _mockProdutoRepository
            .Setup(r => r.Atualizar(It.IsAny<Produto>()));

        _mockOperacaoRepository
            .Setup(o => o.AdicionarAsync(It.IsAny<OperacaoProcessada>()))
            .Returns(Task.CompletedTask);

        var result = await _service.BaixarEstoqueAsync(dto);

        Assert.True(result.Sucesso);
        Assert.Equal(7, result.SaldoAtual);
        Assert.Equal("Estoque baixado com sucesso", result.Mensagem);

        _mockUnitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()), Times.Once);
        
        _mockProdutoRepository.Verify(r => r.Atualizar(It.Is<Produto>(p => p.Saldo == 7)), Times.Once);
        _mockOperacaoRepository.Verify(o => o.AdicionarAsync(It.Is<OperacaoProcessada>(op => op.IdempotencyKey == dto.IdempotencyKey)), Times.Once);
        
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task BaixarEstoqueAsync_ComSaldoInsuficiente_DeveLancarExcecaoNaTransicaoERetornarFalha()
    {
        var produto = new Produto { Id = 1, Codigo = "PROD-001", Descricao = "Notebook", Saldo = 2 };
        var dto = new BaixaEstoqueDTO("PROD-001", 5, "test-key-insuficiente");

        _mockOperacaoRepository
            .Setup(o => o.ObterPorChaveAsync(dto.IdempotencyKey))
            .ReturnsAsync((OperacaoProcessada?)null);

        _mockUnitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Callback<Func<Task>>(async (operation) => await operation()); // Executa a lógica

        _mockProdutoRepository
            .Setup(r => r.ObterPorCodigoAsync(dto.CodigoProduto))
            .ReturnsAsync(produto); // Retorna saldo 2

        var result = await _service.BaixarEstoqueAsync(dto);

        Assert.False(result.Sucesso);
        Assert.Contains("Saldo insuficiente", result.Mensagem);
        Assert.Equal(2, result.SaldoAtual);

        _mockUnitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()), Times.Once);
        
        _mockProdutoRepository.Verify(r => r.Atualizar(It.IsAny<Produto>()), Times.Never);
    }

    [Fact]
    public async Task BaixarEstoqueAsync_ChaveDuplicada_DeveRetornarSucessoImediatamente()
    {
        var produto = new Produto { Id = 1, Codigo = "PROD-001", Descricao = "Notebook", Saldo = 10 };
        var dto = new BaixaEstoqueDTO("PROD-001", 3, "test-key-duplicada");
        var operacaoExistente = new OperacaoProcessada { IdempotencyKey = dto.IdempotencyKey };

        _mockOperacaoRepository
            .Setup(o => o.ObterPorChaveAsync(dto.IdempotencyKey))
            .ReturnsAsync(operacaoExistente);
        
        _mockProdutoRepository
            .Setup(r => r.ObterPorCodigoAsync(dto.CodigoProduto))
            .ReturnsAsync(produto);

        var result = await _service.BaixarEstoqueAsync(dto);

        Assert.True(result.Sucesso);
        Assert.Contains("Requisição já processada", result.Mensagem);
        Assert.Equal(10, result.SaldoAtual); // Saldo não mudou

        _mockUnitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()), Times.Never);
    }
}