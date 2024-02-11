function Clear-VSChannelCache
{
    $localAppData = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
    $cachePath = Join-Path -Path $localAppData -ChildPath 'Microsoft\VisualStudio\Packages\_Channels'
    if (Test-Path -Path $cachePath)
    {
        Write-Verbose "Emptying the VS Installer channel cache: '$cachePath'"
        Get-ChildItem -Path $cachePath -Force | Remove-Item -Recurse -Force -ErrorAction Continue
    }
}
