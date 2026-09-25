# Contributing

## Workflow

`master` is the only long-lived branch and is always releasable.

1. Create a short-lived branch from `master` (`feature/<topic>` or `fix/<topic>`).
2. Open a pull request against `master`. CI must pass.
3. Pull requests are squash-merged, so the PR title becomes the commit message and must follow [Conventional Commits](https://www.conventionalcommits.org/), for example:
   - `feat(core): add async event handlers`
   - `fix(stores): flush pending ids on dispose`
   - `feat(di)!: replace the scoped factory` (a `!` marks a breaking change)

The commit types feed the generated release notes: `feat`, `fix`, `perf`, `refactor`, `doc` and `chore`.

## Building and testing

```bash
dotnet restore --locked-mode
dotnet build -c Release --no-restore
dotnet test -c Release --no-build --filter "Category!=IntegrationTests"
```

Integration tests use Testcontainers and need Docker. Package versions are pinned with lock files: after changing a dependency run `dotnet restore --force-evaluate` and commit the updated `packages.lock.json` files.

## Versioning and releases

Versions are calculated from git tags by [MinVer](https://github.com/adamralph/minver). Commits on `master` produce preview versions (`2.0.0-preview.0.<height>`), which are pushed to GitHub Packages.

To release, push a tag:

```bash
git tag v2.0.0-rc.1
git push origin v2.0.0-rc.1
```

The `release` workflow then runs the tests, builds the packages, waits for approval on the `nuget` environment, publishes to nuget.org and creates a GitHub release with notes generated from the commits. Stable tags also publish the documentation site.

Publishing to nuget.org uses NuGet trusted publishing: the `NUGET_USER` repository variable holds the nuget.org profile name, and a trusted publishing policy for this repository and the `release.yml` workflow has to exist on nuget.org.
