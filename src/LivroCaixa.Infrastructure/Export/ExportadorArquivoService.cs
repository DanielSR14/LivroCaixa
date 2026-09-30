using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using LivroCaixa.Application.DTOs.Exportacao;
using LivroCaixa.Application.Interfaces;

namespace LivroCaixa.Infrastructure.Export;

public class ExportadorArquivoService : IExportadorArquivoService
{
    private static readonly JsonSerializerOptions OpcoesJson = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <remarks>
    /// Roda num thread do pool pelo mesmo motivo do backup (<see cref="Files.BackupArquivoService"/>):
    /// compactar os comprovantes do mês é síncrono e travaria a janela de exportação.
    /// Recebe só DTOs já montados, sem acesso ao DbContext.
    /// </remarks>
    public Task<string> GerarArquivoAsync(
        PacoteExportacaoDto pacote,
        IReadOnlyList<InstrucaoCopiaAnexoDto> anexos,
        string diretorioDestino) =>
        Task.Run(() => GerarArquivo(pacote, anexos, diretorioDestino));

    private static string GerarArquivo(
        PacoteExportacaoDto pacote,
        IReadOnlyList<InstrucaoCopiaAnexoDto> anexos,
        string diretorioDestino)
    {
        Directory.CreateDirectory(diretorioDestino);

        var cnpjSanitizado = new string(pacote.Cnpj.Where(char.IsLetterOrDigit).ToArray());
        var nomeArquivo = $"LivroCaixa_{cnpjSanitizado}_{pacote.Ano:D4}-{pacote.Mes:D2}.contabil";
        var caminhoCompleto = Path.Combine(diretorioDestino, nomeArquivo);

        if (File.Exists(caminhoCompleto))
            File.Delete(caminhoCompleto);

        using (var arquivoZip = ZipFile.Open(caminhoCompleto, ZipArchiveMode.Create))
        {
            EscreverEntradaJson(arquivoZip, "manifest.json", pacote.Manifesto);
            EscreverEntradaJson(arquivoZip, "empresa.json", pacote.Empresa);
            EscreverEntradaJson(arquivoZip, "lancamentos.json", pacote.Lancamentos);
            EscreverEntradaTexto(arquivoZip, "lancamentos.csv", GerarCsv(pacote.Lancamentos), Utf8ComBom);
            EscreverEntradaJson(arquivoZip, "historico.json", pacote.Historico);
            EscreverEntradaJson(arquivoZip, "transferencias.json", pacote.Transferencias);

            foreach (var anexo in anexos)
            {
                if (File.Exists(anexo.CaminhoOrigemCompleto))
                    arquivoZip.CreateEntryFromFile(anexo.CaminhoOrigemCompleto, $"anexos/{anexo.NomeDestino}");
            }
        }

        return caminhoCompleto;
    }

    // JSON sem BOM (a RFC 8259 proíbe e leitores estritos rejeitam); CSV com BOM, que é o que faz o
    // Excel reconhecer os acentos de um CSV em UTF-8.
    private static readonly Encoding Utf8SemBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly Encoding Utf8ComBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    private static void EscreverEntradaJson(ZipArchive arquivoZip, string nomeEntrada, object dados) =>
        EscreverEntradaTexto(arquivoZip, nomeEntrada, JsonSerializer.Serialize(dados, OpcoesJson), Utf8SemBom);

    private static void EscreverEntradaTexto(ZipArchive arquivoZip, string nomeEntrada, string conteudo, Encoding codificacao)
    {
        var entrada = arquivoZip.CreateEntry(nomeEntrada);
        using var escritor = new StreamWriter(entrada.Open(), codificacao);
        escritor.Write(conteudo);
    }

    private static string GerarCsv(IReadOnlyList<LancamentoExportacaoDto> lancamentos)
    {
        var construtor = new StringBuilder();
        construtor.AppendLine("Data;Tipo;Valor;Categoria;FormaPagamento;Descricao;Observacoes;Anexos");

        foreach (var lancamento in lancamentos)
        {
            construtor.AppendLine(string.Join(';',
                CampoCsv(lancamento.Data),
                CampoCsv(lancamento.Tipo),
                CampoCsv(lancamento.Valor.ToString(CultureInfo.InvariantCulture)),
                CampoCsv(lancamento.Categoria),
                CampoCsv(lancamento.FormaPagamento),
                CampoCsv(lancamento.Descricao ?? string.Empty),
                CampoCsv(lancamento.Observacoes ?? string.Empty),
                CampoCsv(string.Join(", ", lancamento.NomesAnexos))));
        }

        return construtor.ToString();
    }

    private static readonly char[] InicioDeFormula = { '=', '+', '-', '@', '\t', '\r' };

    /// <summary>
    /// Campo entre aspas (aspas internas duplicadas). Texto que começa com =, +, -, @ ganha um
    /// apóstrofo na frente: sem isso o Excel do escritório executaria o texto como fórmula ao abrir
    /// o CSV (injeção de fórmula — ex: uma descrição "=HYPERLINK(...)"). Valores numéricos nunca
    /// começam com esses caracteres (o valor do lançamento é sempre positivo).
    /// </summary>
    private static string CampoCsv(string valor)
    {
        var seguro = valor.Length > 0 && InicioDeFormula.Contains(valor[0]) ? "'" + valor : valor;
        return $"\"{seguro.Replace("\"", "\"\"")}\"";
    }
}
