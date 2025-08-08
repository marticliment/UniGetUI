# Available configurations

### `unigetui-min.winget`
Installs UniGetUI and its dependencies. WinGet and PowerShell 5 will work out of the box, but other package managers will require manual installation.

### `unigetui-full.winget`
Includes everything from `unigetui-min.winget` and also installs all supported package managers (except for vcpkg and Scoop, which must be installed manually).

### `unigetui-dev.winget`
Includes everything from `unigetui-full.winget`, installs required development tools, and clones the repository to your user folder.
