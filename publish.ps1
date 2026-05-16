# publishes to NuGet
# $apiKey を呼び出し側変数で渡すか、環境変数 NUGET_API_KEY で指定する
Write-Host $PSScriptRoot

# PowerShell の `.\publish.ps1` 起動は子スクリプトスコープを生成し、
# 親スコープのローカル変数 $apiKey は子に伝播しない仕様。
# よって CI (publish.yml) では `$env:NUGET_API_KEY` 経由のみが確実に届く。
if (-not $apiKey)
{
    $apiKey = $env:NUGET_API_KEY
}

if (-not $apiKey)
{
    throw "Need to set the API key first (引数 -ApiKey か 環境変数 NUGET_API_KEY で指定)"
}

# publish the unified 1llum1n4t1s.NAudio package (bin may be bin\Release or bin\x64\Release etc.)
$binFolder = "$PSScriptRoot\NAudio\bin"
$recent = Get-ChildItem -Path $binFolder -Filter "*.nupkg" -Recurse -File | Sort-Object LastWriteTime | Select-Object -Last 1
if ($recent)
{
    $pkg = $recent.Name
    $pkgPath = $recent.FullName
    Write-Host "publishing $pkg"
    Write-Host "Package path: $pkgPath"
    Write-Host "API key length: $($apiKey.Length)"
    
    $result = dotnet nuget push "$pkgPath" --api-key $apiKey --source https://api.nuget.org/v3/index.json 2>&1
    $exitCode = $LASTEXITCODE
    
    if ($exitCode -ne 0)
    {
        Write-Host "Error output:"
        Write-Host $result
        Write-Error "Failed to publish $pkg (exit code: $exitCode)"
        exit $exitCode
    }
    else
    {
        Write-Host "Successfully published $pkg"
    }
}
else
{
    Write-Error "No package found under $binFolder"
    exit 1
}