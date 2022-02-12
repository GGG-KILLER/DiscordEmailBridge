#!/usr/bin/env nix-shell
#!nix-shell -i bash -p nuget-to-nix dotnet-sdk_6
# Based on https://github.com/NixOS/nixpkgs/blob/nixos-unstable/pkgs/servers/jackett/updater.sh

set -eo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")"

deps_file="$(realpath "./deps.nix")"
src="$(mktemp -d /tmp/discordemailbridge-src.XXX)"
cp -rT ./ "$src"
chmod -R +w "$src"

pushd "$src"

export DOTNET_NOLOGO=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1

mkdir ./nuget_pkgs

for project in src/DiscordEmailBridge.csproj test/DiscordEmailBridge.Test.csproj; do
  dotnet restore "$project" --packages ./nuget_pkgs
done

nuget-to-nix ./nuget_pkgs > "$deps_file"

popd
rm -r "$src"