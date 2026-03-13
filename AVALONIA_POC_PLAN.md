# Plan de Portage Avalonia — Proof of Concept macOS

## Vue d'ensemble

Ce plan est divisé en **5 phases incrémentales** (MVP). Chaque phase produit un livrable testable.
Le principe directeur est : **réutiliser 100% du backend existant**, ne réécrire que la couche UI et les intégrations plateforme.

### Fait critique découvert dans l'analyse

`PEInterface.cs` utilise **déjà** `#if WINDOWS` pour séparer les managers Windows-only :
```csharp
#if WINDOWS
    public static readonly WinGet WinGet = new();
    public static readonly Scoop Scoop = new();
    public static readonly Chocolatey Chocolatey = new();
#endif
    public static readonly Npm Npm = new();
    public static readonly Pip Pip = new();
    // ...
```

Le settings engine est **100% fichier** (pas de registre). Les projets `SharedTargetFrameworks` compilent déjà pour `net8.0` pur. Le travail backend est minimal.

---

## PHASE 1 — Squelette Avalonia qui démarre (MVP : "ça compile et ça s'ouvre")

### Objectif
Créer un projet Avalonia qui référence le backend existant, initialise les package managers cross-platform, et affiche une fenêtre vide avec navigation.

### Étapes

#### 1.1 — Créer le projet Avalonia

Créer `src/UniGetUI.Avalonia/` :

```
src/UniGetUI.Avalonia/
├── UniGetUI.Avalonia.csproj
├── App.axaml
├── App.axaml.cs
├── Program.cs
├── ViewLocator.cs
└── Views/
    └── MainWindow.axaml
    └── MainWindow.axaml.cs
```

**UniGetUI.Avalonia.csproj** :
- TargetFramework : `net8.0` (PAS `net8.0-windows`)
- Dépendances NuGet :
  - `Avalonia` (11.x)
  - `Avalonia.Desktop` (11.x)
  - `Avalonia.Themes.Fluent` (11.x — thème qui ressemble à WinUI)
  - `FluentAvalonia` (pour `NavigationView`, `InfoBar`, `ContentDialog` — équivalents WinUI)
- Références projet :
  - `UniGetUI.PackageEngine.PEInterface`
  - `UniGetUI.Core.Data`
  - `UniGetUI.Core.Settings`
  - `UniGetUI.Core.Logging`
  - `UniGetUI.Core.LanguageEngine`
  - `UniGetUI.Core.Tools`
  - `UniGetUI.Core.IconEngine`
  - `UniGetUI.Interface.Enums`
  - `UniGetUI.Interface.BackgroundApi`

#### 1.2 — Program.cs (point d'entrée)

```csharp
public static void Main(string[] args)
{
    BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
}

public static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace();
```

#### 1.3 — App.axaml.cs (initialisation)

