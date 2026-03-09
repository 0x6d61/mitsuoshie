; Mitsuoshie Inno Setup Script
; ハニートークン検知ツール インストーラー

#define MyAppName "Mitsuoshie"
#define MyAppVersion "0.0.1"
#define MyAppPublisher "0x6d61"
#define MyAppURL "https://github.com/0x6d61/mitsuoshie"
#define MyAppExeName "Mitsuoshie.exe"

[Setup]
AppId={{8A3F5E2D-7B1C-4D6A-9E8F-0A2B3C4D5E6F}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
; 管理者権限を要求（SACL設定と監査ポリシーに必要）
PrivilegesRequired=admin
OutputDir=..\dist
OutputBaseFilename=MitsuoshieSetup-{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
SetupIconFile=app.ico
UninstallDisplayIcon={app}\Mitsuoshie.exe
; 日本語優先、英語フォールバック
ShowLanguageDialog=auto

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "startup"; Description: "Windows 起動時に自動で開始する"; GroupDescription: "起動設定:"; Flags: checkedonce
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; publish 出力ディレクトリからコピー
Source: "..\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; インストール後に監査ポリシーを有効化
Filename: "auditpol"; Parameters: "/set /subcategory:""File System"" /success:enable"; \
  Flags: runhidden waituntilterminated; StatusMsg: "ファイルシステム監査ポリシーを有効化中..."

; Task Scheduler にスタートアップ登録（現在のユーザーで最高権限タスク作成）
; /RU でインストール実行ユーザーではなく実際のログオンユーザーを指定
Filename: "schtasks"; Parameters: "/Create /TN ""{#MyAppName}"" /TR """"""{app}\{#MyAppExeName}"""""" /SC ONLOGON /RL HIGHEST /RU ""{username}"" /F"; \
  Flags: runhidden waituntilterminated; Tasks: startup; StatusMsg: "スタートアップタスクを登録中..."

; インストール完了後にアプリを起動
Filename: "{app}\{#MyAppExeName}"; Description: "{#MyAppName} を今すぐ起動する"; \
  Flags: nowait postinstall skipifsilent shellexec runasoriginaluser

[UninstallRun]
; アンインストール前に Mitsuoshie を停止する（未起動時のエラーを無視）
Filename: "cmd"; Parameters: "/c taskkill /F /IM Mitsuoshie.exe >nul 2>&1 || exit /b 0"; \
  Flags: runhidden waituntilterminated; RunOnceId: "KillMitsuoshie"
; Task Scheduler のタスクも削除
Filename: "cmd"; Parameters: "/c schtasks /Delete /TN ""Mitsuoshie"" /F >nul 2>&1 || exit /b 0"; \
  Flags: runhidden waituntilterminated; RunOnceId: "DeleteTask"
; 注意: auditpol disable は行わない（他アプリの監査設定に影響するため）

[UninstallDelete]
; Mitsuoshie のローカルデータを削除
Type: filesandordirs; Name: "{localappdata}\Mitsuoshie"

[Code]
// settings.json の各行から "FilePath" の値を抽出する。
function ExtractPathFromLine(const Line: String): String;
var
  KeyPos, ValStart, ValEnd: Integer;
  Value: String;
begin
  Result := '';
  KeyPos := Pos('"FilePath"', Line);
  if KeyPos = 0 then
    Exit;

  // ": " の後の値を取得
  ValStart := Pos('": "', Line);
  if ValStart = 0 then
    Exit;
  ValStart := ValStart + 4;

  // 閉じ " を探す
  ValEnd := ValStart;
  while (ValEnd <= Length(Line)) and (Line[ValEnd] <> '"') do
    ValEnd := ValEnd + 1;

  if ValEnd <= ValStart then
    Exit;

  Value := Copy(Line, ValStart, ValEnd - ValStart);
  // JSON エスケープ (\\ → \) を戻す
  StringChangeEx(Value, '\\', '\', True);
  Result := Value;
end;

// ディレクトリが空かどうかチェック
function IsDirEmpty(const DirPath: String): Boolean;
var
  FindRec: TFindRec;
begin
  Result := True;
  if FindFirst(DirPath + '\*', FindRec) then
  begin
    try
      repeat
        if (FindRec.Name <> '.') and (FindRec.Name <> '..') then
        begin
          Result := False;
          Break;
        end;
      until not FindNext(FindRec);
    finally
      FindClose(FindRec);
    end;
  end;
end;

// アンインストール時に罠ファイルを削除するか確認。
// ハードコードせず settings.json から実際のパスを読み取ることで
// 管理者プロファイルと一般ユーザーのプロファイルの不一致問題を回避。
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  SettingsPath, ParentDir, FilePath: String;
  Lines: TArrayOfString;
  I: Integer;
  DeleteHoney: Boolean;
begin
  if CurUninstallStep = usUninstall then
  begin
    SettingsPath := ExpandConstant('{localappdata}') + '\Mitsuoshie\settings.json';

    if not FileExists(SettingsPath) then
      Exit;

    DeleteHoney := MsgBox(
      '配置された罠ファイルも削除しますか？' + #13#10 +
      '（残す場合は「いいえ」を選択してください）',
      mbConfirmation, MB_YESNO) = IDYES;

    if not DeleteHoney then
      Exit;

    if not LoadStringsFromFile(SettingsPath, Lines) then
      Exit;

    for I := 0 to GetArrayLength(Lines) - 1 do
    begin
      FilePath := ExtractPathFromLine(Lines[I]);
      if (FilePath <> '') and FileExists(FilePath) then
      begin
        DeleteFile(FilePath);

        // 親ディレクトリが空なら削除（.secure, .confidential 等）
        ParentDir := ExtractFileDir(FilePath);
        if DirExists(ParentDir) and IsDirEmpty(ParentDir) then
          RemoveDir(ParentDir);
      end;
    end;
  end;
end;
