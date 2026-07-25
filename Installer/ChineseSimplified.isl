; *** Inno Setup Chinese Simplified language file v0.2.0 ***
[LangOptions]
LanguageName=ChineseSimplified
LanguageID=$0804
LanguageCodePage=936
DialogFontName=Microsoft YaHei
DialogFontSize=9
WelcomeFontName=Microsoft YaHei
WelcomeFontSize=12
TitleFontName=Microsoft YaHei
TitleFontSize=12
CopyrightFontName=Microsoft YaHei
CopyrightFontSize=8
RightToLeft=no

[Messages]
; ---- Wizard page titles ----
WizardSelectDir=选择安装位置
WizardSelectComponents=选择组件
WizardSelectProgramGroup=选择开始菜单文件夹
WizardSelectTasks=选择附加任务
WizardReady=准备安装
WizardInstalling=正在安装
WizardPreparing=正在准备安装...
WizardUserInfo=用户信息
WizardPassword=密码

; ---- Application titles ----
SetupAppTitle=安装
SetupWindowTitle=安装 - %1
UninstallAppTitle=卸载
UninstallAppFullTitle=%1 卸载程序

; ---- Welcome page ----
WelcomeLabel1=欢迎使用 [name] 安装向导
WelcomeLabel2=本程序将在您的计算机上安装 [name/ver]。%n%n建议您在继续安装前关闭所有其他应用程序。

; ---- License page ----
LicenseLabel=请仔细阅读以下许可协议，然后选择是否接受。
LicenseLabel3=请仔细阅读以下许可协议。您必须接受该协议才能继续安装。
LicenseAccepted=我接受许可协议(&A)
LicenseNotAccepted=我不接受许可协议(&D)

; ---- Password page ----
PasswordLabel1=此安装受密码保护。%n%n请输入密码，然后点击"下一步"继续。密码区分大小写。
PasswordEditLabel=密码(&P):
IncorrectPassword=输入的密码不正确，请重试。

; ---- Select Destination Location ----
SelectDirLabel3=安装程序将把 [name] 安装到以下文件夹中。
SelectDirBrowseLabel=点击"下一步"继续。如需选择其他文件夹，请点击"浏览"。
SelectDirDesc=请选择 [name] 的安装文件夹。
SelectDirectoryLabel=请选择安装目录。
DiskSpaceMBLabel=至少需要 [mb] MB 的可用磁盘空间。
DiskSpaceGBLabel=至少需要 [gb] GB 的可用磁盘空间。
CannotInstallToNetworkDrive=安装程序无法安装到网络驱动器。
CannotInstallToUNCPath=安装程序无法安装到 UNC 路径。
InvalidPath=您必须输入一个带有驱动器号的完整路径。
InvalidDrive=您所选的驱动器或 UNC 共享不存在或不可访问。
DirNameTooLong=文件夹名称或路径太长。
InvalidDirName=文件夹名称无效。
BadDirName32=文件夹名称不能包含以下任何字符：%n%n%1
DiskSpaceWarningTitle=磁盘空间不足
DiskSpaceWarning=安装程序至少需要 %1 KB 的可用磁盘空间才能安装。
DirExistsTitle=文件夹已存在
DirExists=文件夹已存在。您确定要继续安装到同一文件夹吗？
DirDoesntExistTitle=文件夹不存在
DirDoesntExist=文件夹不存在。您要创建该文件夹吗？
NewFolderName=新建文件夹

; ---- Select Components ----
SelectComponentsLabel2=选择您要安装的组件，清除您不想安装的组件。
SelectComponentsDesc=选择要安装的组件：
ComponentsDiskSpaceMBLabel=当前选择至少需要 [mb] MB 的磁盘空间。
ComponentsDiskSpaceGBLabel=当前选择至少需要 [gb] GB 的磁盘空间。
FullInstallation=完全安装
CompactInstallation=紧凑安装
CustomInstallation=自定义安装
ComponentSize1=%1 需要 %2 KB 的磁盘空间。
ComponentSize2=%1 需要 %2 MB 的磁盘空间。

