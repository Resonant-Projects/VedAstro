# VedAstro master calculator restoration

Date: 2026-08-25

## Result

The public `master` API and library now compile with the recovered calculation engine. The original build failed with 698 errors, predominantly references to missing `Calculate` members. Both of these commands now complete with zero errors in the .NET 7 SDK:

```bash
dotnet build Library/Library.csproj
dotnet build API/API.csproj
```

The API also builds as the repository's Docker image and serves real calculation routes.

## Recovery design

The restoration deliberately separates provenance from compatibility:

- `Calculate.cs` uses the newest complete first-party source found in official history (`e1e65d81450e3387115e50cfc6eaf1bdc48939bb`) as its algorithmic donor. Regions already implemented by current, independently maintained partial classes were removed instead of compiling duplicate implementations.
- `CalculateKP.cs` and the historical horoscope rules restore first-party implementations that disappeared from the current ancestry line.
- `Compatibility.cs` implements the modern names and D-chart API surface expected by current call sites. It delegates to the recovered algorithms instead of duplicating astronomical calculations.
- `HoroscopeAshtakavarga.cs` restores the missing modern horoscope/Ashtakavarga rule members from the condition catalog retained in the repository.
- `APITools.cs` restores the response envelope and request helpers required by the current Azure Functions API. `host.json` is now a tracked, non-secret runtime file; `local.settings.json` remains ignored.

The source lineage and why this is a port rather than a merge are documented in [vedastro-missing-calculate-provenance.md](vedastro-missing-calculate-provenance.md).

## Correctness work beyond compilation

Compilation exposed several historical edge-case defects. The restoration includes targeted corrections for:

- circular angle normalization, including the Sun's chesta bala and conjunctions across 0 degrees;
- house-junction midpoints that cross 0 degrees;
- lunar-month leap detection using the solar signs at the surrounding new moons;
- Tajika annual return search using the sidereal solar longitude;
- the normalized Ishta/Kashta benefic-malefic balance returned by production;
- conventional `(latitude, longitude)` input recovery in `GeoLocation` when the second coordinate cannot be a latitude;
- current `MemoryCache` enumeration without reflection into removed runtime internals;
- JSON serialization when a calculation returns a `JProperty`.

## Verification

### Hosted API oracle

`HostedOracleRegressionTests` freezes public `vedastro.org` results for two charts. The recovered implementation matches:

- Sun and Moon nirayana longitudes to one arc-second;
- lagna sign and longitude to ten arc-seconds;
- D9 sign and longitude to one arc-second;
- Sun Ishta and Kashta scores to `0.000001`;
- Venus's normalized Ishta/Kashta balance to `0.0005`.

The broader focused suites for D1-D3, Ashtakavarga, horoscope initialization, lunar month, Tajika solar return, and house junctions pass.

### HTTP/container smoke test

The rebuilt Azure Functions container returns successful JSON envelopes for the public `Calculate` routes. For Singapore at J2000, it returned:

| Calculation | Recovered API result | Hosted oracle |
| --- | ---: | ---: |
| Sun nirayana longitude | 256.1758333333 degrees | 256.1758333333 degrees |
| D9 Sun | Leo 25.5797222222 degrees | Leo 25.5797222222 degrees |
| Sun Ishta score | 8.5872649871 | 8.5872649871 |
| House 1 | Aquarius 29.5238888889 degrees | Aquarius 29.5255555556 degrees |

Both House 1 values use the Singapore J2000 fixture at explicit longitude `103.85` and latitude `1.289`; the hosted value is the frozen literal `29.5255555556` degrees. The evidence establishes only the observed six-arc-second difference, not a geocoder-specific cause.

### Full legacy suite

The complete `LibraryTests` run now reports 87 passed, 9 skipped, and 0 failed out of 96. The suite resets mutable calculation settings before every `CalculateTests` case, replaces debugger-era `Assert.Fail` calls with deterministic behavioral assertions, uses type-correct contracts for Pancha Pakshi activity results, and adds focused regressions for recovered compatibility, unsupported calculations, input overflow/rounding, coordinate recovery, and divisional/KP edge cases.

The nine skipped tests remain named and carry explicit reasons:

- Jaimini chara dasa still needs an authoritative structured fixture;
- the Parvata and whole-sign house fixtures lack cited source charts;
- historical Ishta/Kashta and Ashtakavarga book tables disagree with the recovered first-party engine and hosted-oracle coverage;
- Tajika planetary longitudes are still labeled as placeholder expected values in the legacy test.

These cases are quarantined rather than rewritten to match the implementation, so unresolved validation boundaries remain visible without making every test run fail by construction.

## Known boundaries

This is a complete build restoration, not a claim of exact private-production parity across every astrological system. The following remain explicit boundaries:

1. The deployed server binary/source is not public, so methods absent from all public history can only be reconstructed from metadata and observable API behavior.
2. The hosted Jaimini endpoint does not expose enough structured period data to validate the provisional chara-dasa wrapper. It should not be promoted as production-equivalent yet.
3. D2 behavior returned by the hosted API conflicts with the classical table and the repository's own D2 tests. The port retains the first-party/classical implementation pending an upstream contract decision.
4. Named-place geocoding requires Azure Maps or Google credentials to be configured. Coordinate-based calculations work without them, but existing `LocationManager` fallback behavior can substitute its empty/default location when providers are unavailable.
5. Storage-, email-, and other cloud-backed API routes still require their documented deployment secrets. Calculation routes using explicit coordinates do not.
6. The historical horoscope methods from `VasiYoga` through `MalikaYoga` have no surviving implementation. Their `false`/`NotOccuring` values are compatibility sentinels, not verified non-occurrences.

## Recommended next tranche

Treat this restoration as the new integration baseline. The next work should be narrow, oracle-backed tranches: first obtain an authoritative Jaimini fixture or upstream artifact, then resolve yoga/chart and divisional disagreements one domain at a time. Placeholder tests should be rewritten into real contracts separately from calculation changes so test cleanup cannot silently redefine astronomy behavior.
