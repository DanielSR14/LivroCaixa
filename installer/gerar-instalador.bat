@echo off
setlocal

rem =============================================================
rem  Gera o instalador (.exe) do Livro Caixa usando Velopack.
rem
rem  Uso:
rem      gerar-instalador.bat <versao>
rem  Exemplo:
rem      gerar-instalador.bat 1.0.0
rem
rem  A versao precisa seguir o formato semver (MAJOR.MINOR.PATCH).
rem  Sempre usar um numero MAIOR que o anterior (sem auto-update: a
rem  distribuicao de cada nova versao para os clientes e manual).
rem =============================================================

if "%~1"=="" (
    echo Uso: gerar-instalador.bat ^<versao^>
    echo Exemplo: gerar-instalador.bat 1.0.0
    exit /b 1
)

set VERSAO=%~1
set PACK_ID=LivroCaixa
set MAIN_EXE=LivroCaixa.App.exe
set RAIZ=%~dp0..
set PROJETO=%~dp0..\src\LivroCaixa.App\LivroCaixa.App.csproj
set PASTA_PUBLISH=%~dp0publish
set PASTA_RELEASES=%~dp0Releases

echo ============================================
echo  Gerando instalador do Livro Caixa v%VERSAO%
echo ============================================

echo.
echo [1/4] Publicando o aplicativo (Release, win-x64, self-contained)...
rem -p:Version grava a versao no proprio .exe (App.Versao), que aparece no log de erros.
dotnet publish "%PROJETO%" -c Release -r win-x64 --self-contained true -p:Version=%VERSAO% -o "%PASTA_PUBLISH%"
if errorlevel 1 (
    echo.
    echo ERRO: falha ao publicar o aplicativo. Abortando.
    exit /b 1
)

echo.
echo [2/4] Empacotando com Velopack (vpk)...
pushd "%RAIZ%"

dotnet tool run vpk pack ^
    --packId %PACK_ID% ^
    --packVersion %VERSAO% ^
    --packDir "%PASTA_PUBLISH%" ^
    --mainExe %MAIN_EXE% ^
    --runtime win-x64 ^
    --outputDir "%PASTA_RELEASES%"

set VPK_RESULT=%errorlevel%
popd

if %VPK_RESULT% neq 0 (
    echo.
    echo ERRO: falha ao empacotar o instalador. Abortando.
    exit /b 1
)

rem Sem auto-update, os pacotes .nupkg de versoes antigas (~80-90MB cada) nao servem
rem para nada depois que a nova versao e gerada - so ocupam espaco em disco. Mantem
rem apenas os da versao atual.
echo.
echo [3/4] Limpando pacotes de versoes antigas (mantendo so a v%VERSAO%)...
for %%F in ("%PASTA_RELEASES%\*-full.nupkg" "%PASTA_RELEASES%\*-delta.nupkg") do (
    echo %%~nxF | findstr /C:"-%VERSAO%-" >nul
    if errorlevel 1 (
        del /q "%%F"
        echo   removido: %%~nxF
    )
)

rem Hash de integridade do instalador (SEGURANCA.md, secao 3): permite o cliente conferir
rem que o arquivo recebido e exatamente o que o escritorio gerou, mesmo sem assinatura de
rem codigo. Gravado num .sha256.txt ao lado do instalador, pra incluir/citar no e-mail de
rem distribuicao da versao.
echo.
echo [4/4] Calculando hash SHA-256 do instalador...
set SETUP_EXE=%PASTA_RELEASES%\%PACK_ID%-win-Setup.exe
set HASH_ARQUIVO=%PASTA_RELEASES%\%PACK_ID%-win-Setup.exe.sha256.txt
certutil -hashfile "%SETUP_EXE%" SHA256 > "%HASH_ARQUIVO%"

echo.
echo ============================================
echo  Instalador gerado com sucesso!
echo  Local: %PASTA_RELEASES%
echo  Arquivo principal: %PACK_ID%-win-Setup.exe
echo  Hash SHA-256: %HASH_ARQUIVO%
echo ============================================

endlocal
