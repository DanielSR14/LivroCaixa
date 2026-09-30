using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using LivroCaixa.Application.Services;
using LivroCaixa.Domain.Entities;
using LivroCaixa.Domain.Enums;
using LivroCaixa.Infrastructure.Database;
using LivroCaixa.Infrastructure.Export;
using LivroCaixa.Infrastructure.Files;
using LivroCaixa.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LivroCaixa.Tests.Infrastructure;

/// <summary>
/// Contrato do arquivo .contabil com a ferramenta do escritório (Python) e com o Excel: formato de
/// datas/valores independente da cultura do Windows, codificação, escape de CSV, e consistência
/// entre o que o JSON diz e o que o zip contém.
/// </summary>
public class ExportacaoFormatoTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly LivroCaixaDbContext _context;
    private readonly string _diretorioDestino = Path.Combine(Path.GetTempPath(), $"contabil_formato_{Guid.NewGuid():N}");
    private readonly Empresa _empresa;
    private readonly Conta _caixa;
    private readonly Conta _banco;
    private readonly Categoria _categoria;
    private readonly DateOnly _data = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    public ExportacaoFormatoTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _context = new LivroCaixaDbContext(new DbContextOptionsBuilder<LivroCaixaDbContext>().UseSqlite(_connection).Options);
        _context.Database.EnsureCreated();

        _empresa = new Empresa("Associação dos Moradores", "12.345.678/0001-90");
        _caixa = new Conta(_empresa.Id, "Caixa", TipoConta.Caixa);
        _banco = new Conta(_empresa.Id, "Sicoob", TipoConta.Banco);
        _categoria = new Categoria(_empresa.Id, "Contribuições", TipoLancamento.Entrada);
        _context.AddRange(_empresa, _caixa, _banco, _categoria);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
        if (Directory.Exists(_diretorioDestino))
            Directory.Delete(_diretorioDestino, recursive: true);
    }

    private Lancamento CriarLancamento(decimal valor, string? descricao)
    {
        var lancamento = new Lancamento(_empresa.Id, _caixa, _categoria, TipoLancamento.Entrada, _data, valor, FormaPagamento.Pix, descricao);
        _context.Lancamentos.Add(lancamento);
        _context.SaveChanges();
        return lancamento;
    }

    private Task<string> ExportarAsync() =>
        new ExportacaoService(
                new EmpresaRepository(_context), new ContaRepository(_context), new CategoriaRepository(_context),
                new LancamentoRepository(_context), new ArmazenamentoAnexoService(), new ExportadorArquivoService(),
                new RemessaExportacaoRepository(_context), new TransferenciaRepository(_context), _context)
            .ExportarAsync(_empresa.Id, _data.Year, _data.Month, _diretorioDestino);

    private static byte[] LerBytes(ZipArchive zip, string nome)
    {
        using var origem = zip.GetEntry(nome)!.Open();
        using var memoria = new MemoryStream();
        origem.CopyTo(memoria);
        return memoria.ToArray();
    }

    private static string LerTexto(ZipArchive zip, string nome) => Encoding.UTF8.GetString(LerBytes(zip, nome)).TrimStart('﻿');

    private static async Task<T> ComCulturaAsync<T>(string cultura, Func<Task<T>> acao)
    {
        var anterior = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultura);
        try
        {
            return await acao();
        }
        finally
        {
            CultureInfo.CurrentCulture = anterior;
        }
    }

    // pt-BR: vírgula decimal. th-TH: calendário budista (ano 2569 em vez de 2026). ar-SA: calendário
    // Hijri. Nenhuma dessas pode vazar para o arquivo, que é lido por outro programa.
    [Theory]
    [InlineData("pt-BR")]
    [InlineData("th-TH")]
    [InlineData("ar-SA")]
    [InlineData("en-US")]
    public async Task Datas_e_valores_devem_sair_no_formato_invariante_em_qualquer_cultura(string cultura)
    {
        CriarLancamento(1234.56m, "Mensalidade");
        _context.Transferencias.Add(new Transferencia(_empresa.Id, _banco, _caixa, _data, 50.5m));
        _context.SaveChanges();

        var caminho = await ComCulturaAsync(cultura, ExportarAsync);

        using var zip = ZipFile.OpenRead(caminho);
        var dataEsperada = $"{_data.Year:D4}-{_data.Month:D2}-01";

        using var lancamentos = JsonDocument.Parse(LerTexto(zip, "lancamentos.json"));
        var lancamento = lancamentos.RootElement[0];
        Assert.Equal(dataEsperada, lancamento.GetProperty("data").GetString());
        Assert.Equal(1234.56m, lancamento.GetProperty("valor").GetDecimal());

        using var transferencias = JsonDocument.Parse(LerTexto(zip, "transferencias.json"));
        Assert.Equal(dataEsperada, transferencias.RootElement[0].GetProperty("data").GetString());

        var linhaCsv = LerTexto(zip, "lancamentos.csv").Split('\n')[1];
        Assert.StartsWith($"\"{dataEsperada}\";\"Entrada\";\"1234.56\"", linhaCsv);
        Assert.EndsWith($"_{_data.Year:D4}-{_data.Month:D2}.contabil", caminho);
    }

    [Fact]
    public async Task Csv_deve_escapar_separador_aspas_e_quebra_de_linha_e_manter_acentos()
    {
        CriarLancamento(10m, "Taxa; \"extra\" de ação\nsegunda linha");

        using var zip = ZipFile.OpenRead(await ExportarAsync());
        var csv = LerTexto(zip, "lancamentos.csv");

        Assert.Contains("\"Taxa; \"\"extra\"\" de ação\nsegunda linha\"", csv);
        Assert.Contains("Contribuições", csv);
    }

    [Fact]
    public async Task Csv_deve_ter_bom_para_o_excel_e_json_nao_deve_ter_bom()
    {
        CriarLancamento(10m, "Doação");

        using var zip = ZipFile.OpenRead(await ExportarAsync());

        // Excel só reconhece acentos de um CSV UTF-8 com BOM; já JSON com BOM é proibido pela
        // RFC 8259 e quebra leitores estritos (ex: json.loads de uma string em Python).
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, LerBytes(zip, "lancamentos.csv")[..3]);
        foreach (var nome in new[] { "manifest.json", "empresa.json", "lancamentos.json", "historico.json", "transferencias.json" })
            Assert.NotEqual(0xEF, LerBytes(zip, nome)[0]);

        Assert.Contains("\"Doação\"", Encoding.UTF8.GetString(LerBytes(zip, "lancamentos.json")));
    }

    // Um texto começando com =, +, - ou @ vira fórmula quando o CSV é aberto no Excel do escritório
    // (injeção de fórmula / "CSV injection").
    [Theory]
    [InlineData("=HYPERLINK(\"http://x\")")]
    [InlineData("+1+1")]
    [InlineData("-2+3")]
    [InlineData("@SUM(A1)")]
    public async Task Csv_deve_neutralizar_texto_que_o_excel_executaria_como_formula(string descricaoMaliciosa)
    {
        CriarLancamento(10m, descricaoMaliciosa);

        using var zip = ZipFile.OpenRead(await ExportarAsync());

        Assert.Contains($"\"'{descricaoMaliciosa.Replace("\"", "\"\"")}\"", LerTexto(zip, "lancamentos.csv"));
        // O JSON é dado estruturado, não planilha: continua com o texto original.
        using var json = JsonDocument.Parse(LerTexto(zip, "lancamentos.json"));
        Assert.Equal(descricaoMaliciosa, json.RootElement[0].GetProperty("descricao").GetString());
    }

    [Fact]
    public async Task Json_nao_deve_citar_comprovante_cujo_arquivo_nao_existe_mais()
    {
        // Ex: comprovantes apagados na tela de Armazenamento e competência exportada de novo.
        var lancamento = CriarLancamento(10m, "Com comprovante apagado");
        var anexo = new Anexo(lancamento.Id, Path.Combine(Guid.NewGuid().ToString(), "sumiu.pdf"), "nota.pdf", TipoArquivoAnexo.Pdf, 100);
        _context.Anexos.Add(anexo);
        _context.SaveChanges();

        using var zip = ZipFile.OpenRead(await ExportarAsync());

        using var json = JsonDocument.Parse(LerTexto(zip, "lancamentos.json"));
        var nomesAnexos = json.RootElement[0].GetProperty("anexos").EnumerateArray().Select(a => a.GetString()!).ToList();
        foreach (var nome in nomesAnexos)
            Assert.NotNull(zip.GetEntry($"anexos/{nome}"));
    }

    [Fact]
    public async Task Exportar_de_novo_o_mesmo_periodo_deve_substituir_o_arquivo()
    {
        CriarLancamento(10m, "Primeiro");
        var primeiro = await ExportarAsync();
        CriarLancamento(20m, "Segundo");

        var segundo = await ExportarAsync();

        Assert.Equal(primeiro, segundo);
        using var zip = ZipFile.OpenRead(segundo);
        using var json = JsonDocument.Parse(LerTexto(zip, "lancamentos.json"));
        Assert.Equal(2, json.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task Lancamento_e_transferencia_excluidos_nao_devem_ser_exportados()
    {
        CriarLancamento(10m, "Fica");
        CriarLancamento(20m, "Excluído").Excluir();
        var transferencia = new Transferencia(_empresa.Id, _banco, _caixa, _data, 5m);
        transferencia.Excluir();
        _context.Transferencias.Add(transferencia);
        _context.SaveChanges();

        using var zip = ZipFile.OpenRead(await ExportarAsync());

        using var lancamentos = JsonDocument.Parse(LerTexto(zip, "lancamentos.json"));
        Assert.Equal("Fica", Assert.Single(lancamentos.RootElement.EnumerateArray()).GetProperty("descricao").GetString());
        using var transferencias = JsonDocument.Parse(LerTexto(zip, "transferencias.json"));
        Assert.Equal(0, transferencias.RootElement.GetArrayLength());
    }
}
