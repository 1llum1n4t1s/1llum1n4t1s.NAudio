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

# publish the unified 1llum1n4t1s.NAudio package
# Directory.Build.props の <Version> と実際の nupkg ファイル名を突き合わせて、
# Debug 残骸や古いバージョンの誤 push を防ぐ。
$buildPropsPath = Join-Path $PSScriptRoot "Directory.Build.props"
if (-not (Test-Path $buildPropsPath))
{
    throw "Directory.Build.props が見つかりません: $buildPropsPath"
}
$expectedVersion = ([xml](Get-Content -Path $buildPropsPath -Raw)).Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($expectedVersion))
{
    throw "Directory.Build.props から <Version> を読み取れませんでした"
}
$expectedName = "1llum1n4t1s.NAudio.$expectedVersion.nupkg"
$binFolder = "$PSScriptRoot\NAudio\bin\x64\Release"
if (-not (Test-Path $binFolder))
{
    throw "Release 出力ディレクトリが存在しません: $binFolder ($expectedName をビルドしてください)"
}
$pkg = Get-ChildItem -Path $binFolder -Filter $expectedName -Recurse -File -ErrorAction SilentlyContinue | Select-Object -First 1
if ($pkg)
{
    $pkgName = $pkg.Name
    $pkgPath = $pkg.FullName
    Write-Host "publishing $pkgName"
    Write-Host "Package path: $pkgPath"
    Write-Host "API key length: $($apiKey.Length)"

    $result = dotnet nuget push "$pkgPath" --api-key $apiKey --source https://api.nuget.org/v3/index.json 2>&1
    $exitCode = $LASTEXITCODE

    if ($exitCode -ne 0)
    {
        Write-Host "Error output:"
        Write-Host $result
        Write-Error "Failed to publish $pkgName (exit code: $exitCode)"
        exit $exitCode
    }
    else
    {
        Write-Host "Successfully published $pkgName"
    }
}
else
{
    Write-Error "期待するパッケージ ($expectedName) が $binFolder 以下に見つかりません。<Version> 更新を忘れていない？"
    exit 1
}