Include ..\AGENTS.md

# Storage Monitor — Mod-Specific Agent Instructions

## Identity
- **Assembly:** `storagemonitor`
- **Namespace:** `Calloatti.StorageMonitor`
- **ModId:** `Calloatti.StorageMonitor`
- **Framework:** Bindito DI
- **Publicizer:** `Timberborn.GameDistricts`, `Timberborn.BlueprintSystem`, `Timberborn.CoreUI` are publicized via `CommonModSettings.props`, with `DoNotPublicize` for `RadioToggle.RadioButtonSelected` and `ComponentSpec.EqualityContract`/`PrintMembers` (see csproj)
- **Min Game Version:** 1.0.12.5 — uses `timberborn-decompiled-1.0.*`

## What This Mod Does
Adds a storage monitor building that displays real-time storage amounts and fill levels. Includes banner setter, goods dropdown selector, and entity panel fragment.

## Source Architecture (`Version-1.0/Source/`)

| File | Role |
|---|---|
| `StorageMonitorModStarter.cs` | Entry point — `IModStarter` |
| `StorageMonitorConfigurator.cs` | DI configurator |
| `StorageMonitor.cs` | Core monitor component |
| `StorageMonitorFragment.cs` | Entity panel UI fragment |
| `StorageMonitorBannerSetter.cs` | Banner visual customization |
| `StorageMonitorGoodsDropdownProvider.cs` | Goods dropdown data provider |
| `StorageMonitorSpec.cs` | ComponentSpec record |

## Version Folders
- `Version-1.0` — targets game 1.0.x.x
- `Version-1.1` — targets game 1.1.x.x

## Hard Rule
DO NOT EVER TOUCH THE DEPLOY FOLDER.

BUILD DOES EVERYTHING, NEVER EVER MESS WITH THE DEPLOY PROCESS.
