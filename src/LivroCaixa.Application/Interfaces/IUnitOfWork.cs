namespace LivroCaixa.Application.Interfaces;

public interface IUnitOfWork
{
    Task SalvarAlteracoesAsync();

    /// <summary>
    /// Esquece alterações rastreadas ainda não salvas. Usado depois de um erro inesperado: como o
    /// DbContext da janela principal vive o app inteiro, uma alteração que falhou ao salvar
    /// continuaria pendente e seria reenviada (e falharia de novo) em todo salvamento seguinte.
    /// </summary>
    void DescartarAlteracoesPendentes();
}