; ---- Select Start Menu Folder ----
SelectStartMenuFolderLabel3=安装程序将在以下文件夹中创建快捷方式。
SelectStartMenuFolderBrowseLabel=点击"下一步"继续。如需选择其他文件夹，请点击"浏览"。
SelectStartMenuFolderDesc=您希望将程序的快捷方式放在哪里？
MustEnterGroupName=您必须输入文件夹名称。
GroupNameTooLong=文件夹名称或路径太长。
InvalidGroupName=文件夹名称无效。
BadGroupName=文件夹名称不能包含以下任何字符：%n%n%1
NoProgramGroupCheck2=不创建开始菜单文件夹(&N)

; ---- Select Tasks ----
SelectTasksLabel2=选择您想要安装程序执行的附加任务，然后点击"下一步"继续。
SelectTasksDesc=您想要安装程序执行哪些附加任务？

; ---- Ready to Install ----
ReadyLabel1=安装程序已准备就绪，即将开始安装。
ReadyLabel2a=点击"安装"继续安装，如果您想检查或更改设置，请点击"返回"。
ReadyLabel2b=点击"安装"继续安装。
ReadyMemoUserInfo=用户信息：
ReadyMemoDir=安装目录：
ReadyMemoType=安装类型：
ReadyMemoComponents=选定组件：
ReadyMemoGroup=开始菜单文件夹：
ReadyMemoTasks=附加任务：
ClickNext=点击"下一步"继续，或点击"返回"修改设置。

; ---- Installing ----
InstallingLabel=请稍候，安装程序正在安装 [name] 到您的计算机上。
StatusExtractFiles=正在提取文件...
StatusCreateDirs=正在创建目录...
StatusCreateIcons=正在创建快捷方式...
StatusCreateIniEntries=正在创建配置文件条目...
StatusCreateRegistryEntries=正在创建注册表条目...
StatusRegisterFiles=正在注册文件...
StatusSavingUninstall=正在创建卸载程序...
StatusRunProgram=正在完成安装...
StatusRollback=正在回滚更改...
StatusClosingApplications=正在关闭应用程序...
StatusRestartingApplications=正在重新启动应用程序...
StatusUninstalling=正在卸载...

; ---- Finished ----
FinishedHeadingLabel=安装完成
FinishedLabel=%1 安装完成。
FinishedLabelNoIcons=%1 已成功安装。
FinishedRestartLabel=要完成安装，必须重新启动计算机。%n%n您想现在重新启动吗？
FinishedRestartMessage=要完成安装，必须重新启动计算机。%n%n请保存所有工作，然后点击"是"重新启动。
ClickFinish=点击"完成"退出安装程序。
ShowReadmeCheck=您想查看自述文件吗？
RunEntryExec=运行 %1
RunEntryShellExec=打开 %1

; ---- Buttons ----
ButtonBack=< 上一步(&B)
ButtonNext=下一步(&N) >
ButtonInstall=安装(&I)
ButtonCancel=取消
ButtonFinish=完成(&F)
ButtonBrowse=浏览(&B)...
ButtonWizardBrowse=浏览(&B)...
ButtonNewFolder=新建文件夹(&M)
ButtonOK=确定
ButtonYes=是(&Y)
ButtonYesToAll=全部是(&A)
ButtonNo=否(&N)
ButtonNoToAll=全部否(&O)
ButtonStopDownload=停止下载(&S)

; ---- Exit dialog ----
ExitSetupTitle=退出安装程序
ExitSetupMessage=安装程序尚未完成。如果您现在退出，程序将不会被安装。%n%n您可以稍后再次运行安装程序以完成安装。%n%n确定要退出吗？

; ---- About dialog ----
AboutSetupTitle=关于安装程序
AboutSetupMessage=%1 版本 %2%n%n%1 主页：%n%3%n%nInno Setup 版本 %4
AboutSetupNote=
AboutSetupMenuItem=关于安装程序(&A)...

