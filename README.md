<p align="center">
  <img src="docs/icon.png" alt="Mitsuoshie" width="200">
</p>

<h1 align="center">Mitsuoshie（ミツオシエ）</h1>

<p align="center">
  <strong>Windows向けハニートークン検知ツール</strong><br>
  「蜜の場所を教え、侵入者を罠に導く鳥 — Honeyguide」
</p>

---

## 概要

Mitsuoshie は、Windows PC に罠ファイル（ハニートークン）を自動配置し、不正アクセスを検知するセキュリティツールです。

- インストールするだけで罠ファイルを自動配置
- ファイルにアクセスされたら即座にバルーン通知
- 誤検知ほぼゼロ（正規アプリは罠ファイルにアクセスしない）
- ブロックしない（検知と通知のみ）
- 再インストールしても既存の罠ファイルを維持し監視を再登録

## なぜハニートークンか — LLM エージェント時代の検知

従来の Infostealer は `.aws/credentials`、`.ssh/id_rsa`、`wallet.dat` を機械的に列挙します。
LLM エージェントが攻撃に使われる時代では、エージェントは「クレデンシャルを探して」という指示に従い、人間のようにファイルシステムを探索します。

ハニートークンの強み:
- 攻撃手法に依存しない（シグネチャ不要）
- LLM エージェントも Infostealer も同じ罠にかかる
- 誤検知がほぼゼロ（正規アプリは罠ファイルにアクセスしない）
- 攻撃者が罠を避けるには、罠の存在を事前に知る必要がある

## 仕組み

1. **罠を撒く** — AWS クレデンシャル、SSH 秘密鍵、パスワードファイルなどを模した罠ファイルを配置
2. **監視する** — NTFS 監査 ACL（SACL）を設定し、Windows カーネルが Security Event Log にアクセスを記録（Event ID 4663）
3. **通知する** — Event ID 4663 をリアルタイム購読し、罠ファイルへのアクセスを検知したらバルーン通知 + ログ出力

## 配置される罠ファイル

| 種別 | パス | 装うもの |
|------|------|----------|
| AWS Credential | `.aws\credentials.bak` | AWS クレデンシャルのバックアップ |
| SSH Key | `.ssh\id_rsa.old` | SSH 秘密鍵の旧版 |
| Env File | `.config\.env.production` | 本番環境の環境変数 |
| Password File | `Documents\.secure\passwords.xlsx` | パスワード一覧（隠しフォルダ） |
| Crypto Wallet | `AppData\Roaming\Bitcoin\wallet.dat.bak` | Bitcoin ウォレット |
| Browser Data | `Chrome\User Data\Login Data.bak` | ブラウザ保存パスワード |
| Confidential | `Desktop\.confidential\重要_機密情報.docx` | 機密文書（隠しフォルダ） |

全てランダム生成されたダミーデータです。有効なクレデンシャルは一切含みません。
ファイルのタイムスタンプは 3〜12 ヶ月前に設定され、最近作られたファイルに見えないようにしています。

## インストール

### インストーラー（推奨）

