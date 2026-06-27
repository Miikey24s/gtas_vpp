dotnet build -c Release
dotnet pack -c Release --no-build -o .\nupkg

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

dotnet nuget add source "https://nuget.pkg.github.com/USERNAME/index.json" --name github --username USERNAME --password TOKEN --store-password-in-clear-text