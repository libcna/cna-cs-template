# CNA C# game template

This is both a small CNA-backed game and an installable `dotnet new` template. The game code uses
the `Microsoft.Xna.Framework` API supplied by `CNA.XnaCompat`; the one engine-specific capability
query is isolated in `EngineDiagnostics.cs`.

The sample exercises the game lifecycle, graphics-device management, resize handling, keyboard,
mouse and gamepad input, raw PNG decoding, `Texture2D`, `SpriteBatch`, and a rotating
`BasicEffect` cube. CNA renderers without a 3D pipeline receive a bouncing 2D fallback.

## Build with CNA

Two explicit consumer modes are supported. Development mode (the default) references a CNA.NET
source checkout and keeps the fast sibling-project workflow used by contributors. Package mode is
for isolated acceptance against a local NuGet feed and emits no source-root property or project
reference.

### Development mode

CNA managed packages and RID-native packages are not published yet. Point the project at a
`cna-cs` checkout using either a property or an environment variable:

```bash
CNA_CS_ROOT=/path/to/cna-cs dotnet build
dotnet build -p:CnaCsRoot=/path/to/cna-cs
```

At runtime, put the CNA C ABI library next to the executable or configure it explicitly:

```bash
CNA_NATIVE_LIBRARY=/path/to/libcna_c_api.so dotnet run
# or
CNA_NATIVE_DIR=/path/to/cna-native-directory dotnet run
```

The template repository's sibling `../cna-cs` is discovered by a repository-only
`Directory.Build.props`. That file is excluded from generated projects: generated games use only
the explicit property/environment hook and emit a clear MSBuild error if no root is set.

### Package acceptance mode

The packages are not published. Given an acceptance feed produced by `cna-cs`, generate a consumer
which references only `CNA.XnaCompat` by package ID and version:

```bash
dotnet new cna-game --name MyPackagedGame \
  --consumerMode Package --cnaPackageVersion 0.1.0-local.1
dotnet restore MyPackagedGame --source /path/to/local/feed
dotnet build MyPackagedGame --no-restore
```

The RID-native asset, when deliberately included in `CNA.Interop` by the local acceptance harness,
is resolved beside the built application without `CNA_NATIVE_LIBRARY`, `CNA_NATIVE_DIR`, a sibling
checkout, or a system-library search.

## Deterministic runs

```bash
dotnet run -- --smoke-test       # 60 frames
dotnet run -- --stability-test   # 600 frames
dotnet run -- --frames 240       # exact custom count
CNA_SMOKE_FRAMES=120 dotnet run -- --smoke-test
```

A successful run creates graphics resources, updates, draws, disposes, and exits with code 0.
Runtime verification still requires a compatible native CNA library and display/headless renderer;
a managed build alone is not recorded as a runtime pass.

## Install as a `dotnet new` template

```bash
dotnet new install /path/to/cna-cs-template
dotnet new cna-game --name MyGame
CNA_CS_ROOT=/path/to/cna-cs dotnet build MyGame/MyGame.csproj
```

`scripts/verify-template.sh` performs an isolated install, generates a fresh project in a temporary
directory, and builds it. Its default `--mode development` preserves the source-reference check.
Use `--mode package --package-feed /path/to/feed --package-version 0.1.0-local.1` for the isolated
package check. Set `CNA_TEMPLATE_RUN_SMOKE=1` for 60 frames or
`CNA_TEMPLATE_RUN_STABILITY=1` for 600 frames; package mode needs no native environment override
when its `CNA.Interop` package contains the qualified RID asset.

## Portability harness

The raw logo is loaded through `Texture2D.FromStream`, so a missing XNB/content build step cannot be
mistaken for runtime compatibility. Conditional projects remain available for source-portability
checks:

```bash
dotnet build -p:Engine=MonoGame
dotnet build -p:Engine=Kni
FNA_FRAMEWORK_PATH=/path/to/FNA.dll dotnet build -p:Engine=FNA
```

The Kni configuration includes its SDL2.GL desktop backend; referencing only Kni's modular
framework packages compiles but leaves no concrete `GameFactory` for runtime startup.

FNA is intentionally not bundled. An absent FNA path produces an actionable error rather than a
silent reference to `libs/FNA.dll`. A configured but unloadable managed/native engine dependency
produces an actionable message and exit code 2. A successful alternate-engine build proves source
compilation; claim runtime support only after running that engine on the target platform.

## License

The template is licensed under the MIT License; see `LICENSE`.
