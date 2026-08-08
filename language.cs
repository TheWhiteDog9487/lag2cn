#!/usr/bin/env -S dotnet --
// https://learn.microsoft.com/zh-cn/dotnet/core/sdk/file-based-apps

#:property PublishAot=true
#:property StaticExecutable=true
#:property OptimizationPreference=Speed
// #:property OptimizationPreference=Size
#:property InvariantGlobalization=true
// #:property StackTraceSupport=false

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

[DllImport("libc", EntryPoint = "geteuid")]
static extern uint GetEffectiveUserId();

bool CheckUser(string DisplayMessage = "即将开始更新语言配置"){
    if (DisplayMessage != null){
        Console.WriteLine(DisplayMessage); }
    Console.WriteLine("按回车继续，按Esc取消");
    while (true){
        var KeyInfo = Console.ReadKey(true);
        if (KeyInfo.Key == ConsoleKey.Enter){
            return true; }
        else if (KeyInfo.Key == ConsoleKey.Escape){
            return false; } } }

bool IsRunningInsideWSL(){
    string VersionFile = File.ReadAllText("/proc/version");
    string KernelOsReleaseFile = File.ReadAllText("/proc/sys/kernel/osrelease");
    if (VersionFile.Contains("Microsoft") || VersionFile.Contains("WSL")){
        return true; }
    if (KernelOsReleaseFile.Contains("microsoft") || KernelOsReleaseFile.Contains("WSL")){
        return true; }
    return false; }

bool ExecuteCommand(string Command, 
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
        Process.BeginOutputReadLine();
        Process.BeginErrorReadLine();
        Process.WaitForExit();
        ExitCode = Process.ExitCode;
        if (ExitCode != 0){
            WhenErrorThrown?.Invoke(ExitCode, string.Join("\n", StandardError), null);
            return false; }
        return true; }
    catch (Exception ExceptionInstance){
        WhenErrorThrown ??= (ExitCode, StandardError, ExceptionMessage) => {
            Console.WriteLine(@$"
            命令执行失败
            退出代码: {ExitCode}
            stderr: {StandardError}
            异常信息: {ExceptionMessage}"); };
        WhenErrorThrown(ExitCode, string.Join("\n", StandardError), ExceptionInstance.Message);
        return false; } }
string OsDescription = RuntimeInformation.OSDescription;
if (Enum.TryParse<SupportedLinuxDistributions>(OsDescription.Split(' ')[0], out var CurrentSystem) == false){
    if (OsDescription.Contains("Arch Linux")){
        CurrentSystem = SupportedLinuxDistributions.ArchLinux; }
    else{
        string CurrentSuppoetedLinuxDistributions = string.Join(", ", Enum.GetNames<SupportedLinuxDistributions>());
        string Message = @$"
            当前系统为 {OsDescription}
            当前仅支持 {CurrentSuppoetedLinuxDistributions}
            强行使用可能会造成不可预知的问题
            为避免出现故障，程序会强制退出
            如果是误报，请在GitHub仓库打开一个新的issue。
        ".Trim();
        throw new NotSupportedException(Message); } }
if (GetEffectiveUserId() != 0){
    throw new UnauthorizedAccessException("更改语言文件需要root权限，请使用sudo重新运行本程序"); }

Console.WriteLine($"探测到当前系统为 {OsDescription}");
switch (CurrentSystem){
    case SupportedLinuxDistributions.Debian or SupportedLinuxDistributions.Armbian: {
        CheckUser();
        Console.WriteLine("正在检查语言配置");
            using var FileStream = new FileStream("/etc/locale.gen", FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            var NewContent = File.ReadLines("/etc/locale.gen")
            .Select(Line => Line.Trim())
            .Select(Line => {
                if (Line.Contains("zh_CN.UTF-8 UTF-8")){
                    Line = "zh_CN.UTF-8 UTF-8"; }
                return Line; } )
            .ToArray();
            FileStream.SetLength(0);
            FileStream.Seek(0, SeekOrigin.Begin);
            using var StreamWriter = new StreamWriter(FileStream, Encoding.UTF8);
            foreach (var Line in NewContent){
                StreamWriter.WriteLine(Line); }
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
    case SupportedLinuxDistributions.ArchLinux: {
        CheckUser();
        Console.WriteLine("开始更新语言配置");
        ExecuteCommand("localectl", ["set-locale", "LANG=zh_CN.UTF-8"]);
        if (IsRunningInsideWSL()){
                // https://wiki.archlinuxcn.org/wiki/%E5%9C%A8_WSL_%E4%B8%8A%E5%AE%89%E8%A3%85_Arch_Linux#%E4%BF%AE%E6%94%B9%E5%8C%BA%E5%9F%9F%E8%AE%BE%E7%BD%AE
                Console.WriteLine("检测到您正在WSL中运行Arch Linux，正在创建 /etc/locale.conf -> /etc/default/locale 符号链接");
                File.CreateSymbolicLink("/etc/locale.conf", "/etc/default/locale"); }
            break; } }
Console.WriteLine("配置完成，更改将在您下一次登录Shell时生效，按任意键退出");
Console.ReadKey(true);

enum SupportedLinuxDistributions {
    Debian,
    Armbian,
    Ubuntu,
    ArchLinux,
    CachyOS }