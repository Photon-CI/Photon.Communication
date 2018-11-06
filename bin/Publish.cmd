if exist "Photon.Communication\bin\Package\" rmdir /Q /S "Photon.Communication\bin\Package"
dotnet pack "Photon.Communication\Photon.Communication.csproj" -c Release -o "Photon.Communication\bin\Package"
nuget push "Photon.Communication\bin\Package\*.nupkg" -Source "https://www.nuget.org/api/v2/package" -NonInteractive
