[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$StoreId,

    [string]$Market,

    [string]$Locale,

    [ValidateSet('Windows.Desktop', 'Windows.Xbox', 'Windows.Team', 'Windows.Holographic')]
    [string]$DeviceFamily = 'Windows.Desktop',

    [ValidateSet('Tile', 'Logo', 'Screenshot')]
    [string[]]$PreferredImagePurpose = @('Tile', 'Logo'),

    [string]$OutputPath,

    [string]$OutputDirectory,

    [switch]$MetadataOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-DefaultLocale {
    $culture = Get-Culture
    if ([string]::IsNullOrWhiteSpace($culture.Name)) {
        return 'en-US'
    }

    return $culture.Name
}

function Get-DefaultMarket {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResolvedLocale
    )

    $parts = $ResolvedLocale.Split('-', [System.StringSplitOptions]::RemoveEmptyEntries)
    if ($parts.Count -ge 2) {
        return $parts[-1].ToUpperInvariant()
    }

    return 'US'
}

function Resolve-ImageUri {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Uri
    )

    if ($Uri.StartsWith('//')) {
        return "https:$Uri"
    }

    return $Uri
}

function Get-ExtensionFromContentType {
    param(
        [string]$ContentType
    )

    $mapping = @{
        'image/png'                = '.png'
        'image/jpeg'               = '.jpg'
        'image/jpg'                = '.jpg'
        'image/webp'               = '.webp'
        'image/gif'                = '.gif'
        'image/svg+xml'            = '.svg'
        'image/x-icon'             = '.ico'
        'image/vnd.microsoft.icon' = '.ico'
        'image/tiff'               = '.tif'
        'image/avif'               = '.avif'
    }

    if ([string]::IsNullOrWhiteSpace($ContentType)) {
        return '.img'
    }

    $normalized = $ContentType.Split(';', 2)[0].Trim().ToLowerInvariant()
    return $mapping[$normalized] ?? '.img'
}

function Select-BestImage {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Images,

        [Parameter(Mandatory = $true)]
        [string[]]$PurposePreference
    )

    $purposeRank = @{}
    for ($index = 0; $index -lt $PurposePreference.Count; $index++) {
        $purposeRank[$PurposePreference[$index]] = $index
    }

    $ranked = foreach ($image in $Images) {
        if (-not $image.Uri) {
            continue
        }

        $purpose = [string]$image.ImagePurpose
        $height = [int]($image.Height ?? 0)
        $width = [int]($image.Width ?? 0)
        $area = $height * $width
        $rank = if ($purposeRank.ContainsKey($purpose)) { $purposeRank[$purpose] } else { [int]::MaxValue }

        [pscustomobject]@{
            ImagePurpose = $purpose
            Width        = $width
            Height       = $height
            Area         = $area
            Uri          = Resolve-ImageUri -Uri ([string]$image.Uri)
            Caption      = [string]$image.Caption
            Rank         = $rank
        }
    }

    return $ranked |
        Sort-Object -Property @{ Expression = 'Rank'; Ascending = $true }, @{ Expression = 'Area'; Descending = $true }, @{ Expression = 'Height'; Descending = $true }, @{ Expression = 'Width'; Descending = $true } |
        Select-Object -First 1
}

function Get-StoreProductResponse {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResolvedStoreId,

        [Parameter(Mandatory = $true)]
        [string]$ResolvedMarket,

        [Parameter(Mandatory = $true)]
        [string]$ResolvedLocale,

        [Parameter(Mandatory = $true)]
        [string]$ResolvedDeviceFamily
    )

    $uri = "https://storeedgefd.dsx.mp.microsoft.com/v8.0/sdk/products?market=$ResolvedMarket&locale=$ResolvedLocale&deviceFamily=$ResolvedDeviceFamily"
    $body = @{ productIds = $ResolvedStoreId } | ConvertTo-Json -Compress

    return Invoke-RestMethod -Uri $uri -Method Post -ContentType 'application/json' -Body $body
}