1. [Releases](https://github.com/0x6d61/mitsuoshie/releases) から `MitsuoshieSetup-x.x.x.exe` をダウンロード
2. 管理者として実行
3. 「次へ」を数回クリックして完了

インストーラーが自動で行うこと:
- ファイルシステム監査ポリシーの有効化（`auditpol`）
- Windows Event Log ソース（`Mitsuoshie`）の事前登録
- スタートアップタスクの登録（Task Scheduler、オプション）

### 手動ビルド

```powershell
# ビルド + テスト + publish + インストーラー生成
pwsh build.ps1

# テストのみ
dotnet test

# publish のみ
dotnet publish src/Mitsuoshie.App -c Release -r win-x64 --self-contained -o publish/win-x64
```

## 要件

- Windows 10 / 11
- 管理者権限（SACL 設定・監査ポリシー・EventLog 購読に必要）

## 動作モード

| モード | 条件 | 検知対象 |
|--------|------|----------|
| SACL 監視（推奨） | 管理者権限で起動 | 読み取り・書き込み・削除 |
| 簡易監視（フォールバック） | 非管理者で起動 | 書き込み・削除のみ（FileSystemWatcher） |

Task Scheduler で `highestAvailable` 権限のスタートアップタスクとして登録されるため、通常は SACL 監視モードで動作します。

## 安全プロセスの除外

以下のプロセスからのアクセスは自動的に除外されます:

| プロセス | 理由 |
|----------|------|
| `MsMpEng.exe` | Windows Defender |
| `SearchIndexer.exe` | Windows Search |
| `SearchProtocolHost.exe` | Windows Search |
| `TiWorker.exe` | Windows Update |
| `consent.exe` | UAC ダイアログ |

`explorer.exe` は ReadAttributes（0x80）のみ除外し、ReadData を含むアクセスはアラート対象になります。

## ログ出力

### Windows Event Log

- EventSource: `Mitsuoshie`
- EventLog: `Application`
- EventID: 1000（読取）, 1001（書込）, 1002（削除）, 1003（改ざん）
- EventID: 2000（サービス開始）, 2001（サービス停止）

### Sysmon 互換 JSON

- 出力先: `%LOCALAPPDATA%\Mitsuoshie\logs\mitsuoshie_sysmon.jsonl`
- JSONL 形式（1行1JSON）
- SIEM / ログ分析ツールに直接投入可能

## アラート機能

- **バルーン通知**: 罠ファイルへのアクセスを検知するとタスクトレイからバルーン通知
- **重複抑制**: 同一プロセス・同一ファイル・同一イベントタイプのアラートは 5 分間抑制
- **整合性チェック**: 30 分間隔で全罠ファイルの SHA256 ハッシュを比較し、改ざんを検知

## アンインストール

アンインストーラーが以下を行います:
- Mitsuoshie プロセスの停止
- Task Scheduler タスクの削除
- 罠ファイルの削除（確認ダイアログで選択可能、親ディレクトリは削除しない）
- ローカルデータの削除

> **注意**: 監査ポリシー（`auditpol`）は他のアプリに影響するため無効化しません。

## 技術スタック

| コンポーネント | 技術 |
|--------------|------|
| 言語 | C# / .NET 10 |
| ファイルアクセス検知 | NTFS SACL + Security Event Log (Event ID 4663) |
| SACL 設定 | System.Security.AccessControl |
| イベント購読 | System.Diagnostics.Eventing.Reader.EventLogWatcher |
| 整合性チェック | SHA256 ハッシュ比較（30 分間隔） |
| Windows Event Log 出力 | System.Diagnostics.EventLog |
| Sysmon 互換ログ | System.Text.Json |
| UI | WinForms NotifyIcon（タスクトレイ常駐） + バルーン通知 |
| インストーラー | Inno Setup |
| テスト | xUnit（131 テスト） |

## ライセンス

MIT License

---

# Mitsuoshie

**Honeytoken Detection Tool for Windows**

"A honeyguide that leads intruders into traps"

## Overview

Mitsuoshie automatically deploys honeytoken files on Windows PCs and detects unauthorized access via NTFS SACL auditing (Event ID 4663).

- Install and forget — honeytokens are deployed automatically
- Instant balloon notification when a trap file is accessed
- Near-zero false positives (legitimate apps never access trap files)
- Detection only — no blocking
- Survives reinstallation — existing honeytokens are re-registered for monitoring

## How It Works

1. **Deploy** — Place fake credential files (AWS keys, SSH keys, passwords, crypto wallets, etc.) with randomized dummy data
2. **Monitor** — Set NTFS audit ACLs (SACL) so Windows kernel logs all access to Security Event Log (Event ID 4663)
3. **Alert** — Subscribe to Event ID 4663 in real-time, notify via balloon tip, and write to Windows Event Log + Sysmon-compatible JSON

## Requirements

- Windows 10 / 11
- Administrator privileges (required for SACL, audit policy, and EventLog subscription)

## License

MIT License
