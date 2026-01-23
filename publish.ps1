# publishes to NuGet
# $apiKey needs to be already set up with NuGet publishing key
Write-Host $PSScriptRoot

if (-not $apiKey)
{
    throw "Need to set the API key first"
}

# publish the unified YAudio package
$folder = "$PSScriptRoot\NAudio\bin\Release"
$recent = gci "$folder\*.nupkg" | sort LastWriteTime | select -last 1
if ($recent)
{
    $pkg = $recent.Name
    $pkgPath = "$folder\$pkg"
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
    Write-Error "No package found in $folder"
    exit 1
}