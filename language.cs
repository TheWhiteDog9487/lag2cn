#!/usr/bin/env -S dotnet --
// https://learn.microsoft.com/zh-cn/dotnet/core/sdk/file-based-apps

#:property PublishAot=true
#:property StaticExecutable=true
#:property OptimizationPreference=Speed
// #:property OptimizationPreference=Size
#:property InvariantGlobalization=true
// ↑ 这个东西需要libicu
// #:property StackTraceSupport=false

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

void CheckUser(string DisplayMessage = "即将开始更新语言配置"){
    Console.WriteLine(DisplayMessage);
    Console.WriteLine("按回车继续，按Esc取消");
    while (true){
        var KeyInfo = Console.ReadKey(true);
        if (KeyInfo.Key == ConsoleKey.Enter){
            return; }
        else if (KeyInfo.Key == ConsoleKey.Escape){
            Environment.Exit(0); } } }

bool IsRunningInsideWSL(){
    string VersionFile = File.ReadAllText("/proc/version");
    string KernelOsReleaseFile = File.ReadAllText("/proc/sys/kernel/osrelease");
    if (VersionFile.Contains("Microsoft") || VersionFile.Contains("WSL")){
        return true; }
    if (KernelOsReleaseFile.Contains("microsoft") || KernelOsReleaseFile.Contains("WSL")){
        return true; }
    return false; }