Adapter la séquence de démarrage de `App.xaml.cs` :
1. `PEInterface.LoadLoaders()` — tel quel, aucun changement
2. Créer `MainWindow`
3. `PEInterface.LoadManagers()` — tel quel, charge pip, npm, cargo, etc.
4. `IconDatabase.Instance.LoadFromCacheAsync()` — tel quel
5. **Sauter** : `RegisterNotificationService()`, `LoadGSudo()`, `DWMThreadHelper`, `IntegrityTester`, `SetUpWebViewUserDataFolder()`
6. `BackgroundApi.Start()` — tel quel (c'est un serveur HTTP ASP.NET, portable)
7. Quand les managers sont chargés, passer à l'interface

#### 1.4 — MainWindow.axaml (shell de navigation)

Utiliser `FluentAvalonia.UI.Controls.NavigationView` (API quasi-identique au WinUI NavigationView) :

```xml
<fa:NavigationView x:Name="NavView" PaneDisplayMode="Left">
    <fa:NavigationView.MenuItems>
        <fa:NavigationViewItem Content="Discover" Tag="Discover" />
        <fa:NavigationViewItem Content="Updates" Tag="Updates" />
        <fa:NavigationViewItem Content="Installed" Tag="Installed" />
    </fa:NavigationView.MenuItems>
    <fa:NavigationView.FooterMenuItems>
        <fa:NavigationViewItem Content="Settings" Tag="Settings" />
    </fa:NavigationView.FooterMenuItems>

    <Grid RowDefinitions="*,Auto,Auto">
        <!-- Contenu principal -->
        <ContentControl x:Name="ContentFrame" Grid.Row="0" />
        <!-- Splitter -->
        <GridSplitter Grid.Row="1" Height="4" />
        <!-- File d'opérations -->
        <ListBox x:Name="OperationList" Grid.Row="2" MaxHeight="200" />
    </Grid>
</fa:NavigationView>
```

#### 1.5 — Ajouter au fichier solution

Mettre à jour `UniGetUI.Avalonia.slnx` pour inclure le nouveau projet.

### Critères de validation Phase 1
- [ ] `dotnet build` réussit sur macOS/Linux (pas de dépendance Windows)
- [ ] L'application s'ouvre, affiche une fenêtre avec NavigationView
- [ ] Les managers cross-platform s'initialisent (vérifier dans les logs)
- [ ] `pip`, `npm`, `cargo` (si installés) sont détectés par le backend

---

## PHASE 2 — Liste de paquets fonctionnelle (MVP : "on peut voir des paquets")

### Objectif
Afficher les paquets installés et les résultats de recherche dans une liste.

### Étapes

#### 2.1 — Créer le PackageWrapper Avalonia

Fichier : `src/UniGetUI.Avalonia/Models/PackageWrapper.cs`

Porter `src/UniGetUI/Controls/PackageWrapper.cs` (262 lignes) :
- Garde `INotifyPropertyChanged`
- Mêmes propriétés : `ListedOpacity`, `VersionComboString`, `MainIconId`, etc.
- Remplacer les types WinUI (`Microsoft.UI.*`) par les types Avalonia
- La logique métier reste identique à 100%

#### 2.2 — Créer la vue AbstractPackagesPage

Fichier : `src/UniGetUI.Avalonia/Views/PackagesPage.axaml`

Version simplifiée de `AbstractPackagesPage.xaml`. Structure :

```xml
<UserControl>
  <Grid RowDefinitions="Auto,*">
    <!-- Barre de recherche -->
    <Grid Grid.Row="0" ColumnDefinitions="*,Auto,Auto">
      <TextBox x:Name="SearchBox" Watermark="Search packages..." />
      <ComboBox x:Name="ManagerFilter" /> <!-- Filtre par manager -->
      <Button Content="Reload" x:Name="ReloadButton" />
    </Grid>

    <!-- Liste de paquets -->
    <ListBox Grid.Row="1" x:Name="PackageList"
             VirtualizationMode="Simple">
      <ListBox.ItemTemplate>
        <DataTemplate DataType="models:PackageWrapper">
          <Grid ColumnDefinitions="Auto,*,Auto,Auto,Auto" Opacity="{Binding ListedOpacity}">
            <CheckBox Grid.Column="0" IsChecked="{Binding IsChecked}" />
            <TextBlock Grid.Column="1" Text="{Binding Package.Name}" />
            <TextBlock Grid.Column="2" Text="{Binding Package.Id}" Opacity="0.6" />
            <TextBlock Grid.Column="3" Text="{Binding VersionComboString}" />
            <TextBlock Grid.Column="4" Text="{Binding Package.Source.Name}" Opacity="0.6" />
          </Grid>
        </DataTemplate>
      </ListBox.ItemTemplate>
    </ListBox>
  </Grid>
</UserControl>
```

#### 2.3 — Code-behind PackagesPage.axaml.cs

- Recevoir un `PackageLoader` (Discoverable, Installed, ou Upgradable) en paramètre
- S'abonner à `PackagesChanged` event du loader
- Remplir `ObservableCollection<PackageWrapper>` comme source de la ListBox
- Implémenter le filtrage texte (reprendre la logique de normalisation d'accents existante dans `AbstractPackagesPage.xaml.cs`)
- Implémenter le filtre par manager (ComboBox)

#### 2.4 — Brancher la navigation

Dans `MainWindow.axaml.cs` :
- "Discover" → `PackagesPage` avec `DiscoverablePackagesLoader.Instance`
- "Updates" → `PackagesPage` avec `UpgradablePackagesLoader.Instance`
- "Installed" → `PackagesPage` avec `InstalledPackagesLoader.Instance`

#### 2.5 — Barre de chargement

Ajouter un `ProgressBar IsIndeterminate="True"` visible pendant que `PackageLoader.IsLoading` est true.

### Critères de validation Phase 2
- [ ] La page "Installed" affiche les paquets pip/npm/cargo installés sur la machine
- [ ] La recherche filtre les paquets en temps réel
- [ ] Le filtre par manager fonctionne
- [ ] La liste est virtualisée (performance OK avec 500+ paquets)
- [ ] L'indicateur de chargement s'affiche pendant le scan

---

## PHASE 3 — Opérations install/update/uninstall (MVP : "on peut agir")

### Objectif
Permettre d'installer, mettre à jour et désinstaller des paquets, avec retour visuel.

### Étapes

#### 3.1 — Menu contextuel sur les paquets

Dans `PackagesPage`, ajouter un `ContextMenu` sur chaque item :
```xml
<ListBox.ContextMenu>
    <ContextMenu>
        <MenuItem Header="Install" Command="{Binding InstallCommand}" />
        <MenuItem Header="Update" Command="{Binding UpdateCommand}" />
        <MenuItem Header="Uninstall" Command="{Binding UninstallCommand}" />
        <Separator />
        <MenuItem Header="Package Details" Command="{Binding DetailsCommand}" />
    </ContextMenu>
</ListBox.ContextMenu>
```

Le double-clic lance l'action par défaut (install sur Discover, update sur Updates, details sur Installed).

#### 3.2 — Créer OperationControl Avalonia

Fichier : `src/UniGetUI.Avalonia/Controls/OperationControl.axaml`

Version simplifiée de `OperationControl.cs` (811 lignes WinUI → ~300 lignes Avalonia) :

```xml
<UserControl>
  <Grid ColumnDefinitions="Auto,*,Auto,Auto">
    <!-- Icône status -->
    <PathIcon Grid.Column="0" Data="{Binding StatusIcon}" />
    <!-- Info -->
    <StackPanel Grid.Column="1">
      <TextBlock Text="{Binding Title}" FontWeight="Bold" />
      <TextBlock Text="{Binding LastOutputLine}" Opacity="0.6" FontSize="11" />
    </StackPanel>
    <!-- Progress -->
    <ProgressBar Grid.Column="2" IsIndeterminate="{Binding IsRunning}" Width="120" />
    <!-- Cancel/Close -->
    <Button Grid.Column="3" Content="{Binding ActionButtonText}"
            Command="{Binding CancelCommand}" />
  </Grid>
</UserControl>
```

#### 3.3 — Brancher les opérations au backend

Le système d'opérations existant (`UniGetUI.PackageEngine.Operations`) est **déjà cross-platform**.

L'opération crée un `Process` avec les paramètres fournis par `IPackageOperationHelper.GetParameters()`.

Dans le code-behind de MainWindow :
1. Quand l'utilisateur lance une action, créer un `PackageOperation` (classe existante)
2. Ajouter le `PackageOperation` à l'`ObservableCollection<OperationControl>` de la liste d'opérations
3. L'opération se lance automatiquement via le système de queue existant
4. Mettre à jour l'UI via les événements `StatusChanged`, `ProgressChanged` de l'opération

#### 3.4 — Adaptation pour les couleurs de statut

Mapper les statuts d'opération aux brushes Avalonia Fluent :
- `InQueue` → `SystemFillColorNeutralBrush` (gris)
- `Running` → `SystemFillColorAttentionBrush` (orange)
- `Succeeded` → `SystemFillColorSuccessBrush` (vert)
- `Failed` → `SystemFillColorCriticalBrush` (rouge)
- `Canceled` → `SystemFillColorCautionBrush` (jaune)

Les noms de ressources sont les mêmes dans le Fluent theme d'Avalonia.

### Critères de validation Phase 3
- [ ] Clic droit → "Install" lance l'installation d'un paquet pip/npm
- [ ] La barre de progression montre l'avancement
- [ ] Le log de sortie défile en temps réel
- [ ] "Cancel" arrête l'opération
- [ ] Le statut final (succès/échec) s'affiche correctement
- [ ] Plusieurs opérations se lancent en parallèle (selon le réglage)

---

## PHASE 4 — Détails et paramètres (MVP : "expérience complète")

### Objectif
Ajouter la page de détails d'un paquet, les options d'installation, et les paramètres de l'application.

### Étapes

#### 4.1 — Page de détails paquet

Fichier : `src/UniGetUI.Avalonia/Views/PackageDetailsPage.axaml`

Adapter `PackageDetailsPage.xaml` (348 lignes XAML) :

```xml
<UserControl>
  <ScrollViewer>
    <StackPanel Spacing="16" Margin="24">
      <!-- En-tête : icône + nom + source -->
      <Grid ColumnDefinitions="80,*">
        <Image Width="80" Height="80" Source="{Binding IconSource}" />
        <StackPanel Grid.Column="1" Spacing="4">
          <TextBlock Text="{Binding Package.Name}" FontSize="24" FontWeight="Bold" />
          <TextBlock Text="{Binding Package.Id}" Opacity="0.6" />
          <TextBlock Text="{Binding Package.Source.Name}" />
        </StackPanel>
      </Grid>

      <!-- Description -->
      <TextBlock Text="{Binding Details.Description}" TextWrapping="Wrap" />

      <!-- Métadonnées (auteur, licence, URL...) -->
      <ItemsControl ItemsSource="{Binding MetadataItems}">
        <ItemsControl.ItemTemplate>
          <DataTemplate>
            <Grid ColumnDefinitions="150,*">
              <TextBlock Text="{Binding Key}" FontWeight="SemiBold" />
              <TextBlock Text="{Binding Value}" Grid.Column="1" TextWrapping="Wrap" />
            </Grid>
          </DataTemplate>
        </ItemsControl.ItemTemplate>
      </ItemsControl>

      <!-- Boutons d'action -->
      <StackPanel Orientation="Horizontal" Spacing="8">
        <Button Content="Install" Classes="accent" Command="{Binding InstallCommand}" />
        <Button Content="Options..." Command="{Binding ShowOptionsCommand}" />
      </StackPanel>
    </StackPanel>
  </ScrollViewer>
</UserControl>
```

Le chargement des détails utilise `IPackageDetailsHelper.GetDetails()` — déjà cross-platform.

#### 4.2 — Dialog options d'installation

Fichier : `src/UniGetUI.Avalonia/Views/InstallOptionsDialog.axaml`

Utiliser `FluentAvalonia.UI.Controls.ContentDialog` :
- Version personnalisée (ComboBox)
- Scope (si le manager le supporte — vérifier `Capabilities.SupportsCustomScopes`)
- Architecture (si supportée)
- Paramètres custom (TextBox)
- Skip integrity check (CheckBox, si supporté)

Le dialog lit `ManagerCapabilities` pour afficher/masquer les options pertinentes.

#### 4.3 — Page de paramètres

Fichier : `src/UniGetUI.Avalonia/Views/SettingsPage.axaml`

Version simplifiée combinant les 10 pages de paramètres WinUI en une seule page scrollable.

Recréer les widgets settings :
- `CheckboxCard` → `CheckBox` dans un `Border` stylisé (ou `SettingsExpander` de FluentAvalonia)
- `ComboboxCard` → `ComboBox` dans un `Border` stylisé
- `TextboxCard` → `TextBox` dans un `Border` stylisé

FluentAvalonia fournit `SettingsExpander` et `SettingsExpanderItem` — équivalents directs des settings cards WinUI.

Sections essentielles pour le PoC :
- Général : Thème, langue, démarrage au login
- Gestionnaires : Activer/désactiver chaque manager
- Mises à jour : Intervalle de vérification, notifications

Le settings engine (`Settings.Get()`/`Settings.Set()`) est 100% portable — aucun changement nécessaire.

#### 4.4 — Système d'icônes

Option A (rapide) : Utiliser les icônes Fluent System intégrées dans FluentAvalonia (`SymbolIcon`/`FASymbolIcon`)
Option B (fidèle) : Embarquer la police `UniGetUI-Symbols.ttf` et créer un contrôle `LocalIcon` Avalonia

Pour le PoC, Option A est suffisante. Mapper les `IconType` enum aux `Symbol` enum de FluentAvalonia.

### Critères de validation Phase 4
- [ ] Double-clic sur un paquet ouvre la page de détails
- [ ] Les métadonnées (description, auteur, licence, URL) s'affichent
- [ ] Le bouton "Options" ouvre un dialog avec les options pertinentes au manager
- [ ] La page Settings permet de changer le thème (clair/sombre)
- [ ] Activer/désactiver un manager dans les paramètres fonctionne

---

## PHASE 5 — Intégrations macOS et polish (MVP : "prêt pour démo")

### Objectif
Ajouter les intégrations spécifiques macOS, le system tray, les notifications, et les finitions.

### Étapes

#### 5.1 — System Tray (menu bar macOS)

Avalonia fournit `TrayIcon` nativement :

```xml
<!-- App.axaml -->
<TrayIcon Icon="/Assets/icon.png" ToolTipText="UniGetUI">
    <TrayIcon.Menu>
        <NativeMenu>
            <NativeMenuItem Header="Open UniGetUI" Command="{Binding ShowWindowCommand}" />
            <NativeMenuItemSeparator />
            <NativeMenuItem Header="Available Updates: 0" IsEnabled="False" />
            <NativeMenuItemSeparator />
            <NativeMenuItem Header="Update All" Command="{Binding UpdateAllCommand}" />
            <NativeMenuItem Header="Quit" Command="{Binding QuitCommand}" />
        </NativeMenu>
    </TrayIcon.Menu>
</TrayIcon>
```

Brancher `UpgradablePackagesLoader.PackagesChanged` pour mettre à jour le compteur de mises à jour.

#### 5.2 — Notifications macOS

Créer `src/UniGetUI.Avalonia/Services/NotificationService.cs` :

Option A (simple, pour PoC) : Utiliser `Process.Start("osascript", ...)` :
```csharp
Process.Start("osascript", $"-e 'display notification \"{message}\" with title \"{title}\"'");
```

Option B (propre) : Utiliser un package comme `DesktopNotifications` (NuGet, cross-platform).

#### 5.3 — Elevation (sudo) sur macOS

Créer `src/UniGetUI.Avalonia/Services/ElevationService.cs` :

Pour les opérations nécessitant des droits admin sur macOS :
- Utiliser `osascript -e 'do shell script "command" with administrator privileges'`
- Ou intégrer avec `pkexec` sur Linux

Note : La plupart des managers cross-platform (pip, npm, cargo) n'ont PAS besoin de sudo sur macOS.

#### 5.4 — Remplacements ExternalLibraries

**Clipboard** : Avalonia fournit `TopLevel.GetTopLevel(this)?.Clipboard` — rien à faire.

**FilePickers** : Avalonia fournit `StorageProvider` :
```csharp
var files = await TopLevel.GetTopLevel(this)?.StorageProvider
    .OpenFilePickerAsync(new FilePickerOpenOptions { ... });
```

#### 5.5 — Gestionnaire Homebrew (optionnel pour la démo)

Si le temps le permet, créer `src/UniGetUI.PackageEngine.Managers.Homebrew/` :

```
UniGetUI.PackageEngine.Managers.Homebrew/
├── Homebrew.cs
├── UniGetUI.PackageEngine.Managers.Homebrew.csproj
└── Helpers/
    ├── HomebrewPkgOperationHelper.cs
    └── HomebrewPkgDetailsHelper.cs
```

**Homebrew.cs** — Implémenter les méthodes abstraites :
- `_loadManagerExecutableFile()` : Chercher `brew` dans `/opt/homebrew/bin/brew` (Apple Silicon), `/usr/local/bin/brew` (Intel), puis `which brew`
- `_loadManagerVersion()` : Parser `brew --version`
- `FindPackages_UnSafe()` : Parser `brew search --formulae <query>` (une formule par ligne)
- `GetInstalledPackages_UnSafe()` : Parser `brew list --formulae --versions` (format : `name version`)
- `GetAvailableUpdates_UnSafe()` : Parser `brew outdated --formulae` (format : `name installed_ver -> new_ver`)

**HomebrewPkgOperationHelper.cs** :
- `_getOperationParameters()` :
  - Install : `["install", packageId]` (+ `"--cask"` si cask)
  - Update : `["upgrade", packageId]`
  - Uninstall : `["uninstall", packageId]`
- `_getOperationResult()` : Code retour 0 = Success, autre = Failure

**HomebrewPkgDetailsHelper.cs** :
- `GetDetails_UnSafe()` : Parser `brew info --json=v2 <packageId>` (JSON riche avec description, homepage, license, dependencies)
- `GetInstallableVersions_UnSafe()` : Homebrew ne supporte pas facilement les versions multiples → retourner liste vide ou version actuelle
- `GetIcon_UnSafe()` : Retourner null (Homebrew n'a pas d'icônes de paquets)
- `GetScreenshots_UnSafe()` : Retourner liste vide
- `GetInstallLocation_UnSafe()` : Retourner `brew --prefix <packageId>`

**ManagerCapabilities pour Homebrew** :
```csharp
Capabilities = new ManagerCapabilities
{
    CanRunAsAdmin = false,           // Homebrew refuse de tourner en root
    SupportsCustomVersions = false,  // Pas de version pinning simple
    SupportsCustomScopes = false,    // Pas de scope user/machine
    CanListDependencies = true,      // brew deps
    SupportsCustomSources = true,    // brew tap
    SupportsPreRelease = false,
    CanDownloadInstaller = false,
};
```

**Cask support** (bonus) : Homebrew a deux types — formulae (CLI tools) et casks (GUI apps).
- `brew list --cask --versions` pour les casks
- `brew search --cask <query>` pour la recherche
- Implémenter comme deux sources du même manager, ou comme suffixe sur les commandes

#### 5.6 — Polish UI

- Icône d'application macOS (`.icns`)
- Titre de fenêtre et barre de titre macOS native (Avalonia le gère automatiquement)
- Raccourcis clavier : Cmd+F (recherche), Cmd+, (préférences), Cmd+Q (quitter)
- Thème qui suit le mode sombre macOS (Avalonia Fluent le fait automatiquement)
- Badge de mise à jour sur l'icône Dock (via `NativeMenu` ou API macOS native)

#### 5.7 — Distribution

- Créer un bundle `.app` macOS : `dotnet publish -r osx-arm64 --self-contained`
- Empaqueter dans un `.dmg` avec `create-dmg` (outil open-source)
- Fichier `Info.plist` pour les métadonnées macOS

### Critères de validation Phase 5
- [ ] L'icône apparaît dans la barre de menu macOS (system tray)
- [ ] Le menu tray affiche le nombre de mises à jour disponibles
- [ ] Les notifications macOS s'affichent après une installation
- [ ] Le Clipboard et les FilePickers fonctionnent
- [ ] (Optionnel) Homebrew apparaît comme manager et détecte les paquets installés
- [ ] L'app se distribue comme un `.app` bundle fonctionnel

---

## Résumé des phases

| Phase | Nom | Livrable | Effort estimé |
|---|---|---|---|
| 1 | Squelette | Fenêtre Avalonia + backend initialisé | 1-2 jours |
| 2 | Liste de paquets | Voir/chercher/filtrer les paquets | 2-3 jours |
| 3 | Opérations | Install/update/uninstall fonctionnels | 2-3 jours |
| 4 | Détails + Settings | Expérience utilisateur complète | 3-4 jours |
| 5 | Intégrations macOS | Tray, notifications, Homebrew, .app | 3-5 jours |
| **Total** | | **PoC complet** | **~11-17 jours** |

## Arborescence finale du PoC

```
src/
├── UniGetUI.Avalonia/                          # NOUVEAU - Projet UI Avalonia
│   ├── UniGetUI.Avalonia.csproj
│   ├── Program.cs
│   ├── App.axaml / App.axaml.cs
│   ├── Assets/
│   │   └── icon.png
│   ├── Views/
│   │   ├── MainWindow.axaml / .axaml.cs        # Shell navigation
│   │   ├── PackagesPage.axaml / .axaml.cs      # Liste de paquets (réutilisable)
│   │   ├── PackageDetailsPage.axaml / .axaml.cs
│   │   ├── SettingsPage.axaml / .axaml.cs
│   │   └── InstallOptionsDialog.axaml / .axaml.cs
│   ├── Controls/
│   │   ├── OperationControl.axaml / .axaml.cs
│   │   └── SettingsWidgets/                    # Cards de paramètres
│   ├── Models/
│   │   └── PackageWrapper.cs                   # Adapté depuis WinUI
│   └── Services/
│       ├── NotificationService.cs              # Abstraction notifications
│       └── ElevationService.cs                 # Abstraction sudo/admin
│
├── UniGetUI.PackageEngine.Managers.Homebrew/   # NOUVEAU - Manager Homebrew (Phase 5)
│   ├── UniGetUI.PackageEngine.Managers.Homebrew.csproj
│   ├── Homebrew.cs
│   └── Helpers/
│       ├── HomebrewPkgOperationHelper.cs
│       └── HomebrewPkgDetailsHelper.cs
│
├── UniGetUI.Core.*/                            # EXISTANT - Aucun changement
├── UniGetUI.PackageEngine.*/                   # EXISTANT - Aucun changement
├── UniGetUI.Interface.*/                       # EXISTANT - Aucun changement
└── UniGetUI/                                   # EXISTANT - App WinUI Windows (inchangée)
```

## Fichiers existants à modifier

| Fichier | Changement | Phase |
|---|---|---|
| `src/UniGetUI.Avalonia.slnx` | Ajouter `UniGetUI.Avalonia.csproj` et `Managers.Homebrew.csproj` | 1, 5 |
| `src/UniGetUI.PackageEngine.PackageEngine/PEInterface.cs` | Ajouter Homebrew dans le bloc non-WINDOWS | 5 |
| Aucun autre fichier existant ne devrait être modifié | | |

## Risques et mitigations

| Risque | Impact | Mitigation |
|---|---|---|
| FluentAvalonia NavigationView pas 100% compatible | Moyen | Fallback sur TabControl simple |
| Managers cross-platform ne trouvent pas les exécutables sur macOS | Faible | Les managers utilisent déjà `which`/PATH lookup |
| Performance ListView avec beaucoup de paquets | Moyen | Activer virtualisation Avalonia |
| Avalonia Fluent theme look & feel différent | Faible | Acceptable pour un PoC |
| `brew` output format change entre versions | Faible | Parser `--json` quand possible |
