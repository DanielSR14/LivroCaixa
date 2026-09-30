namespace LivroCaixa.Application.Interfaces;

/// <summary>
/// Registro técnico local (arquivo em disco), para o escritório conseguir diagnosticar um erro
/// na máquina do cliente. Nunca registrar valores monetários, descrições de lançamentos ou dados
/// de documentos — só eventos técnicos (erro, exceção, versão), ver PLANO_DE_PROJETO.md, seção 7.
/// Implementações nunca lançam exceção: falhar ao registrar não pode derrubar o app.
/// </summary>
public interface IRegistroLog
{
    string DiretorioLogs { get; }

    void RegistrarInformacao(string mensagem);

    void RegistrarErro(string contexto, Exception excecao);
}
