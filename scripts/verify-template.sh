#!/usr/bin/env bash
set -euo pipefail

template_root=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
dotnet_command=${DOTNET_COMMAND:-dotnet}
mode=development
package_feed=${CNA_PACKAGE_FEED:-}
package_version=${CNA_PACKAGE_VERSION:-0.1.0-local.1}

while (($# > 0)); do
  case "$1" in
    --mode)
      mode=${2:?--mode requires development or package}
      shift 2
      ;;
    --package-feed)
      package_feed=${2:?--package-feed requires a path}
      shift 2
      ;;
    --package-version)
      package_version=${2:?--package-version requires a value}
      shift 2
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 2
      ;;
  esac
done

if [[ "$mode" != development && "$mode" != package ]]; then
  echo "--mode must be 'development' or 'package'." >&2
  exit 2
fi

cna_root=${CNA_DOTNET_ROOT:-$(cd "$template_root/../cna-dotnet" 2>/dev/null && pwd || true)}
if [[ "$mode" == development && ( -z "$cna_root" || ! -f "$cna_root/src/CNA.XnaCompat/CNA.XnaCompat.csproj" ) ]]; then
  echo "Development mode requires CNA_DOTNET_ROOT to identify a cna-dotnet checkout." >&2
  exit 2
fi
if [[ "$mode" == package && ( -z "$package_feed" || ! -d "$package_feed" ) ]]; then
  echo "Package mode requires --package-feed or CNA_PACKAGE_FEED." >&2
  exit 2
fi

# The checked-in project, before `dotnet new` touches it. This repository is a working game as
# well as a template, its README says so, and nothing verified it: the template's conditional
# comments are processed only at generation time, so in the repository both consumer-mode blocks are
# live at once. That made restore fail with "'CnaPackageVersion' is not a valid version string" --
# a defect that survived because every check here started by generating a project.
repository_build_status=skipped
if [[ "$mode" == development ]]; then
  "$dotnet_command" build "$template_root/CnaDotnetTemplate.csproj" -c Release \
    -p:CnaDotnetRoot="$cna_root" -m:1
  repository_build_status=passed
fi

# Never /tmp, and never inside this template: `dotnet new` would copy a nested consumer into the
# next generated game, and MSBuild would apply this repository's Directory.Build.props to it. The
# generated consumer is a fixture of the binding, so it lives in cna-dotnet's shared build-consumer/
# (or CNA_CONSUMER_ROOT) and is replaced on the next run of the same mode.
consumer_root=${CNA_CONSUMER_ROOT:-${cna_root:+$cna_root/build-consumer}}
if [[ -z "$consumer_root" ]]; then
  consumer_root=$(cd "$template_root/../cna-dotnet" 2>/dev/null && pwd)/build-consumer
fi
verification_root="$consumer_root/template-$mode"
rm -rf "$verification_root"
mkdir -p "$verification_root"

export DOTNET_CLI_HOME="$verification_root/dotnet-home"
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1

"$dotnet_command" new install "$template_root"
if [[ "$mode" == development ]]; then
  "$dotnet_command" new cna-game --name GeneratedCnaGame --output "$verification_root/GeneratedCnaGame" \
    --consumerMode Development
else
  "$dotnet_command" new cna-game --name GeneratedCnaGame --output "$verification_root/GeneratedCnaGame" \
    --consumerMode Package --cnaPackageVersion "$package_version"
fi

generated_root="$verification_root/GeneratedCnaGame"
generated_project="$generated_root/GeneratedCnaGame.csproj"
if [[ -e "$generated_root/Directory.Build.props" || -d "$generated_root/scripts" ]]; then
  echo "Generated output contains repository-only template infrastructure." >&2
  exit 1
fi
# grep rather than rg, which a macOS host does not ship (CNA plans/plan_apple_m4.md AM4-226).
if grep -n -F '..\cna-dotnet' "$generated_project"; then
  echo "Generated project contains a repository-specific sibling path." >&2
  exit 1
fi

