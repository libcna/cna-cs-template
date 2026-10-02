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

## In a browser and on Android

`Platforms/Browser` and `Platforms/Android` build the same game -- the same `.cs` files and `Content/`
-- for CNA.NET's WebAssembly and Android hosts. Both are development consumers (`CNA_CS_ROOT` or
`-p:CnaCsRoot=...`) and need the .NET 11 SDK with its `wasm-tools` or `android` workload, plus the
native CNA build that cna-cs stages for that platform:

```bash
# browser: cna-cs scripts/Build-BrowserNative.sh first; CNA is linked into dotnet.native.wasm
dotnet publish Platforms/Browser -c Release
#   serve bin/Release/net11.0/publish/wwwroot over HTTP and open it

# Android: cna-cs scripts/Build-AndroidNative.sh first (x86_64 by default; --abi arm64-v8a)
dotnet build Platforms/Android -c Release -t:Install    # onto the running emulator or device
```

On Android, `Content/` is packaged as assets and extracted beside the game before `Main` runs; in a
browser it is in the page's file system. A browser game runs on the page's single thread, so a game
that starts threads of its own is not for this head.

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

`scripts/verify-template.sh` performs an isolated install, generates a fresh project and builds it.
The generated consumer is written to cna-cs's shared `build-consumer/template-<mode>` (or
`CNA_CONSUMER_ROOT`) -- never `/tmp`, and never inside this template, where `dotnet new` would copy it
into the next generated game and this repository's `Directory.Build.props` would apply to it. Its
default `--mode development` preserves the source-reference check. Use
`--mode package --package-feed /path/to/feed --package-version 0.1.0-local.1` for the isolated
package check. Set `CNA_TEMPLATE_RUN_SMOKE=1` for 60 frames or `CNA_TEMPLATE_RUN_STABILITY=1` for
600 frames. Runs go through CNA's private display runner (`../cna/tools/platform/run_gpu_tests_private.sh`,
or `CNA_ROOT`), never the desktop; development mode needs `CNA_NATIVE_LIBRARY`, package mode must
load the packaged native asset with no override.

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

**`FNA_FRAMEWORK_PATH` must name a .NET-targeting FNA build, not a .NET Framework one.** FNA's
repository ships four project files, and only `FNA.Core.csproj` produces an assembly a `net8.0` host
can load. Pointing at the output of `FNA.csproj` builds cleanly and then fails at startup with
`Game framework dependency could not be loaded: FNA` -- the build references it happily and the
runtime cannot load it, which is a confusing pair of outcomes and is worth stating rather than
rediscovering:

```bash
dotnet build /path/to/FNA/FNA.Core.csproj -c Release
FNA_FRAMEWORK_PATH=/path/to/FNA/bin/Release/net8.0/FNA.dll \
  LD_LIBRARY_PATH=/path/to/fnalibs \
  dotnet run -p:Engine=FNA -- --frames 600
```

`LD_LIBRARY_PATH` is in that command deliberately. FNA needs its own native `libFNA3D`, SDL and
FAudio, which are FNA's dependencies rather than this template's -- and without them the run fails
with a list of `cannot open shared object file` lines naming paths that do not exist, one of which
is a doubled `liblibFNA3D.so.0`. That is the .NET loader trying every naming convention it knows,
not a broken build, and it reads like one. Put the libraries beside the executable or on the loader
path.

Measured on Linux x64, all from the same game source: **600 frames on FNA (Vulkan, AMD Radeon
780M)**, and 600 frames on CNA over each of four renderers.

| CNA renderer | 60 frames | 600 frames | what it exercises here |
| --- | --- | --- | --- |
| OPENGLES3 | pass | pass | the full path, 3D cube included |
| SOFTWARE | pass | pass | a 3D pipeline with no volume textures and no compiled effects |
| SDL_RENDERER | pass | pass | **the 2D fallback**, and the only renderer that takes it |
| HEADLESS | pass | pass | the loop and the call sequence, rasterising nothing |

`SDL_RENDERER` is the one worth noting. It reports no `ThreeD` capability, so it is the first
renderer on which the template's "CNA renderers without a 3D pipeline receive a bouncing 2D
fallback" sentence is a measurement rather than a promise -- the guarded 3D path is skipped and the
fallback draws for 600 frames. The template needed no change to run there, which is the point of
asking the renderer for its capabilities instead of naming renderers.

The renderer line prints what the engine calls itself, so on FNA it reports the display adapter's
description (`Dell Inc. 27"`) rather than a backend name. The template asks the runtime and prints
the answer; the difference is FNA's, and the template deliberately does not translate it.

## License

The template is licensed under the MIT License; see `LICENSE`.
