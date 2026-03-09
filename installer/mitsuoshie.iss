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
; インストール後に監査ポリシーを有効化（GUID指定でロケール非依存）
Filename: "auditpol"; Parameters: "/set /subcategory:""{{0CCE921D-69AE-11D9-BED3-505054503030}}"" /success:enable"; \
  Flags: runhidden waituntilterminated; StatusMsg: "ファイルシステム監査ポリシーを有効化中..."

; Task Scheduler にスタートアップ登録（現在のユーザーで最高権限タスク作成）
; /RU でインストール実行ユーザーではなく実際のログオンユーザーを指定
Filename: "schtasks"; Parameters: "/Create /TN ""{#MyAppName}"" /TR """"""{app}\{#MyAppExeName}"""""" /SC ONLOGON /RL HIGHEST /RU ""{username}"" /F"; \
  Flags: runhidden waituntilterminated; Tasks: startup; StatusMsg: "スタートアップタスクを登録中..."

; EventSource を事前登録（管理者権限で実行されるため成功する）
Filename: "powershell"; Parameters: "-NoProfile -Command ""if (-not [System.Diagnostics.EventLog]::SourceExists('Mitsuoshie')) {{ [System.Diagnostics.EventLog]::CreateEventSource('Mitsuoshie', 'Application') }}"""; \
  Flags: runhidden waituntilterminated; StatusMsg: "Windows Event Log ソースを登録中..."

; インストール完了後にアプリを起動（管理者権限でSACL設定を行うため昇格したまま起動）
Filename: "{app}\{#MyAppExeName}"; Description: "{#MyAppName} を今すぐ起動する"; \
  Flags: nowait postinstall skipifsilent shellexec

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
Type: files; Name: "{app}\settingsdir.txt"

[Code]
// インストール後に settings.json のパスをインストール先に記録する。
// アンインストール時に {localappdata} が管理者プロファイルを指す問題を回避。
procedure CurStepChanged(CurStep: TSetupStep);
var
  SettingsDir: String;
begin
  if CurStep = ssPostInstall then
  begin
    SettingsDir := ExpandConstant('{localappdata}\Mitsuoshie');
    SaveStringToFile(ExpandConstant('{app}\settingsdir.txt'), SettingsDir, False);
  end;
end;

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

  // KeyPos の後にある ": " を探す（他のキーの値を誤取得しないよう）
  // Length('"FilePath"') = 10
  ValStart := 0;
  if KeyPos + Length('"FilePath"') <= Length(Line) then
    ValStart := Pos('": "', Copy(Line, KeyPos, Length(Line) - KeyPos + 1));
  if ValStart = 0 then
    Exit;
  ValStart := KeyPos + ValStart - 1 + 4;

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

// アンインストール時に罠ファイルを削除するか確認。
// settingsdir.txt からインストール時のユーザーの設定ディレクトリを読み取り、
// 管理者プロファイルと一般ユーザーのプロファイルの不一致問題を回避。
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  SettingsPath, SettingsDirFile, FilePath: String;
  Lines: TArrayOfString;
  I: Integer;
  DeleteHoney: Boolean;
begin
  if CurUninstallStep = usUninstall then
  begin
    // インストール時に保存した設定ディレクトリパスを読み取る
    SettingsDirFile := ExpandConstant('{app}\settingsdir.txt');
    SettingsPath := '';
    if FileExists(SettingsDirFile) then
    begin
      if LoadStringsFromFile(SettingsDirFile, Lines) and (GetArrayLength(Lines) > 0) then
        SettingsPath := Lines[0] + '\settings.json';
    end;
    if SettingsPath = '' then
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
        // 親ディレクトリは削除しない（.ssh, .aws 等にユーザーの実ファイルがある可能性）
      end;
    end;
  end;
end;
