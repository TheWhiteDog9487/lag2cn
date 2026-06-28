#include <cstdlib>
#include <iostream>
#include <string>
#include <fstream>
#include <filesystem>
#include <vector>
#include <utility>

#include <unistd.h>

using namespace std;

enum class System{
    Ubuntu,
    Debian,
    ArchLinux,
    Unsupported };
enum class User{
    Root,
    Other };

auto CheckIfRunningInsideWSL(){
    if (filesystem::exists("/proc/version")) {
        ifstream version_file("/proc/version");
        string version_info = "";
        getline(version_file, version_info);
        if (version_info.find("Microsoft") != string::npos or 
        version_info.find("WSL") != string::npos) {
            return true; }
        version_file.close(); }
    if (filesystem::exists("/proc/sys/kernel/osrelease")) {
        ifstream osrelease_file("/proc/sys/kernel/osrelease");
        string osrelease_info = "";
        getline(osrelease_file, osrelease_info);
        if (osrelease_info.find("microsoft") != string::npos or
            osrelease_info.find("WSL") != string::npos) {
            return true; }
        osrelease_file.close(); }
    return false; }

auto GetCurrentUser(){
    if (getenv("USER") != nullptr){
        string user = getenv("USER");
        if (user == "root" or geteuid() == 0){
            return User::Root; }
        else{
            return User::Other; } }
    else{
        throw runtime_error("无法获取用户信息"); } }

/// @brief  获取当前系统的发行版名称
/// @return 发行版名称，带有版本号
/// @author ChatGPT
auto GetCurrentSystem() {
    string distro_name = "";
    ifstream release_file("/etc/os-release");
    if (release_file.is_open()) {
        string line = "";
        while (getline(release_file, line)) {
            if (line.find("PRETTY_NAME=") != string::npos) {
                distro_name = line.substr(line.find("=") + 1);
                distro_name.erase(0, 1); // remove opening quote
                distro_name.erase(distro_name.size() - 1); // remove closing quote
                break; } }
        release_file.close(); }

    if(distro_name.find("Ubuntu") != string::npos){
        return pair(System::Ubuntu, distro_name); }
    else if(distro_name.find("Debian") != string::npos or
            distro_name.find("Armbian") != string::npos){
        return pair(System::Debian, distro_name); }
    else if(distro_name.find("Arch Linux") != string::npos or
            distro_name.find("CachyOS") != string::npos){
        return pair(System::ArchLinux, distro_name); }
    else{
        return pair(System::Unsupported, distro_name); } }

auto check() {
    cout << "请输入您的选项（输入y或者yes继续）";
    string userin = "";
    cin >> userin;
    if (userin == "y" or userin == "yes")
        return;
    else{
        cout << "输入不正确欸，如果想停止本程序的话请按下Ctrl+C" << endl;
        check(); } }

auto Ubuntu(){
    cout << "即将开始安装并配置简体中文语言包，请输入y或者yes继续，或者按Ctrl+C终止本程序";
    check();
    cout << "开始更新语言配置" << endl;
    system("apt update");
    system("apt install -y language-pack-zh-hans");
    system("update-locale LANG=zh_CN.UTF-8");
    cout << "配置完成，更改将在您下一次登录Shell时应用" << endl; }

auto Debian(){
    cout << "即将开始更新语言配置，请输入y或者yes继续，或者按Ctrl+C终止本程序";
    check();
    cout << "开始更新语言配置" << endl;
    fstream locale_gen("/etc/locale.gen");
    if (locale_gen.is_open() == false){
        throw system_error(errno, system_category(), "未能正确打开/etc/locale.gen"); }
    vector<string> FileContent;
    string Buffer = "";
    while (getline(locale_gen, Buffer)){
        FileContent.push_back(Buffer); }
    for (auto& Line : FileContent){
        if (Line == "zh_CN.UTF-8 UTF-8"){
            goto skip_write; } }
    locale_gen.clear();
    locale_gen.seekp(0, ios::end);
    locale_gen << '\n' << "zh_CN.UTF-8 UTF-8" << endl;
skip_write:
    locale_gen.close();
    system("locale-gen");
    system("update-locale LANG=zh_CN.UTF-8");
    cout << "配置完成，更改将在您下一次登录Shell时应用" << endl; }

auto ArchLinux(){
    // https://wiki.archlinuxcn.org/wiki/Locale
    cout << "即将开始更新语言配置，请输入y或者yes继续，或者按Ctrl+C终止本程序";
    check();
    cout << "开始更新语言配置" << endl;
    system("localectl set-locale LANG=zh_CN.UTF-8");
    if (CheckIfRunningInsideWSL() == true){
        // https://wiki.archlinuxcn.org/wiki/%E5%9C%A8_WSL_%E4%B8%8A%E5%AE%89%E8%A3%85_Arch_Linux#%E4%BF%AE%E6%94%B9%E5%8C%BA%E5%9F%9F%E8%AE%BE%E7%BD%AE
        cout << "检测到您正在WSL中运行Arch Linux，正在创建 /etc/locale.conf -> /etc/default/locale 符号链接" << endl;
        filesystem::create_symlink("/etc/locale.conf", "/etc/default/locale"); }
    cout << "配置完成，更改将在您下一次登录Shell时应用" << endl; }

auto ThrowUnsupportedSystemError(){
    cout << "错误：未知操作系统" << endl
        << "当前仅适配了Ubuntu，Debian和Arch Linux" << endl
        << "强行使用可能会造成不可预知的问题" << endl
        << "为避免出现故障，程序会强制退出" << endl
        << "如果是误报，请在GitHub仓库打开一个新的issue。" << endl;
    throw runtime_error("不支持的操作系统"); }

int main() {
    auto OsType = GetCurrentSystem();
    if (OsType.first == System::Unsupported){
        ThrowUnsupportedSystemError(); }

    if (GetCurrentUser() == User::Other){
        cout << "更改语言文件需要root权限，请使用sudo重新运行本程序" << endl;
        throw runtime_error("权限不足"); }

    cout << "检测到您使用的系统为：" << OsType.second << endl;
    if (OsType.first == System::Ubuntu){ Ubuntu(); }
    if (OsType.first == System::Debian){ Debian(); }
    if (OsType.first == System::ArchLinux){ ArchLinux(); } }