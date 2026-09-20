<#
Runs the backend build + test suite inside a Linux container (mcr.microsoft.com/dotnet/sdk:10.0),
mirroring .github/workflows/backend-ci.yml's ubuntu-latest job. Use this before pushing to catch
OS-dependent failures (e.g. Uri parsing, path/case-sensitivity differences) that pass on Windows
but fail in GitHub Actions, without waiting on a CI run.

Requires Docker Desktop running locally (Testcontainers.PostgreSql spins up a real Postgres
container for the integration tests, same as CI).
#>
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot

# TESTCONTAINERS_HOST_OVERRIDE: this container talks to the *host's* Docker daemon (via the
# mounted socket) to start the Postgres container, so it and Postgres end up as sibling
# containers, not on the same network. Testcontainers publishes Postgres' port on the host,
# which is only reachable from inside this container via Docker Desktop's host.docker.internal
# gateway, not "localhost" (verified: localhost fails, host.docker.internal works).
docker run --rm `
    -v "${repoRoot}:/repo" `
    -v "/var/run/docker.sock:/var/run/docker.sock" `
    -e "TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal" `
    -w "/repo/backend" `
    mcr.microsoft.com/dotnet/sdk:10.0 `
    bash /repo/scripts/ci-test-steps.sh

exit $LASTEXITCODE