; ---- Uninstall ----
ConfirmUninstall=您确定要完全卸载 %1 及其所有组件吗？
UninstallStatusLabel=请稍候，正在卸载 %1。
UninstalledAll=%1 已成功从您的计算机卸载。
UninstalledMost=%1 卸载完成。%n%n部分文件未能删除，您可以手动删除这些文件。
UninstalledAndNeedsRestart=要完成卸载，必须重新启动计算机。%n%n您想现在重新启动吗？
OnlyAdminCanUninstall=此应用程序只能由具有管理员权限的用户卸载。
UninstallOnlyOnWin64=此卸载程序只能在 64 位 Windows 上运行。

; ---- Privileges ----
PrivilegesRequiredOverrideTitle=需要管理员权限
PrivilegesRequiredOverrideInstruction=安装程序需要管理员权限。请以管理员身份重新运行此安装程序。
PrivilegesRequiredOverrideText1=%1 需要管理员权限才能安装。
PrivilegesRequiredOverrideText2=%1 需要管理员权限才能继续。
PrivilegesRequiredOverrideAllUsers=为所有用户安装(&A)
PrivilegesRequiredOverrideAllUsersRecommended=为所有用户安装(&A)（推荐）
PrivilegesRequiredOverrideCurrentUser=仅为我安装(&M)
PrivilegesRequiredOverrideCurrentUserRecommended=仅为我安装(&M)（推荐）

; ---- Running application ----
CloseApplications=关闭应用程序
DontCloseApplications=不关闭应用程序
ApplicationsFound=以下应用程序正在使用需要安装程序更新的文件。%n%n建议您允许安装程序自动关闭这些应用程序。
ApplicationsFound2=以下应用程序正在使用需要安装程序更新的文件。%n%n建议您允许安装程序自动关闭这些应用程序。选择要关闭的应用程序，然后点击下一步继续。

; ---- Misc errors ----
ErrorCreatingDir=安装程序无法创建目录 "%1"
ErrorTooManyFilesInDir=无法在目录 "%1" 中创建文件，因为其中已包含过多文件
ErrorCreatingTemp=安装程序无法创建临时文件。
ErrorReadingSource=安装程序无法读取源文件。
ErrorReadingExistingDest=安装程序无法读取现有目标文件。
ErrorRenamingTemp=安装程序无法重命名临时文件。
ErrorReplacingExistingFile=安装程序无法替换现有文件。
ErrorChangingAttr=安装程序无法更改文件属性。

; ---- Misc labels ----
BeveledLabel=由 %1 提供
HelpTextNote=/?
SetupAlreadyRunning=安装程序已在运行。
SetupAppRunningError=安装程序检测到 %1 当前正在运行。%n%n请先关闭所有正在运行的 %1 实例，然后点击"确定"继续，或点击"取消"退出。
UninstallAppRunningError=卸载程序检测到 %1 当前正在运行。%n%n请先关闭所有正在运行的 %1 实例，然后点击"确定"继续。

[CustomMessages]
CreateDesktopIcon=创建桌面快捷方式(&D)
MsgRuntimeMissing=检测到您的系统尚未安装 .NET 10 Desktop Runtime。%n%nTsubakiCursorApp 需要 .NET 10 Desktop Runtime 才能运行。%n%n是否现在打开下载页面？%n（您也可以安装完成后再手动下载安装）
MsgInstallSuccess=TsubakiCursorApp 已成功安装！%n%nThemes 文件夹已创建在程序目录中，%n您可以通过应用程序远程下载主题。%n%n您可以在开始菜单找到快捷方式。
MsgInstallSuccessNoRuntime=安装完成，但 .NET 10 Desktop Runtime 尚未安装。%n应用程序可能无法正常启动。%n%n是否现在打开下载页面进行手动安装？
MsgUninstallUserData=是否同时删除用户数据？%n%n这将删除已下载的主题和备份文件：