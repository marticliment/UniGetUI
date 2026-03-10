# UniGetUI Auto-Update Testing Guide (productinfo.json path)

This guide validates the new default auto-update flow that reads from `productinfo.json`.

If `productinfo.json` lookup fails for any reason, UniGetUI now falls back to the legacy updater logic that uses the existing version endpoint and GitHub release download URL.

## Files used

- `testing/productinfo.unigetui.test.json`
- Test artifacts expected by that file:
	- `UniGetUI.Installer.x64.exe`
	- `UniGetUI.Installer.arm64.exe`
	- `UniGetUI.x64.zip`
	- `UniGetUI.arm64.zip`

## What is being tested

- Default updater source is productinfo-based.
- Product key lookup for `Devolutions.UniGetUI`.
- Architecture-aware installer selection (`x64`/`arm64`, `exe` preferred).
- Hash validation (enabled by default).
- Test-only override behavior via registry keys under `HKCU\Software\Devolutions\UniGetUI`.

## 1) Host the test files locally

From repository root:

```powershell
Push-Location testing
python -m http.server 8080
Pop-Location
```

Make sure these URLs are reachable:

- `http://127.0.0.1:8080/productinfo.unigetui.test.json`
- `http://127.0.0.1:8080/UniGetUI.Installer.x64.exe`
- `http://127.0.0.1:8080/UniGetUI.Installer.arm64.exe`

## 2) Configure updater overrides (test mode)

Run in PowerShell:

```powershell
$regPath = 'HKCU:\Software\Devolutions\UniGetUI'
New-Item -Path $regPath -Force | Out-Null

# Point updater to local productinfo
Set-ItemProperty -Path $regPath -Name 'UpdaterProductInfoUrl' -Value 'http://127.0.0.1:8080/productinfo.unigetui.test.json'

# Product key inside productinfo JSON
Set-ItemProperty -Path $regPath -Name 'UpdaterProductKey' -Value 'Devolutions.UniGetUI'

# Allow local http URL and local domain for package downloads
Set-ItemProperty -Path $regPath -Name 'UpdaterAllowUnsafeUrls' -Type DWord -Value 1

# Keep hash validation enabled for normal test pass
Set-ItemProperty -Path $regPath -Name 'UpdaterSkipHashValidation' -Type DWord -Value 0

# Keep signer thumbprint validation enabled for normal test pass
Set-ItemProperty -Path $regPath -Name 'UpdaterSkipSignerThumbprintCheck' -Type DWord -Value 0

# Keep legacy path disabled (productinfo path is default)
Set-ItemProperty -Path $regPath -Name 'UpdaterUseLegacyGithub' -Type DWord -Value 0

# Optional only for HTTPS cert troubleshooting in test environments
Set-ItemProperty -Path $regPath -Name 'UpdaterDisableTlsValidation' -Type DWord -Value 0
```

## 3) Trigger update check in UniGetUI

1. Launch UniGetUI.
2. Go to **Settings → General**.
3. Click **Check for updates**.

Expected result:

- Updater reads the local `productinfo.unigetui.test.json`.
- It picks the correct architecture `exe` installer.
- Download starts and hash is validated.
- Update banner/toast appears when update is ready.

## 4) Negative test: hash mismatch protection

Use one of these methods:

- Replace installer file content but keep original hash in JSON, or
- Edit hash in JSON to an incorrect value.

Expected result:

- Hash validation fails.
- Update is aborted with installer authenticity error.

## 5) Negative test: block unsafe URLs

Set:

```powershell
Set-ItemProperty -Path 'HKCU:\Software\Devolutions\UniGetUI' -Name 'UpdaterAllowUnsafeUrls' -Type DWord -Value 0
```

Expected result with local `http://127.0.0.1` URLs:

- Updater rejects source/download URL as unsafe.
- No installer launch.

## 6) Optional: force legacy GitHub updater path

```powershell
Set-ItemProperty -Path 'HKCU:\Software\Devolutions\UniGetUI' -Name 'UpdaterUseLegacyGithub' -Type DWord -Value 1
```

Expected result:

- Legacy endpoint/GitHub code path is used.
- Productinfo path is bypassed for that run.

## 7) Fallback test: broken productinfo with successful legacy fallback

Use one of these methods:

- Point `UpdaterProductInfoUrl` to a missing URL, or
- Point `UpdaterProductInfoUrl` to a malformed JSON file, or
- Point `UpdaterProductKey` to a non-existent product.

Example:

```powershell
Set-ItemProperty -Path 'HKCU:\Software\Devolutions\UniGetUI' -Name 'UpdaterProductInfoUrl' -Value 'http://127.0.0.1:8080/does-not-exist.json'
Set-ItemProperty -Path 'HKCU:\Software\Devolutions\UniGetUI' -Name 'UpdaterUseLegacyGithub' -Type DWord -Value 0
```

Expected result:

- Productinfo check fails.
- UniGetUI logs that it is falling back to the legacy GitHub updater source.
- Legacy updater path is used automatically.
- If the legacy source has a newer version, the update flow continues normally.

## 8) Fallback test: both sources fail

Use a broken productinfo override and also make the legacy source unavailable in your test environment.

Expected result:

- Productinfo check fails first.
- UniGetUI attempts the legacy updater path.
- The updater shows the existing terminal error because neither source succeeded.

## 9) Optional: disable signer thumbprint check (test-only)

Use this only if your local installer is unsigned or signed with a non-Devolutions certificate.

```powershell
Set-ItemProperty -Path 'HKCU:\Software\Devolutions\UniGetUI' -Name 'UpdaterSkipSignerThumbprintCheck' -Type DWord -Value 1
```

## 10) Cleanup after testing

Reset to default production behavior:

```powershell
$regPath = 'HKCU:\Software\Devolutions\UniGetUI'
Remove-ItemProperty -Path $regPath -Name 'UpdaterProductInfoUrl' -ErrorAction SilentlyContinue
Remove-ItemProperty -Path $regPath -Name 'UpdaterProductKey' -ErrorAction SilentlyContinue
Remove-ItemProperty -Path $regPath -Name 'UpdaterAllowUnsafeUrls' -ErrorAction SilentlyContinue
Remove-ItemProperty -Path $regPath -Name 'UpdaterSkipHashValidation' -ErrorAction SilentlyContinue
Remove-ItemProperty -Path $regPath -Name 'UpdaterSkipSignerThumbprintCheck' -ErrorAction SilentlyContinue
Remove-ItemProperty -Path $regPath -Name 'UpdaterDisableTlsValidation' -ErrorAction SilentlyContinue
Remove-ItemProperty -Path $regPath -Name 'UpdaterUseLegacyGithub' -ErrorAction SilentlyContinue
```

With all override values removed, UniGetUI uses:

- `https://devolutions.net/productinfo.json`
- product key `Devolutions.UniGetUI`
- safety checks enabled
- hash validation enabled
