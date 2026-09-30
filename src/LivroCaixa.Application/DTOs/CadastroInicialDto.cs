namespace LivroCaixa.Application.DTOs;

public record CadastroInicialDto(
    string RazaoSocial,
    string Cnpj,
    string? NomeFantasia,
    string? Segmento,
    string NomeUsuario,
    string Senha,
    string? VersaoTermoAceita = null);
