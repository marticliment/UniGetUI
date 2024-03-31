# Turns on features in the customizations file
function Update-AdminFile
{
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [hashtable] $parameters,
        [string] $adminFile
    )
    Write-Debug "Running 'Update-AdminFile' with parameters:'$parameters', adminFile:'$adminFile'";
    if ($adminFile -eq '') { return }
    $s = $parameters['Features']
    if (!$s) { return }

    $features = $s.Split(',')
    [xml]$xml = Get-Content $adminFile

    $selectableItemCustomizations = $xml.DocumentElement.SelectableItemCustomizations
    $featuresSelectedByDefault = $selectableItemCustomizations.ChildNodes | Where-Object { $_.NodeType -eq 'Element' -and $_.GetAttribute('Hidden') -eq 'no' -and $_.GetAttribute('Selected') -eq 'yes' } | Select-Object -ExpandProperty Id
    $selectedFeatures = New-Object System.Collections.ArrayList
    $invalidFeatures = New-Object System.Collections.ArrayList
    foreach ($feature in $features)
    {
        $node = $selectableItemCustomizations.SelectSingleNode("*[@Id=""$feature""]")
        if ($null -ne $node)
        {
            Write-Host "Enabling requested feature: $feature"
            $node.Selected = "yes"
            $selectedFeatures.Add($feature) | Out-Null
        }
        else
        {
            $invalidFeatures.Add($feature) | Out-Null
        }
    }
    if ($invalidFeatures.Count -gt 0)
    {
        $errorMessage = "Invalid feature name(s): $invalidFeatures"
        $validFeatureNames = $selectableItemCustomizations.ChildNodes | Where-Object { $_.NodeType -eq 'Element' } | Select-Object -ExpandProperty Id
        Write-Warning $errorMessage
        Write-Warning "Valid feature names are: $validFeatureNames"
        throw $errorMessage
    }
    Write-Verbose "Features selected by default: $featuresSelectedByDefault"
    Write-Verbose "Features selected using package parameters: $selectedFeatures"
    $notSelectedNodes = $xml.DocumentElement.SelectableItemCustomizations.ChildNodes | Where-Object { $_.NodeType -eq 'Element' -and $_.Selected -eq "no" }
    foreach ($nodeToRemove in $notSelectedNodes)
    {
        Write-Verbose "Removing not selected AdminDeployment node: $($nodeToRemove.Id)"
        $nodeToRemove.ParentNode.RemoveChild($nodeToRemove) | Out-Null
    }
    $xml.Save($adminFile)
}
