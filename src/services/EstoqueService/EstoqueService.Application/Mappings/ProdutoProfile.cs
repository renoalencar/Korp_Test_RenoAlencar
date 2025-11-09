using AutoMapper;
using EstoqueService.Domain.Entities;
using EstoqueService.Application.DTOs;

namespace EstoqueService.Application.Mappings;

public class ProdutoProfile : Profile
{
    public ProdutoProfile()
    {
        CreateMap<AdicionarProdutoDTO, Produto>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.DataCriacao, opt => opt.Ignore())
            .ForMember(dest => dest.DataAtualizacao, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.Deletado, opt => opt.Ignore())
            .ForMember(dest => dest.DataDelecao, opt => opt.Ignore());

        CreateMap<Produto, ProdutoResponseDTO>();

        CreateMap<AtualizarProdutoDTO, Produto>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Codigo, opt => opt.Ignore())
            .ForMember(dest => dest.DataCriacao, opt => opt.Ignore())
            .ForMember(dest => dest.DataAtualizacao, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.Deletado, opt => opt.Ignore())
            .ForMember(dest => dest.DataDelecao, opt => opt.Ignore());
    }
}