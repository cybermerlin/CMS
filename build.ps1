param (
    [string]$ProjectName = "CMS" 
)

Write-Host "--- Сборка ---" -ForegroundColor Magenta

# 1. Очистка
if (Test-Path "./publish") { Remove-Item -Recurse -Force "./publish" }
mkdir "./publish"

# 2. Публикация с поддержкой Preview-пакетов
# Мы добавляем свойство /p:SuppressRuntimePackError=true на случай, если NuGet капризничает
dotnet publish -f net10.0-windows `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -p:UseMonoRuntime=false    `
    -p:WindowsPackageType=None `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    --interactive `
    -o "./publish"
#dotnet publish -f net10.0-windows `
#    -c Release `
#    -r win-x64 `
#    --self-contained true `
#    -p:PublishSingleFile=true `
#    -p:UseMonoRuntime=false    `
#    -p:WindowsPackageType=None `
#    -p:IncludeNativeLibrariesForSelfExtract=true `
#    --interactive `
#    -o "./publish"
#dotnet publish -f net10.0-windows `
#    -c Release `
#    -r win-x64 `
#    --self-contained true `
#    -p:PublishSingleFile=true `
#    -p:WindowsPackageType=None `
#    -p:UseMonoRuntime=false `
#    -p:SuppressRuntimePackError=true `
#    --source "https://api.nuget.org" `
#    --source "https://pkgs.dev.azure.com" `
#    -o "./publish"


if ($LASTEXITCODE -ne 0) {
    Write-Error "Ошибка: Скорее всего, нужный рантайм .NET 10 еще не попал в основной NuGet.org."
    Write-Host "Попробуйте добавить фид ночных сборок: dotnet nuget add source https://pkgs.dev.azure.com -n dotnet10" -ForegroundColor Yellow
    exit 1
}

# 3. Упаковка
$zipFileName = "$ProjectName-Win11-Preview.zip"
Compress-Archive -Path "./publish/*" -DestinationPath $zipFileName -Update

Write-Host "`nГотово! Передайте пользователю файл: $zipFileName" -ForegroundColor Green
Write-Host "Пользователю нужно просто распаковать его и запустить $ProjectName.exe"



# Добавьте этот класс в ваш проект в папку Services. Он гарантирует, что папка в AppData создастся сама при первом запуске.
# using LiteDB;
# using System.IO;

# public static class DatabaseService
# {
    # private static string _dbPath;

    # public static LiteDatabase GetConnection()
    # {
        # // Путь: C:\Users\Имя\AppData\Local\CMS\data.db
        # var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        # var folder = Path.Combine(appData, "CMS");
        
        # if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        
        # _dbPath = Path.Combine(folder, "discussions.db");
        # return new LiteDatabase(_dbPath);
    # }
# }


# Чтобы скрипт выше сработал идеально, откройте файл вашего проекта .csproj и добавьте эти строки внутри <PropertyGroup> для Windows:

# <PropertyGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'">
    # <!-- Позволяет запускать EXE без установки в систему -->
    # <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    # <WindowsPackageType>None</WindowsPackageType>
    # <SelfContained>true</SelfContained>
    # <PublishSingleFile>true</PublishSingleFile>
    # <!-- Отключает проверку подписи для портативной версии -->
    # <AppxPackage>false</AppxPackage>
# </PropertyGroup>

