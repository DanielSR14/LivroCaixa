# Livro Caixa

🇧🇷 [Português](#português) · 🇺🇸 [English](#english)

---

## Português

Aplicativo Windows gratuito que um escritório de contabilidade distribui aos seus clientes (associações, sindicatos, MEI e pequenas empresas) para eles registrarem entradas, saídas e comprovantes, e enviarem tudo digitalmente para o escritório num único arquivo `.contabil` — sem papel e sem lançamentos manuais.

O cliente nunca vê débito, crédito ou plano de contas: só **entrada**, **saída**, **categoria** e **conta**.

### Funcionalidades

- Lançamentos de entrada e saída, com comprovantes anexados (arrastar e soltar)
- Várias contas (caixa, banco com logo, cartão, investimento) e transferências entre elas
- Dashboard, relatórios por competência, fluxo de caixa e extrato com saldo por conta
- Categorias prontas por segmento (associação/sindicato, comércio, serviços, indústria)
- Modelos de lançamento para registros recorrentes
- Exportação mensal `.contabil` (zip com JSON, CSV e comprovantes) para o escritório
- Histórico de alterações (auditoria) e exclusão com possibilidade de restaurar
- Backup automático, bloqueio por inatividade e código de recuperação de acesso
- Tudo fica no computador do usuário (SQLite local); nada é enviado pela internet

### Estrutura

```
src/
├── LivroCaixa.Domain          Entidades e regras de negócio
├── LivroCaixa.Application     Casos de uso, DTOs, interfaces
├── LivroCaixa.Infrastructure  EF Core/SQLite, arquivos, backup, exportação
├── LivroCaixa.App             Interface WPF (MVVM)
└── LivroCaixa.Tests           Testes automatizados (xUnit)
```

### Rodando localmente

Requer Windows e o .NET 8 SDK.

```powershell
dotnet build src/LivroCaixa.sln
dotnet test src/LivroCaixa.sln
dotnet run --project src/LivroCaixa.App
```

Para gerar o instalador (Velopack): `installer\gerar-instalador.bat 1.0.0` (a partir da raiz do repositório; rode `dotnet tool restore` uma vez antes).

### Personalizando para o seu escritório

- **Dados do escritório** (nome, e-mail, CNPJ, endereço, telefone): [`src/LivroCaixa.App/DadosDoEscritorio.cs`](src/LivroCaixa.App/DadosDoEscritorio.cs). Os valores do repositório são genéricos; campos deixados como `null` não aparecem nas telas.
- **Logo e ícone**: `livrocaixa_logo.svg` e `livrocaixa_icone.svg` em `src/LivroCaixa.App/Assets/`. Se trocar a logo, gere de novo `livrocaixa_icone.ico`, `livrocaixa_logo.png` e `splash.png` a partir desses SVGs.
- **Cor principal**: `src/LivroCaixa.App/Theme/Colors.xaml`.

Logos de bancos: coleção de terceiros, ver [`Assets/BancosLogos/README.md`](src/LivroCaixa.App/Assets/BancosLogos/README.md).

---

## English

A free Windows desktop app that an accounting firm hands out to its clients (associations, unions, sole proprietors and small businesses) so they can record income, expenses and receipts, and send everything digitally to the firm in a single `.contabil` file — no paper, no manual data entry at the firm.

Clients never deal with debits, credits or a chart of accounts: only **income**, **expense**, **category** and **account**.

> The user interface is in Brazilian Portuguese, and the code identifiers follow Portuguese naming.

### Features

- Income and expense entries with attached receipts (drag and drop)
- Multiple accounts (cash, bank with logo, card, investment) and transfers between them
- Dashboard, monthly reports, cash flow and statement with per-account balance
- Ready-made categories by segment (association/union, retail, services, industry)
- Entry templates for recurring records
- Monthly `.contabil` export (zip with JSON, CSV and receipts) for the accounting firm
- Change history (audit trail) and soft delete with restore
- Automatic backup, inactivity lock and access recovery code
- All data stays on the user's computer (local SQLite); nothing is sent over the internet

### Project layout

```
src/
├── LivroCaixa.Domain          Entities and business rules
├── LivroCaixa.Application     Use cases, DTOs, interfaces
├── LivroCaixa.Infrastructure  EF Core/SQLite, files, backup, export
├── LivroCaixa.App             WPF user interface (MVVM)
└── LivroCaixa.Tests           Automated tests (xUnit)
```

### Running locally

Requires Windows and the .NET 8 SDK.

```powershell
dotnet build src/LivroCaixa.sln
dotnet test src/LivroCaixa.sln
dotnet run --project src/LivroCaixa.App
```

To build the installer (Velopack): `installer\gerar-instalador.bat 1.0.0` (from the repository root; run `dotnet tool restore` once first).

### Customizing for your firm

- **Firm details** (name, e-mail, tax ID, address, phone): [`src/LivroCaixa.App/DadosDoEscritorio.cs`](src/LivroCaixa.App/DadosDoEscritorio.cs). The repository ships with generic values; fields left as `null` are hidden in the UI.
- **Logo and icon**: `livrocaixa_logo.svg` and `livrocaixa_icone.svg` in `src/LivroCaixa.App/Assets/`. If you change the logo, regenerate `livrocaixa_icone.ico`, `livrocaixa_logo.png` and `splash.png` from those SVGs.
- **Primary color**: `src/LivroCaixa.App/Theme/Colors.xaml`.

Bank logos are a third-party collection — see [`Assets/BancosLogos/README.md`](src/LivroCaixa.App/Assets/BancosLogos/README.md).
