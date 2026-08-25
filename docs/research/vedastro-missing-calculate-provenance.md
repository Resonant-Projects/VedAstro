# Provenance of the missing VedAstro `Calculate` implementation

Research date: 2026-08-24

## Bottom line

The missing engine is not wholly lost. A complete, buildable first-party calculator exists in VedAstro's official Git history, but not on the current `master` line:

- Official tag [`40763952-2665-stable`](https://github.com/VedAstro/VedAstro/tree/40763952742f76369a505d8db2e9e9fa67f75d78) contains [`Library/Logic/Calculate/Calculate.cs`](https://github.com/VedAstro/VedAstro/blob/40763952742f76369a505d8db2e9e9fa67f75d78/Library/Logic/Calculate/Calculate.cs), a 13,999-line implementation. Building `Library/Library.csproj` at that exact commit in the .NET 7 SDK succeeds with zero errors.
- The newest recoverable full first-party file I found is the 14,385-line `Calculate.cs` at maintainer commit [`e1e65d81450e3387115e50cfc6eaf1bdc48939bb`](https://github.com/VedAstro/VedAstro/commit/e1e65d81450e3387115e50cfc6eaf1bdc48939bb) (2024-10-29). The same blob survives through official commit [`505d1f55f93122acecc8a75df9e9aa4747349fc1`](https://github.com/VedAstro/VedAstro/commit/505d1f55f93122acecc8a75df9e9aa4747349fc1), reached by merged PRs 150-153.
- Current `master` instead has only the 3,738-line [`Library/Logic/Calculate/Core.cs`](https://github.com/VedAstro/VedAstro/blob/fcb4dede360372545eb244c53e9a80ec3510e194/Library/Logic/Calculate/Core.cs). It is a partial calculator: it contains many callers of lower-level methods but omits the corresponding declarations.

This makes the stable tag a viable source baseline and the later historical file a valuable donor. It does **not** make either file a drop-in repair for current `master`: `master` renamed and expanded APIs after the last full-source snapshot, so a deliberate compatibility port is required.

## What exists in official Git

### Only one live branch

GitHub's official [branches API](https://api.github.com/repos/VedAstro/VedAstro/branches) currently returns only `master`. There is no hidden live development branch from which the missing file can simply be checked out. The official [tags API](https://api.github.com/repos/VedAstro/VedAstro/tags), however, exposes 70 historical deployment-style tags, including `40763952-2665-stable`.

### The stable source really contains the core algorithms

At `40763952-2665-stable`, `Calculate.cs` contains concrete implementations rather than stubs. Examples include:

- `PlanetZodiacSign` at [line 720](https://github.com/VedAstro/VedAstro/blob/40763952742f76369a505d8db2e9e9fa67f75d78/Library/Logic/Calculate/Calculate.cs#L720), the predecessor of `PlanetRasiD1Sign`;
- `PlanetNirayanaLongitude` at [line 8306](https://github.com/VedAstro/VedAstro/blob/40763952742f76369a505d8db2e9e9fa67f75d78/Library/Logic/Calculate/Calculate.cs#L8306);
- `HouseJunctionPoint` at [line 9101](https://github.com/VedAstro/VedAstro/blob/40763952742f76369a505d8db2e9e9fa67f75d78/Library/Logic/Calculate/Calculate.cs#L9101); and
- `ZodiacSignsOwnedByPlanet` at [line 9194](https://github.com/VedAstro/VedAstro/blob/40763952742f76369a505d8db2e9e9fa67f75d78/Library/Logic/Calculate/Calculate.cs#L9194).

The later full-source commit `e1e65d8` retains these implementations (for example, [`PlanetNirayanaLongitude`](https://github.com/VedAstro/VedAstro/blob/e1e65d81450e3387115e50cfc6eaf1bdc48939bb/Library/Logic/Calculate/Calculate.cs#L8687), [`HouseJunctionPoint`](https://github.com/VedAstro/VedAstro/blob/e1e65d81450e3387115e50cfc6eaf1bdc48939bb/Library/Logic/Calculate/Calculate.cs#L9482), and [`ZodiacSignsOwnedByPlanet`](https://github.com/VedAstro/VedAstro/blob/e1e65d81450e3387115e50cfc6eaf1bdc48939bb/Library/Logic/Calculate/Calculate.cs#L9575)).

### The repository contains parallel histories

The source loss is best explained by parallel commit lines, not by an ordinary deletion on the present line:

- Full-source maintainer commit [`e1e65d8`](https://github.com/VedAstro/VedAstro/commit/e1e65d81450e3387115e50cfc6eaf1bdc48939bb) has message `birth time finder API update` and includes `Calculate.cs`.
- Present-master commit [`7bc99d5`](https://github.com/VedAstro/VedAstro/commit/7bc99d5d21d096088befe13a3c73aaa828b3cd80) has the same author timestamp and message, but is on a different ancestry line and has neither `Calculate.cs` nor `Core.cs`.
- Commit [`89233a8`](https://github.com/VedAstro/VedAstro/commit/89233a86004968e2b18541e9cd36e2c6143c7217) later adds the 3,738-line `Core.cs` while also updating call sites and generated OpenAPI metadata to newer names such as `PlanetRasiD1Sign`. It does not add the remaining implementation.

The full-source and present-master lines share an older merge base but neither `e1e65d8` nor the stable tag is an ancestor of current `master`. A merge/cherry-pick therefore cannot be assumed to preserve semantics.

## Coverage of the 698-error build

The current build log has 687 missing-name diagnostics (`CS0117`/`CS0103`) across 113 distinct names; the other 11 errors are different compiler categories. Searching the latest full `Calculate.cs` finds 59 of those 113 names literally. Most high-impact low-level implementations are among those 59.

The other 54 names are largely post-snapshot names or expanded divisional-chart wrappers. Examples:

- `PlanetRasiD1Sign` corresponds to historical `PlanetZodiacSign`;
- `HouseNavamshaD9Sign` and `PlanetNavamshaD9Sign` correspond to historical `HouseNavamsaSign` and `PlanetNavamsaSign`;
- current `AllHouse*`/`AllPlanet*` D-chart families are broader than the exact names exported by the historical file.

Current [`OpenAPIStaticTable.cs`](https://github.com/VedAstro/VedAstro/blob/fcb4dede360372545eb244c53e9a80ec3510e194/Library/Data/OpenAPIStaticTable.cs) preserves the intended public signatures and descriptions, but it is metadata, not an implementation. It is useful as a porting checklist alongside the historical source.

An experimental graft confirms the compatibility issue:

- Adding historical `Calculate.cs` beside current `Core.cs` changes the missing-member cascade into duplicate-member/type-drift errors.
- Replacing `Core.cs` with a historical `Calculate.cs` initially exposes declaration conflicts and, after resolving those, further renamed-member errors.

Therefore the historical file is a source donor, not a one-file zero-error patch for `master`.

## Packages, releases, artifacts, and related repositories

### NuGet is too old

NuGet's authoritative [flat-container version index](https://api.nuget.org/v3-flatcontainer/vedastro.library/index.json) lists only `1.0.0` and `1.2.0`. Both packages date from March 2023 and contain only a compiled `VedAstro.Library.dll`, not a source package. Their metadata points back to the official repository. Their assemblies expose the older `GetPlanetNirayanaLongitude`/`GetPlanetRasiSign` naming family, not the modern missing API. The [NuGet package page](https://www.nuget.org/packages/VedAstro.Library) is therefore useful for the old baseline, not for recovering the post-2024 implementation.

### No GitHub releases or retained Actions artifact

The official repository's [releases API](https://api.github.com/repos/VedAstro/VedAstro/releases) returns no releases. The repository exposes no retained GitHub Actions build artifact containing a modern DLL/source bundle. Historical tags are the only first-party release-like Git refs found.

### A committed July 2024 library DLL is decompilable

The repository itself contains a 3.45 MB [`VedAstro.Library.dll`](https://github.com/VedAstro/VedAstro/blob/fcb4dede360372545eb244c53e9a80ec3510e194/Desktop/MacOS/Launcher2.app/Contents/Resources/api-build/VedAstro.Library.dll) in the macOS launcher's bundled API. Git history dates that blob to maintainer commit [`ef5c8faf`](https://github.com/VedAstro/VedAstro/commit/ef5c8faf89ecae6e30152217a596115d2e951c72) (2024-07-21). ILSpy successfully decompiles it and shows concrete `Calculate`, `CalculateHoroscope`, and `Krishnamurti` types, including `PlanetNirayanaLongitude` and `HouseJunctionPoint` bodies.

This binary predates the October 2024 full-source donor, so it is not the best primary source for `Calculate.cs`. It is still useful for recovering or validating helper types that disappeared from the present tree and for demonstrating that the omitted engine was included in first-party workstation builds after its source stopped appearing on the present ancestry line. Decompiled output should be treated as a secondary reconstruction artifact because comments, some local names, and original formatting are lost.

### Related official repos are clients and documentation

The official [organization repository list](https://api.github.com/orgs/VedAstro/repos?per_page=100) includes `API-Docs`, `VedAstro.js`, `VedAstro.Python`, `VedAstro.PHP`, `VedAstro.Swift`, examples, MCP, and generated docs. Organization-wide code search finds uses/documentation of names such as `PlanetNirayanaLongitude`, but no second C# engine implementation. These projects call the hosted API; they do not contain the missing calculation body.

### The deployment is an oracle, not a downloadable engine

The live site loads [`Loader.js`](https://vedastro.org/js/Loader.js), which in turn loads JavaScript clients such as [`VedAstro.js`](https://vedastro.org/js/VedAstro.js). It is now a static JavaScript frontend, not a Blazor WebAssembly deployment: conventional `_framework/blazor.boot.json`, `.dll`, and `.wasm` paths resolve to the same SPA fallback HTML rather than binary artifacts. The calculator runs server-side behind the live [API](https://api.vedastro.org/), whose generated documentation exposes calls but not the server DLL or source.

No recoverable official deployed DLL/WASM/source-map containing the modern engine was found. Obtaining the exact code running in production would require the maintainer to publish or supply the deployment output/source from their private build environment.

## Recommended recovery paths

### 1. Lowest-risk: use the last buildable stable source as the engine baseline

Start an integration branch at exact commit `40763952742f76369a505d8db2e9e9fa67f75d78`, retain its complete `Calculate.cs`, and port only required fixes/features forward with API-oracle regression tests. This is the only discovered official snapshot that combines a named stable tag, complete source, and a clean library build.

Useful commands:

```bash
git fetch https://github.com/VedAstro/VedAstro.git \
  refs/tags/40763952-2665-stable:refs/tags/upstream-40763952-2665-stable
test "$(git rev-parse upstream-40763952-2665-stable^{commit})" = \
  "40763952742f76369a505d8db2e9e9fa67f75d78" || exit 1
git show upstream-40763952-2665-stable:Library/Logic/Calculate/Calculate.cs
```

### 2. If `master` APIs are mandatory: reconstruct with an explicit compatibility layer

Use `e1e65d81450e3387115e50cfc6eaf1bdc48939bb:Library/Logic/Calculate/Calculate.cs` as the newest full donor, then:

1. inventory the 113 missing members from the compiler output;
2. map historical names to the post-`89233a8` public signatures;
3. keep current standalone partial files (for example newer Numerology/Vargas code) and remove duplicate historical regions deliberately;
4. implement only genuinely new wrappers after exact-equivalence tests;
5. test planetary longitude, lagna, houses, all vargas, shadbala, and dasa against the hosted API and Swiss Ephemeris fixtures.

Do not paste `Calculate.cs` beside `Core.cs`; the overlapping partial-class members guarantee duplicate-definition errors.

### 3. Exact production parity: ask upstream for the private engine/deployment artifact

The hosted API demonstrates that a more complete build exists somewhere, but neither GitHub, NuGet, related repos, nor public web assets expose it. A maintainer-provided source archive, server DLL/PDB, container image, or Azure deployment package is the only direct recovery route for the exact production implementation. Without that, API-guided reconstruction is necessarily a clean-room compatibility port and should be described as such.

## Recommendation

Treat `40763952-2665-stable` as the authoritative recoverable implementation and the live API as the behavioral oracle. Do not treat current `master` as an engine source baseline until the historical code has been ported behind tests. The code can be recovered substantially, but the modern production delta cannot be recovered exactly from any currently public first-party artifact.
