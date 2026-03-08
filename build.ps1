# Mitsuoshie ビルドスクリプト
# 使い方: pwsh build.ps1

param(
    [switch]$SkipTests,
    [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"

Write-Host "=== Mitsuoshie Build ===" -ForegroundColor Cyan

# 1. テスト実行
if (-not $SkipTests) {
    Write-Host "`n[1/3] テスト実行中..." -ForegroundColor Yellow
    dotnet test
    if ($LASTEXITCODE -ne 0) {
        Write-Host "テスト失敗！ビルドを中止します。" -ForegroundColor Red
        exit 1
    }
    Write-Host "テスト成功！" -ForegroundColor Green
} else {
    Write-Host "`n[1/3] テストスキップ" -ForegroundColor Gray
}

# 2. Publish（自己完結型）
Write-Host "`n[2/3] Publish（win-x64, self-contained）..." -ForegroundColor Yellow
$publishDir = Join-Path $PSScriptRoot "publish" "win-x64"
dotnet publish src/Mitsuoshie.App -c Release -r win-x64 --self-contained -o $publishDir
if ($LASTEXITCODE -ne 0) {
    Write-Host "Publish 失敗！" -ForegroundColor Red
    exit 1
}
Write-Host "Publish 成功: $publishDir" -ForegroundColor Green

# 3. Inno Setup（インストーラー生成）
if (-not $SkipInstaller) {
    Write-Host "`n[3/3] インストーラー生成中..." -ForegroundColor Yellow
    $iscc = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    if (Test-Path $iscc) {
        & $iscc installer\mitsuoshie.iss
        if ($LASTEXITCODE -ne 0) {
            Write-Host "インストーラー生成失敗！" -ForegroundColor Red
            exit 1
        }
        Write-Host "インストーラー生成成功: dist/" -ForegroundColor Green
    } else {
        Write-Host "Inno Setup が見つかりません。インストーラー生成をスキップします。" -ForegroundColor Gray
        Write-Host "インストール先: $iscc" -ForegroundColor Gray
    }
} else {
    Write-Host "`n[3/3] インストーラースキップ" -ForegroundColor Gray
}

Write-Host "`n=== ビルド完了！ ===" -ForegroundColor Cyan
