# nuget-tcg

[![Documentation](https://img.shields.io/badge/Documentation-GitHub%20Pages-blue?style=for-the-badge)](https://italbytz.github.io/nuget-tcg/)

`nuget-tcg` is a small foundation repository for reusable trading card game contracts and adapters.

The initial scope is intentionally narrow:

- game-agnostic card and set abstractions
- repository contracts for persistence layers
- a `TCGDex` adapter for the Pokemon API used by the reference project
- in-memory repository implementations as a lightweight reference for later database adapters

## Why this repository exists

The reference Pokemon project currently talks to `https://api.tcgdex.net` and keeps favorites only in memory.

This repository extracts the reusable parts into packages that can later support:

- Pokemon
- Magic: The Gathering
- other TCGs with similar concepts such as cards, sets, series, images, and external metadata

## Packages in this repository

### `Italbytz.Tcg.Abstractions`

Common domain classes and contracts for cards, sets, sources, and repositories.

### `Italbytz.Tcg.Tcgdex`

HTTP client for `TCGDex` mapped onto the shared abstractions.

### `Italbytz.Tcg.Scryfall`

HTTP client for `Scryfall` mapped onto the shared abstractions for Magic: The Gathering.

### `Italbytz.Tcg.InMemory`

In-memory repository implementations useful for tests, prototypes, and local caching.

## Suggested next packages

- `Italbytz.Tcg.Sqlite`
- `Italbytz.Tcg.EntityFramework`
- `Italbytz.Tcg.MtgJson`

## Documentation

- Product documentation: `https://italbytz.github.io/nuget-tcg/`

## Local validation

```bash
dotnet tool restore
dotnet restore nuget-tcg.sln
dotnet test nuget-tcg.sln -v minimal
dotnet pack nuget-tcg.sln -c Release -v minimal
dotnet tool run docfx docfx/docfx.json
```