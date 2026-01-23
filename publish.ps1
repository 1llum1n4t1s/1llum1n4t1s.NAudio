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
    # note that this will fail with 409 error if you try to push package that already exists
    Write-Host "publishing $pkg"
    dotnet nuget push "$folder\$pkg" --api-key $apiKey --source https://api.nuget.org/v3/index.json | out-null
    if (-not $?) { Write-Error "Failed to publish $pkg" }
}
else
{
    Write-Error "No package found in $folder"
}