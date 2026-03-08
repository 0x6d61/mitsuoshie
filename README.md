# Mitsuoshie（ミツオシエ）

**Windows向けハニートークン検知ツール**

「蜜の場所を教え、侵入者を罠に導く鳥 — Honeyguide」

---

## 概要

Mitsuoshie は、Windows PC に罠ファイル（ハニートークン）を自動配置し、不正アクセスを検知するセキュリティツールです。

- インストールするだけで罠ファイルを自動配置
- ファイルにアクセスされたら即座に通知
- 誤検知ほぼゼロ（正規アプリは罠ファイルにアクセスしない）
- ブロックしない（検知と通知のみ）

## 仕組み

1. **罠を撒く** — AWS クレデンシャル、SSH 秘密鍵、パスワードファイルなどを模した罠ファイルを配置
2. **監視する** — NTFS 監査 ACL（SACL）を設定し、Windows カーネルがアクセスを記録
3. **通知する** — Event ID 4663 を購読し、罠ファイルへのアクセスを検知したら即座に通知

## 配置される罠ファイル

| 種別 | パス | 装うもの |
|------|------|----------|
| AWS Credential | `.aws\credentials.bak` | AWS クレデンシャルのバックアップ |
| SSH Key | `.ssh\id_rsa.old` | SSH 秘密鍵の旧版 |
| Env File | `.config\.env.production` | 本番環境の環境変数 |
| Password File | `Documents\.secure\passwords.xlsx` | パスワード一覧 |
| Crypto Wallet | `AppData\Roaming\Bitcoin\wallet.dat.bak` | Bitcoin ウォレット |
| Browser Data | `Chrome\User Data\Login Data.bak` | ブラウザ保存パスワード |
| Confidential | `Desktop\.confidential\重要_機密情報.docx` | 機密文書 |

## インストール

### インストーラー（推奨）

1. [Releases](https://github.com/0x6d61/mitsuoshie/releases) から `MitsuoshieSetup-x.x.x.exe` をダウンロード
2. 管理者として実行
3. 「次へ」を数回クリックして完了

### 手動ビルド

```powershell
# ビルド + テスト + publish
pwsh build.ps1

# テストのみ
dotnet test

# publish のみ
dotnet publish src/Mitsuoshie.App -c Release -r win-x64 --self-contained -o publish/win-x64
```

## 要件

- Windows 10 / 11
- 管理者権限（初回 SACL 設定時のみ）

## ログ出力

### Windows Event Log

- EventSource: `Mitsuoshie`
- EventLog: `Application`
- EventID: 1000（読取）, 1001（書込）, 1002（削除）, 1003（改ざん）

### Sysmon 互換 JSON

- 出力先: `%LOCALAPPDATA%\Mitsuoshie\logs\mitsuoshie_sysmon.jsonl`
- JSONL 形式（1行1JSON）
- SIEM / ログ分析ツールに直接投入可能

## 技術スタック

| コンポーネント | 技術 |
|--------------|------|
| 言語 | C# / .NET 10 |
| ファイルアクセス検知 | NTFS SACL + Security Event Log (Event ID 4663) |
| 整合性チェック | SHA256 ハッシュ比較（30分間隔） |
| UI | WinForms NotifyIcon（タスクトレイ常駐） |
| ログ | Windows Event Log + Sysmon 互換 JSON |
| インストーラー | Inno Setup |

## ライセンス

MIT License

---

# Mitsuoshie

**Honeytoken Detection Tool for Windows**

"A honeyguide that leads intruders into traps"

## Overview

Mitsuoshie automatically deploys honeytoken files on Windows PCs and detects unauthorized access.

- Install and forget — honeytokens are deployed automatically
- Instant notification when a trap file is accessed
- Near-zero false positives (legitimate apps never access trap files)
- Detection only — no blocking

## How It Works

1. **Deploy** — Place fake credential files (AWS keys, SSH keys, passwords, etc.)
2. **Monitor** — Set NTFS audit ACLs (SACL) so Windows kernel logs all access
3. **Alert** — Subscribe to Event ID 4663 and notify on honeytoken access

## Requirements

- Windows 10 / 11
- Administrator privileges (for initial SACL setup only)

## License

MIT License
