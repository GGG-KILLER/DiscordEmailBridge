{ nixpkgs ? <nixpkgs>
, pkgs ? import nixpkgs { }
}:

let
  inherit (pkgs) buildDotnetModule dotnetCorePackages;
in
buildDotnetModule rec {
  pname = "DiscordEmailBridge";
  version = "0.1";

  src = ./.;

  projectFile = "src/DiscordEmailBridge.csproj";
  testProjectFile = "test/DiscordEmailBridge.Test.csproj";
  nugetDeps = ./deps.nix;

  dotnet-sdk = dotnetCorePackages.sdk_6_0;
  dotnet-runtime = dotnetCorePackages.runtime_6_0;

  executables = [ "DiscordEmailBridge" ];
}
