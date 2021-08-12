dotnet publish -c Release --self-contained -r win-x64 -o publish/win-x64 /p:PublishSingleFile=true
dotnet publish -c Release --self-contained -r win-x86 -o publish/win-x86 /p:PublishSingleFile=true
dotnet publish -c Release --self-contained -r linux-x64 -o publish/linux-x64 /p:PublishSingleFile=true
