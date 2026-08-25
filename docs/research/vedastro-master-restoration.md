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

The six-arc-second House 1 difference is from exercising the local route with explicit rounded coordinates while the hosted snapshot used its named-location geocoder.

### Full legacy suite

The complete `LibraryTests` run currently reports 65 passed and 23 failed out of 88. These failures are not one uniform category:

- explicit unfinished tests: `NextLunarEclipse`, `GocharaKakshas`, `NextNewMoon`, `SunriseTime`, `LunarDay`, `AbstractActivityStrength`, and `EventSlicesToEvents` contain unconditional `Assert.Fail` calls;
- stale test contracts: `LMTToSTD` asserts null for a value type, the bird-activity tests compare enums with the placeholder string `"O"`, and Tajika test comments still request replacement expected values;
- source/book disagreements: isolated old fixtures for ayanamsa, longitude, Ishta/Kashta, and Ashtakavarga differ from the recovered first-party algorithm while the hosted-oracle fixtures pass;
- genuine remaining validation work: Jaimini chara dasa, some yoga/chart collection behavior, and a legacy divisional-longitude fixture are not yet production-verified.

The failing tests remain enabled so the repository does not imply that unverified domains are complete.

## Known boundaries

This is a complete build restoration, not a claim of exact private-production parity across every astrological system. The following remain explicit boundaries:

1. The deployed server binary/source is not public, so methods absent from all public history can only be reconstructed from metadata and observable API behavior.
2. The hosted Jaimini endpoint does not expose enough structured period data to validate the provisional chara-dasa wrapper. It should not be promoted as production-equivalent yet.
3. D2 behavior returned by the hosted API conflicts with the classical table and the repository's own D2 tests. The port retains the first-party/classical implementation pending an upstream contract decision.
4. Named-place geocoding requires configured Azure Maps or Google credentials. Coordinate-based calculations work without them, but existing `LocationManager` fallback behavior can substitute its empty/default location when providers are unavailable.
5. Storage-, email-, and other cloud-backed API routes still require their documented deployment secrets. Calculation routes using explicit coordinates do not.

## Recommended next tranche

Treat this restoration as the new integration baseline. The next work should be narrow, oracle-backed tranches: first obtain an authoritative Jaimini fixture or upstream artifact, then resolve yoga/chart and divisional disagreements one domain at a time. Placeholder tests should be rewritten into real contracts separately from calculation changes so test cleanup cannot silently redefine astronomy behavior.
