# GBA.Ecommerce (Standalone)

This repository is an extracted standalone version of the ecommerce backend from `gba-server`.

## Included Projects

- `src/GBA.Ecommerce`
- `src/GBA.Search`
- `src/GBA.Services`
- `src/GBA.Data`
- `src/GBA.Domain`
- `src/GBA.Common`

## Solution

- `GBA.Ecommerce.slnx`

## Quick Start

1. Install .NET SDK `10.0.100` (see `global.json`).
2. Restore:

   `dotnet restore GBA.Ecommerce.slnx`

3. Build:

   `dotnet build src/GBA.Ecommerce/GBA.Ecommerce.csproj --no-restore`
