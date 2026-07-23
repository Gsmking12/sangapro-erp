#define MyAppName "SunDevPro PrintBridge"
#define MyAppVersion "1.0.23"
#define MyAppPublisher "Sangare Tidiane"

[Setup]
AppId={{6A6C379D-52F3-4EE1-9A3C-4DD09F7E1023}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={commonappdata}\SunDevPro\PrintBridge
DisableProgramGroupPage=yes
OutputDir=..\..\dist
OutputBaseFilename=SunDevPro_V1.0.23_SETUP_COMPLET
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Files]
Source: "..\publish\SunDevProPrintBridge.exe"; DestDir: "{app}"; Flags: ignoreversion

[Run]
Filename: "{cmd}"; Parameters: "/C netsh http add urlacl url=http://+:17823/ user=Everyone"; Flags: runhidden waituntilterminated
Filename: "{cmd}"; Parameters: "/C netsh advfirewall firewall delete rule name=""SunDevPro PrintBridge LAN"""; Flags: runhidden waituntilterminated
Filename: "{cmd}"; Parameters: "/C netsh advfirewall firewall add rule name=""SunDevPro PrintBridge LAN"" dir=in action=allow protocol=TCP localport=17823 profile=private"; Flags: runhidden waituntilterminated
Filename: "{sys}\sc.exe"; Parameters: "create SunDevProPrintBridge binPath= ""{app}\SunDevProPrintBridge.exe"" start= auto DisplayName= ""SunDevPro PrintBridge"""; Flags: runhidden waituntilterminated
Filename: "{sys}\sc.exe"; Parameters: "description SunDevProPrintBridge ""Impression centralisée SunDevPro depuis les postes clients"""; Flags: runhidden waituntilterminated
Filename: "{sys}\sc.exe"; Parameters: "start SunDevProPrintBridge"; Flags: runhidden waituntilterminated

[UninstallRun]
Filename: "{sys}\sc.exe"; Parameters: "stop SunDevProPrintBridge"; Flags: runhidden waituntilterminated
Filename: "{sys}\sc.exe"; Parameters: "delete SunDevProPrintBridge"; Flags: runhidden waituntilterminated
Filename: "{cmd}"; Parameters: "/C netsh advfirewall firewall delete rule name=""SunDevPro PrintBridge LAN"""; Flags: runhidden waituntilterminated
Filename: "{cmd}"; Parameters: "/C netsh http delete urlacl url=http://+:17823/"; Flags: runhidden waituntilterminated
