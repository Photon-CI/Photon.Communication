if exist "Photon.Communication\bin\Package\" rmdir /Q /S "Photon.Communication\bin\Package"
nuget pack "Photon.Communication\Photon.Communication.csproj" -OutputDirectory "Photon.Communication\bin\Package" -Build -Prop "Configuration=Release;Platform=AnyCPU"
nuget push "Photon.Communication\bin\Package\*.nupkg" -Source "https://www.nuget.org/api/v2/package" -NonInteractive