function Save-Image {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Uri,

        [Parameter(Mandatory = $true)]
        [string]$ResolvedStoreId,

        [string]$ResolvedOutputPath,

        [string]$ResolvedOutputDirectory
    )

    if ($ResolvedOutputPath) {
        $targetPath = $ResolvedOutputPath
        $targetDirectory = Split-Path -Parent $targetPath
        if ($targetDirectory) {
            New-Item -ItemType Directory -Force -Path $targetDirectory | Out-Null
        }

        $response = Invoke-WebRequest -Uri $Uri -OutFile $targetPath -PassThru
        return [pscustomobject]@{
            Path        = $targetPath
            ContentType = [string]$response.Headers['Content-Type']
        }
    }

    $directory = if ($ResolvedOutputDirectory) { $ResolvedOutputDirectory } else { (Get-Location).Path }
    New-Item -ItemType Directory -Force -Path $directory | Out-Null

    $tempPath = Join-Path $directory "$ResolvedStoreId.download"
    $response = Invoke-WebRequest -Uri $Uri -OutFile $tempPath -PassThru
    $extension = Get-ExtensionFromContentType -ContentType ([string]$response.Headers['Content-Type'])
    $finalPath = Join-Path $directory "$ResolvedStoreId$extension"

    if (Test-Path $finalPath) {
        Remove-Item $finalPath -Force
    }

    Move-Item -Path $tempPath -Destination $finalPath
    return [pscustomobject]@{
        Path        = $finalPath
        ContentType = [string]$response.Headers['Content-Type']
    }
}

$resolvedLocale = if ($Locale) { $Locale } else { Get-DefaultLocale }
$resolvedMarket = if ($Market) { $Market.ToUpperInvariant() } else { Get-DefaultMarket -ResolvedLocale $resolvedLocale }

$response = Get-StoreProductResponse -ResolvedStoreId $StoreId -ResolvedMarket $resolvedMarket -ResolvedLocale $resolvedLocale -ResolvedDeviceFamily $DeviceFamily
$product = @($response.Products)[0]

if (-not $product) {
    throw "No Microsoft Store product was returned for StoreId '$StoreId'."
}

$localizedProperties = @($product.LocalizedProperties)
$propertySet = $localizedProperties | Where-Object { @($_.Images).Count -gt 0 } | Select-Object -First 1
if (-not $propertySet) {
    throw "The Microsoft Store response for '$StoreId' did not include localized images."
}

$images = @($propertySet.Images)
$selectedImage = Select-BestImage -Images $images -PurposePreference $PreferredImagePurpose
if (-not $selectedImage) {
    throw "No eligible image was found for StoreId '$StoreId'."
}

$result = [pscustomobject]@{
    StoreId               = $StoreId
    Market                = $resolvedMarket
    Locale                = $resolvedLocale
    DeviceFamily          = $DeviceFamily
    Title                 = [string]$propertySet.ProductTitle
    Publisher             = [string]$propertySet.PublisherName
    ProductKind           = [string]$product.ProductKind
    ProductUri            = "https://apps.microsoft.com/detail/$StoreId"
    PreferredImagePurpose = @($PreferredImagePurpose)
    SelectedImagePurpose  = [string]$selectedImage.ImagePurpose
    SelectedImageWidth    = [int]$selectedImage.Width
    SelectedImageHeight   = [int]$selectedImage.Height
    SelectedImageUri      = [string]$selectedImage.Uri
    AvailableImages       = @($images | ForEach-Object {
        [pscustomobject]@{
            ImagePurpose = [string]$_.ImagePurpose
            Width        = [int]($_.Width ?? 0)
            Height       = [int]($_.Height ?? 0)
            Uri          = Resolve-ImageUri -Uri ([string]$_.Uri)
        }
    })
}

if (-not $MetadataOnly) {
    $download = Save-Image -Uri $result.SelectedImageUri -ResolvedStoreId $StoreId -ResolvedOutputPath $OutputPath -ResolvedOutputDirectory $OutputDirectory
    $result | Add-Member -NotePropertyName DownloadedPath -NotePropertyValue $download.Path
    $result | Add-Member -NotePropertyName ContentType -NotePropertyValue $download.ContentType
}

$result