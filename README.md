## 简介
将Linux的系统语言切换到简体中文

## 支持的系统
- Debian
- Armbian
- Ubuntu
- Arch Linux
- CachyOS

## 测试通过的系统
### **x86_64：**
| 系统名称 | 版本 |
| - | - |
| Debian | 13.2 <br/> 11.6 |
| Ubuntu | 26.04 LTS <br/> 25.10 <br/> 25.04 <br/> 24.10 <br/> 24.04.1 LTS <br/> 22.04 LTS <br/> 20.04 LTS |
| Arch Linux | 滚动发行 |

### **aarch64：**
| 系统名称 | 版本 |
| - | - |
| Debian | 11.5 |

## 使用方法
### **x86_64：**
```shell
sudo su
wget https://github.com/TheWhiteDog9487/lag2cn/releases/latest/download/language
chmod +x language
./language
rm language
```
### **aarch64：**
```shell
sudo su
wget https://github.com/TheWhiteDog9487/lag2cn/releases/latest/download/language_arm
chmod +x language_arm
./language_arm
rm language_arm
```

## 关于可用性
对每一个系统版本进行实际测试是不现实的，我会尽量保证可用性，但是更新之后的测试只会在最新版本进行  
并且，由于各种系统版本众多来源复杂，哪怕是最新版本也未必100%可用  
故此，如果您在使用过程中遇到问题，请在GitHub仓库打开一个新的issue，我会尽力处理

## 如果你想自行编译
所需依赖项：
- .NET 10 SDK
- Clang
- build-base或其他等效工具集

推荐使用Alpine Linux进行编译，因为Alpine的系统libc就是musl，编译出的产物可以直接用而不需要管目标系统glibc的版本问题  
您可以使用下面的Docker命令获得一个Alpine环境
```shell
docker run --rm -it -v .:/src -w /src alpine
```
如果您位于中国大陆境内，推荐先对apk进行换源，以加速软件包下载
```shell
# https://help.mirrors.cernet.edu.cn/alpine/
printf '%s' 'https://mirrors.cernet.edu.cn/alpine/latest-stable/main
https://mirrors.cernet.edu.cn/alpine/latest-stable/community
' | tee /etc/apk/repositories
```
安装依赖项
```shell
apk update
apk add --no-cache clang build-base dotnet10-sdk
```
然后进行编译
```shell
dotnet publish language.cs
```
你需要的ELF会出现在`artifacts/language/`文件夹内