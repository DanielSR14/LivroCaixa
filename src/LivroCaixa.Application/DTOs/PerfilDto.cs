using LivroCaixa.Domain.Enums;

namespace LivroCaixa.Application.DTOs;

public record PerfilDto(
    string NomeUsuario,
    PerfilUsuario PerfilUsuario,
    string NomeExibicaoEmpresa,
    string RazaoSocial,
    string Cnpj,
    string? Segmento,
    DateTime DataCadastroEmpresa);
