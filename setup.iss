; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "BatchPlotPdf"
#define MyAppVersion "1.3.5"
#define MyAppPublisher "XuGuang, Inc."

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{1A7D4226-9B83-4EC5-BAB7-AAB648568325}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
; Uncomment the following line to run in non administrative install mode (install for current user only.)
;PrivilegesRequired=lowest
OutputDir=F:\setup
OutputBaseFilename=mysetup
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "F:\project\BatchPlotPdf\BatchPlotPdf\bin\Debug\AcDx.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "F:\project\BatchPlotPdf\BatchPlotPdf\bin\Debug\AcMr.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "F:\project\BatchPlotPdf\BatchPlotPdf\bin\Debug\BatchPlotPdf.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "F:\project\BatchPlotPdf\BatchPlotPdf\bin\Debug\BatchPlotPdf.ini"; DestDir: "{app}"; Flags: ignoreversion
Source: "F:\project\BatchPlotPdf\BatchPlotPdf\bin\Debug\BatchPlotPdf.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: "F:\project\BatchPlotPdf\BatchPlotPdf\bin\Debug\INIFileParser.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "F:\project\BatchPlotPdf\BatchPlotPdf\bin\Debug\INIFileParser.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "F:\project\BatchPlotPdf\BatchPlotPdf\bin\Debug\log4net.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "F:\project\BatchPlotPdf\BatchPlotPdf\bin\Debug\log4net.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: "F:\project\BatchPlotPdf\BatchPlotPdf\bin\Debug\log4net.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "F:\project\BatchPlotPdf\BatchPlotPdf\bin\Debug\log4net.config"; DestDir: "{app}"; Flags: ignoreversion
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

