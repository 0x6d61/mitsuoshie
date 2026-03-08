# Mitsuoshie（ミツオシエ）

Windows向けハニートークン検知ツール。
罠ファイルを自動配置し、アクセスされたら即通知する。誤検知ほぼゼロ。

**「蜜の場所を教え、侵入者を罠に導く鳥 — Honeyguide」**

## 技術スタック

| コンポーネント | 技術 |
|--------------|------|
| 言語 | C# / .NET 8 |
| ファイルアクセス検知 | NTFS SACL + Security Event Log (Event ID 4663) |
| SACL設定 | System.Security.AccessControl |
| イベント購読 | System.Diagnostics.Eventing.Reader.EventLogWatcher |
| 整合性チェック | System.Security.Cryptography.SHA256 |
| Windows Event Log出力 | System.Diagnostics.EventLog |
| Sysmon互換ログ | System.Text.Json |
| タスクトレイ | System.Windows.Forms.NotifyIcon |
| Toast通知 | Microsoft.Toolkit.Uwp.Notifications |
| テスト | xUnit + Moq |
| インストーラー | Inno Setup |

## プロジェクト構成

```
Mitsuoshie/
├── CLAUDE.md
├── .gitignore
├── Mitsuoshie.sln
├── docs/
│   ├── 設計書.md
│   └── icon.png
├── src/
│   ├── Mitsuoshie.Core/          # コアロジック
│   │   ├── Deployment/           # 罠ファイル配置
│   │   ├── Monitoring/           # イベント監視
│   │   ├── Detection/            # アラート生成
│   │   ├── Logging/              # ログ出力
│   │   └── Models/               # データモデル
│   └── Mitsuoshie.App/           # WinForms トレイアプリ
├── tests/
│   ├── Mitsuoshie.Core.Tests/    # Core ユニットテスト
│   └── Mitsuoshie.App.Tests/     # App ユニットテスト
├── templates/
│   └── honey_files/              # ダミーファイルテンプレート
└── config/
    └── default_settings.json
```

## 開発コマンド

```bash
# ビルド
dotnet build

# テスト実行
dotnet test

# テスト（カバレッジ付き）
dotnet test --collect:"XPlat Code Coverage"

# 発行（自己完結型）
dotnet publish src/Mitsuoshie.App -r win-x64 --self-contained -c Release
```

## 開発ワークフロー

### TDD（テスト駆動開発）

1. **Red** — 失敗するテストを先に書く
2. **Green** — テストが通る最小限のコードを実装
3. **Refactor** — コードを整理・改善
4. テストなしで完了宣言禁止

### GitHub Flow

1. **Issue 作成** — タスクの目的・要件を明確化
2. **ブランチ作成** — `feature/<issue番号>-<slug>`
3. **実装 & コミット** — TDD サイクルで開発
4. **PR 作成** — `Closes #<番号>` を含める
5. **Claude Code レビュー** — `claude review-pr <PR番号>` でレビュー実施
6. **マージ** — レビュー通過後にマージ

### PR レビュー（Claude Code）

PRは `claude` CLI を使ってレビューする：

```bash
# PRレビュー実行
claude review-pr <PR番号>

# または直接
claude -p "Review PR #<番号> on this repository. Check for: security issues, test coverage, code quality, and adherence to the design document in docs/設計書.md"
```

レビュー観点：
- セキュリティ上の問題がないか（特に入力検証、権限操作）
- テストカバレッジが十分か
- 設計書（`docs/設計書.md`）との整合性
- コーディング規約への準拠
- エラーハンドリングの適切さ

### ブランチ命名規則

- `main` — 本番ブランチ
- `feature/<issue番号>-<slug>` — 機能開発（例: `feature/1-honey-deployer`）

### コミットメッセージ

- 日本語 or 英語どちらでも可
- 変更の「なぜ」を重視する
- `Co-Authored-By:` を含める（Claude Code 使用時）

## コーディング規約

- ファイルスコープ名前空間を使用（`namespace Mitsuoshie.Core;`）
- `record` 型を積極的に活用（イミュータブルなデータモデル）
- nullable reference types 有効（`<Nullable>enable</Nullable>`）
- セキュリティ関連のコードでは防御的プログラミングを徹底
- 既存ファイルの上書きは絶対に行わない（罠ファイル配置時）
- パスの結合には `Path.Combine()` を使用

## 設計原則

1. **やることは3つだけ**: 罠を撒く、監視する、通知する
2. **ブロックしない**: 検知と通知のみ
3. **誤検知ゼロを目指す**: 正規アプリが絶対にアクセスしないファイルだけを罠にする
4. **インストールしたら放置**: 設定不要、メンテ不要
5. **監視はカーネルに任せる**: NTFS監査（SACL）でWindowsカーネルがアクセスを記録
