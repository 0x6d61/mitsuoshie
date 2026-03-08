; Mitsuoshie Inno Setup Script
; ハニートークン検知ツール インストーラー

#define MyAppName "Mitsuoshie"
#define MyAppVersion "1.0.0"
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

; インストール完了後にアプリを起動
Filename: "{app}\{#MyAppExeName}"; Description: "{#MyAppName} を今すぐ起動する"; \
  Flags: nowait postinstall skipifsilent

[Registry]
; スタートアップ登録（タスク選択時）
Root: HKCU; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; \
  ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"""; \
  Flags: uninsdeletevalue; Tasks: startup

[UninstallRun]
; アンインストール時に監査ポリシーを復元（オプション）
Filename: "auditpol"; Parameters: "/set /subcategory:""File System"" /success:disable"; \
  Flags: runhidden waituntilterminated

[UninstallDelete]
; Mitsuoshie のローカルデータを削除
Type: filesandordirs; Name: "{localappdata}\Mitsuoshie"

[Code]
// アンインストール時に罠ファイルを削除するか確認
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  UserProfile: String;
  HoneyPaths: array of String;
  I: Integer;
  DeleteHoney: Boolean;
begin
  if CurUninstallStep = usUninstall then
  begin
    DeleteHoney := MsgBox(
      '配置された罠ファイルも削除しますか？' + #13#10 +
      '（残す場合は「いいえ」を選択してください）',
      mbConfirmation, MB_YESNO) = IDYES;

    if DeleteHoney then
    begin
      UserProfile := ExpandConstant('{userprofile}');

      // 罠ファイルのパスリスト
      SetArrayLength(HoneyPaths, 7);
      HoneyPaths[0] := UserProfile + '\.aws\credentials.bak';
      HoneyPaths[1] := UserProfile + '\.ssh\id_rsa.old';
      HoneyPaths[2] := UserProfile + '\.config\.env.production';
      HoneyPaths[3] := UserProfile + '\Documents\.secure\passwords.xlsx';
      HoneyPaths[4] := UserProfile + '\AppData\Roaming\Bitcoin\wallet.dat.bak';
      HoneyPaths[5] := UserProfile + '\AppData\Local\Google\Chrome\User Data\Login Data.bak';
      HoneyPaths[6] := UserProfile + '\Desktop\.confidential\重要_機密情報.docx';

      for I := 0 to GetArrayLength(HoneyPaths) - 1 do
      begin
        if FileExists(HoneyPaths[I]) then
          DeleteFile(HoneyPaths[I]);
      end;

      // 隠しフォルダの削除
      if DirExists(UserProfile + '\Documents\.secure') then
        RemoveDir(UserProfile + '\Documents\.secure');
      if DirExists(UserProfile + '\Desktop\.confidential') then
        RemoveDir(UserProfile + '\Desktop\.confidential');
    end;
  end;
end;