if [[ "$mode" == development ]]; then
  if ! grep -q '<ProjectReference ' "$generated_project"; then
    echo "Development-mode output is missing its CNA project reference." >&2
    exit 1
  fi
  "$dotnet_command" build "$generated_project" -p:CnaDotnetRoot="$cna_root" -m:1
else
  if grep -n -E 'CnaDotnetRoot|CNA_DOTNET_ROOT|ProjectReference' "$generated_project"; then
    echo "Package-mode output contains a source/project-reference path." >&2
    exit 1
  fi
  if ! grep -q -F "<PackageReference Include=\"CNA.XnaCompat\" Version=\"$package_version\"" "$generated_project"; then
    echo "Package-mode output does not reference the requested CNA.XnaCompat version." >&2
    exit 1
  fi

  config_file="$verification_root/nuget.config"
  "$dotnet_command" new nugetconfig --output "$verification_root"
  "$dotnet_command" nuget remove source nuget --configfile "$config_file"
  "$dotnet_command" nuget add source "$(cd "$package_feed" && pwd)" --name cna-local --configfile "$config_file"
  "$dotnet_command" restore "$generated_project" --configfile "$config_file" \
    --packages "$verification_root/packages"
  "$dotnet_command" build "$generated_project" --no-restore -m:1

  if grep -r -n -F --include='*.csproj' --include='project.assets.json' "$cna_root" "$generated_root"; then
    echo "Package-mode generated consumer retains a path to the CNA.NET source checkout." >&2
    exit 1
  fi
fi

# Runs happen on CNA's private display (headless Weston + rootful Xwayland on the real GPU), never
# on the developer's desktop. Development mode needs an explicit native library; package mode must
# find the packaged one by itself, so the overrides are removed there.
#
# macOS has no Weston (CNA plans/plan_apple_m4.md AM4-226). There the run stays off the desktop by
# SDL's dummy video driver, which a windowless CNA renderer (SOFTWARE, SDL_RENDERER) draws under; a
# renderer that needs a real window refuses there by name rather than opening one on the desktop.
cna_native_root=${CNA_ROOT:-"$template_root/../cna"}
private_runner="$cna_native_root/tools/platform/run_gpu_tests_private.sh"
if [[ "$(uname -s)" == Darwin ]]; then
  private_runner_command=(env SDL_VIDEODRIVER=dummy)
else
  private_runner_command=("$private_runner" --exec)
fi
run_generated()
{
  if [[ "$(uname -s)" != Darwin && ! -x "$private_runner" ]]; then
    echo "Running the generated game needs $private_runner (set CNA_ROOT)." >&2
    exit 2
  fi
  if [[ "$mode" == package ]]; then
    env -u CNA_NATIVE_LIBRARY -u CNA_NATIVE_DIR SDL_AUDIODRIVER=dummy \
      "${private_runner_command[@]}" "$dotnet_command" run --project "$generated_project" --no-build -- "$@"
  else
    if [[ -z "${CNA_NATIVE_LIBRARY:-}${CNA_NATIVE_DIR:-}" ]]; then
      echo "Development-mode runs need CNA_NATIVE_LIBRARY or CNA_NATIVE_DIR." >&2
      exit 2
    fi
    SDL_AUDIODRIVER=dummy \
      "${private_runner_command[@]}" "$dotnet_command" run --project "$generated_project" --no-build -- "$@"
  fi
}

if [[ "${CNA_TEMPLATE_RUN_SMOKE:-0}" == 1 ]]; then
  run_generated --smoke-test
fi
if [[ "${CNA_TEMPLATE_RUN_STABILITY:-0}" == 1 ]]; then
  run_generated --stability-test
fi

echo "TEMPLATE_MODE=$mode"
echo "TEMPLATE_REPOSITORY_BUILD=$repository_build_status"
echo "TEMPLATE_GENERATED_PROJECT=GeneratedCnaGame/GeneratedCnaGame.csproj"
echo "TEMPLATE_BUILD=passed"
echo "TEMPLATE_SOURCE_PATHS=absent"