void ExecuteCommand(string Command, 
                    List<string>? Arguments = null,
                    Action<int, string, string?>? WhenErrorThrown = null){
    List<string> StandardError = [];
    int ExitCode = 0;

    try{
        using var Process = new Process();
        Process.StartInfo.FileName = Command;
        if (Arguments != null){
            foreach (var Argument in Arguments){
                Process.StartInfo.ArgumentList.Add(Argument); } }
        Process.StartInfo.RedirectStandardError = true;
        Process.ErrorDataReceived += (_, Event) => {
            if (Event.Data != null){
                Console.WriteLine(Event.Data);
                StandardError.Add(Event.Data + "\n"); } };
        Process.Start();
        Process.BeginErrorReadLine();
        Process.WaitForExit();
        ExitCode = Process.ExitCode;
        if (ExitCode != 0){
            throw new Exception($"命令执行失败，退出代码: {ExitCode}"); } }
    catch (Exception ExceptionInstance){
        WhenErrorThrown ??= (ExitCode, StandardError, ExceptionMessage) => {
            Console.WriteLine(@$"
            命令执行失败
            退出代码: {ExitCode}
            stderr: {StandardError}
            异常信息: {ExceptionMessage ?? ""}"); };
        WhenErrorThrown(ExitCode, string.Join("\n", StandardError), ExceptionInstance.Message);
        throw; } }

string OsDescription = RuntimeInformation.OSDescription;
if (Enum.TryParse<SupportedLinuxDistributions>(OsDescription.Split(' ')[0], out var CurrentSystem) == false){
    if (OsDescription.Contains("Arch Linux")){
        CurrentSystem = SupportedLinuxDistributions.ArchLinux; }
    else if (OsDescription.Contains("Rocky Linux")){
        CurrentSystem = SupportedLinuxDistributions.RockyLinux; }
    else{
        string CurrentSupportedLinuxDistributions = string.Join(", ", Enum.GetNames<SupportedLinuxDistributions>());
        string Message = @$"
            当前系统为 {OsDescription}
            当前仅支持 {CurrentSupportedLinuxDistributions}
            强行使用可能会造成不可预知的问题
            为避免出现故障，程序会强制退出
            如果是误报，请在GitHub仓库打开一个新的issue。
        ".Trim();
        throw new NotSupportedException(Message); } }
if (Environment.IsPrivilegedProcess == false) {
    using var Process = new Process();
    Process.StartInfo.FileName = "sudo";
    Process.StartInfo.ArgumentList.Add(Environment.ProcessPath ?? throw new Exception("无法获取当前程序路径"));
    Console.WriteLine("更改语言文件需要root权限，当前用户权限不足");
    Console.WriteLine("正在尝试使用sudo重新运行程序");
    Console.WriteLine("如果您当前登录的用户拥有密码，请在sudo提示符中输入密码");
    Process.Start();
    Process.WaitForExit();
    Environment.Exit(Process.ExitCode); }

Console.WriteLine($"探测到当前系统为 {OsDescription}");
switch (CurrentSystem){
    case SupportedLinuxDistributions.Debian or SupportedLinuxDistributions.Armbian: {
        CheckUser();
        Console.WriteLine("正在检查语言配置");
        string[] NewContent = [];
        using var FileStream = new FileStream("/etc/locale.gen", FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        using (var StreamReader = new StreamReader(FileStream, Encoding.UTF8, true, 1024, true)){
            NewContent = [.. StreamReader.ReadToEnd()
            .Split("\n")
            .Select(Line => Line.Trim())
            .Select(Line => {
                if (Line.Contains("zh_CN.UTF-8 UTF-8")){
                    Line = "zh_CN.UTF-8 UTF-8"; }
                return Line; } ) ]; }
        FileStream.SetLength(0);
        FileStream.Seek(0, SeekOrigin.Begin);
        using (var StreamWriter = new StreamWriter(FileStream, Encoding.UTF8)){
            foreach (var Line in NewContent){
                StreamWriter.WriteLine(Line); } }
        Console.WriteLine("正在生成语言配置");
        ExecuteCommand("locale-gen");
        Console.WriteLine("正在更新语言配置");
        ExecuteCommand("update-locale", ["LANG=zh_CN.UTF-8"]);
        break; }
    case SupportedLinuxDistributions.Ubuntu: {
        CheckUser("即将开始安装中文语言包");
        Console.WriteLine("正在更新apt软件包源");
        ExecuteCommand("apt", ["update"]);
        Console.WriteLine("正在安装中文语言包");
        ExecuteCommand("apt", ["install", "language-pack-zh-hans", "-y"]);
        Console.WriteLine("正在更新语言配置");
        ExecuteCommand("update-locale", ["LANG=zh_CN.UTF-8"]);
        break; }
    case SupportedLinuxDistributions.ArchLinux or SupportedLinuxDistributions.CachyOS: {
        CheckUser();
        Console.WriteLine("开始更新语言配置");
        ExecuteCommand("localectl", ["set-locale", "LANG=zh_CN.UTF-8"]);
        if (IsRunningInsideWSL()){
            // https://wiki.archlinuxcn.org/wiki/%E5%9C%A8_WSL_%E4%B8%8A%E5%AE%89%E8%A3%85_Arch_Linux#%E4%BF%AE%E6%94%B9%E5%8C%BA%E5%9F%9F%E8%AE%BE%E7%BD%AE
            Console.WriteLine("检测到您正在WSL中运行Arch Linux，正在创建 /etc/locale.conf -> /etc/default/locale 符号链接");
            if (File.Exists("/etc/default/locale")){
                File.Move("/etc/default/locale", "/etc/default/locale.bak", true); }
            File.CreateSymbolicLink("/etc/default/locale", "/etc/locale.conf"); }
        break; }
    case SupportedLinuxDistributions.RockyLinux or SupportedLinuxDistributions.AlmaLinux or SupportedLinuxDistributions.Fedora: {
        CheckUser("即将开始安装中文语言包");
        Console.WriteLine("开始更新语言配置");
        Console.WriteLine("正在安装中文语言包");
        ExecuteCommand("dnf", ["install", "-y", "--nogpgcheck", "glibc-langpack-zh", "langpacks-zh_CN"]);
        Console.WriteLine("正在更新语言配置");
        ExecuteCommand("localectl", ["set-locale", "LANG=zh_CN.UTF-8"]);
        
        if (IsRunningInsideWSL() && CurrentSystem == SupportedLinuxDistributions.AlmaLinux) {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();
            Console.WriteLine("警告：检测到您正在WSL内使用AlmaLinux");
            Console.WriteLine("目前已知AlmaLinux的WSL镜像可能存在一些问题");
            Console.WriteLine("如果您遇到了诸如 dnf nano 等软件的显示仍然为英文的问题");
            Console.WriteLine("请尝试使用dnf重新安装或升级出现问题的软件包，或者直接完整更新系统内所有软件包，这应当可以解决问题");
            Console.WriteLine();
            Console.ResetColor(); }
        break; }
    default: {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"错误：您当前使用的操作系统 {OsDescription} ,匹配名称 {CurrentSystem} 受到支持，但是lag2cn没有为其配置执行逻辑");
        Console.WriteLine("这是一个程序Bug，请前往Github仓库开启一个issue，并提供当前输出日志");
        Console.ResetColor();
        throw new InvalidOperationException("遗漏的switch匹配"); } }

Console.WriteLine("配置完成，更改将在您下一次登录Shell时生效，按任意键退出");
Console.ReadKey(true);

enum SupportedLinuxDistributions {
    Debian,
    Armbian,
    Ubuntu,
    ArchLinux,
    CachyOS,
    RockyLinux,
    AlmaLinux,
    Fedora }