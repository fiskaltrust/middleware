# GitHub Workflows

This document provides an overview of all GitHub Actions workflows in this repository. For the complete release process documentation, see [RELEASE.md](../../RELEASE.md).

---

## Slash Commands

All slash commands are dispatched via [`slash-commands.yml`](slash-commands.yml) when commenting on a Pull Request.

### `/run` Command (`command-run.yml`)

Triggers CI builds or acceptance tests.

| Usage                                   | Description                    |
|-----------------------------------------|--------------------------------|
| `/run <component> ci`                   | Run CI build for a component   |
| `/run <component> build`                | Alias for `ci`                 |
| `/run queue acceptance-tests`           | Run all queue acceptance tests |
| `/run queue acceptance-tests <package>` | Run specific acceptance test   |

**Components:** `queue`, `scu-at`, `scu-de`, `scu-es`, `scu-it`

### `/package` Command (`command-package.yml`)

Builds a package without deploying.

```
/package <component> <package>
```

**Example:** `/package queue SQLite`

### `/deploy` Command (`command-package.yml`)

Builds a package and deploys to sandbox.

```
/deploy <component> <package>
```

**Example:** `/deploy scu-de SwissbitCloudV2`

**Behavior:**
1. Builds the package with version based on branch (`-ci` or `-rc`)
2. Updates comment with approval link (👀)
3. On approval, deploys to sandbox
4. Updates comment with success (🎉) or failure (😕)

### `/release` Command (`command-release.yml`)

Creates a version tag and GitHub release. Only works on release branches.

```
/release <component> <package>
```

**Example:** `/release queue SQLite`

**Behavior:**
1. Creates tag using `nbgv tag`
2. Pushes tag to repository
3. Creates GitHub release with appropriate release notes
4. Updates comment with link to release

### `/version` Command (`command-version.yml`)

Removes the `-rc` prerelease suffix from the version. Only works on release branches.

```
/version
```

**Behavior:** Updates `version.json` to full release version (e.g., `1.3.68-rc` → `1.3.68`)

### `/merge` Command (`command-merge.yml`)

Merges a release PR with special conflict handling. Only works on release branches.

```
/merge
```

**Behavior:**
1. Resolves `version.json` conflicts in favor of `main` branch
2. Verifies all status checks pass
3. Merges the PR

---

## Build Workflows

CI workflows that run on push to component directories and can be triggered manually or via `/run`.

| Workflow           | Trigger Paths | Manual Trigger   |
|--------------------|---------------|------------------|
| `queue-build.yml`  | `queue/**`    | `/run queue ci`  |
| `scu-at-build.yml` | `scu-at/**`   | `/run scu-at ci` |
| `scu-de-build.yml` | `scu-de/**`   | `/run scu-de ci` |
| `scu-es-build.yml` | `scu-es/**`   | `/run scu-es ci` |
| `scu-it-build.yml` | `scu-it/**`   | `/run scu-it ci` |
| `scu-be-build.yml` | `scu-be/**`   | `/run scu-be ci` |

### Queue Acceptance Tests (`queue-acceptance-tests.yml`)

**Triggers:**
- Push to `main` branch
- Tag ignore: `queue/**/v*`
- Manual: `/run queue acceptance-tests`

**Options:**
- Run all tests: `/run queue acceptance-tests`
- Run specific test: `/run queue acceptance-tests SQLite`

---

## Package Workflows

### Package (`package.yml`)

Reusable workflow for building and packaging components.

**Inputs:**
- `pattern`: Project pattern to match
- `directory`: Source directory
- `commit`: Optional commit ref
- `deploySandbox`: Deploy to sandbox after packaging

**Called by:** `command-package.yml`, `release.yml`, individual package workflows

### Component Package Workflows

Pre-configured package workflows for each component:

- `queue-package.yml`
- `scu-at-package.yml`
- `scu-de-package.yml`
- `scu-es-package.yml`
- `scu-it-package.yml`
- `scu-be-package.yml`

---

## Release Workflows

### Prepare Release (`prepare-release.yml`)

**Trigger:** Manual (workflow dispatch)

**Purpose:** Creates a release branch with RC version.

**Actions:**
1. Creates `release/vX.Y` branch from `main`
2. Sets release branch version to `-rc`
3. Bumps `main` to next version with `-ci`
4. Creates PR from release branch to `main`

### Release (`release.yml`)

**Trigger:** GitHub release published with tag matching `<component>/<package>/v*`

**Purpose:** Deploys a tagged release to production.

**Actions:**
1. Validates tag format
2. Runs component-specific tests
3. Packages the component
4. Deploys to sandbox (for verification)
5. Deploys to production

### Deploy (`deploy.yml`)

Reusable workflow for deploying packages to an environment.

**Inputs:**
- `package`: Package name to deploy
- `environment`: Target environment (`sandbox` or `production`)

**Deployments:**
- v1 Packages (Azure Storage)
- v2 Packages (Azure Storage)

---

## Utility Workflows

### Enforce All Checks (`enforce-all-checks.yml`)

**Triggers:**
- Pull request updates

**Purpose:** Waits for all status checks to complete.

### Check Labels (`check-labels.yml`)

Validates that PRs have required labels.

### Check Linked Issue (`check-linked-issue.yml`)

Validates that PRs are linked to issues.

### CLA (`cla.yml`)

Contributor License Agreement checking.

### Manual Merging (`manual-merging.yml`)

Makes sure that release PRs can not be merged manually.

### Remove No Issue Label (`remove-no-issue-label.yml`)

Automatically removes `no-issue` label when an issue is linked.

### Smoketests (`smoketests.yml`)

Runs smoke tests for release verification.

## Workflow Dependencies

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                    SLASH COMMANDS                                       │
│                                                                                         │
│    Triggered by: issue_comment on pull requests                                         │
│    Dispatched via: slash-commands.yml                                                   │
│                                                                                         │
│      /run      ──► command-run.yml ──► queue-build.yml                                  │
│                                        queue-acceptance-tests.yml                       │
│                                        scu-at-build.yml                                 │
│                                        scu-de-build.yml                                 │
│                                        scu-es-build.yml                                 │
│                                        scu-it-build.yml                                 │
│                                                                                         │
│      /package  ──► command-package.yml ──► package.yml                                  │
│      /deploy   ──► command-package.yml ──► package.yml ──► deploy.yml                   │
│                                                                                         │
│      /release  ──► command-release.yml                                                  │
│      /version  ──► command-version.yml                                                  │
│      /merge    ──► command-merge.yml                                                    │
│                                                                                         │
└─────────────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                    BUILD WORKFLOWS                                      │
│                                                                                         │
│    Triggered by: pull_request, workflow_dispatch, workflow_call                         │
│    Called by: command-run.yml, release.yml                                              │
│                                                                                         │
│      queue-build.yml              (paths: queue/**)                                     │
│      queue-acceptance-tests.yml   (paths: queue/**, branches: main, release/v*)         │
│      scu-at-build.yml             (paths: scu-at/**)                                    │
│      scu-de-build.yml             (paths: scu-de/**)                                    │
│      scu-es-build.yml             (paths: scu-es/**)                                    │
│      scu-it-build.yml             (paths: scu-it/**)                                    │
│      scu-be-build.yml             (paths: scu-be/**)                                    │
│                                                                                         │
└─────────────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                   PACKAGE WORKFLOWS                                     │
│                                                                                         │
│    Triggered by: workflow_dispatch, workflow_call                                       │
│    Called by: command-package.yml, release.yml                                          │
│                                                                                         │
│      queue-package.yml ───────┐                                                         │
│      scu-at-package.yml ──────┤                                                         │
│      scu-de-package.yml ──────┼──► package.yml ──► deploy.yml (optional)                │
│      scu-es-package.yml ──────┤                                                         │
│      scu-it-package.yml ──────┤                                                         │
│      scu-be-package.yml ──────┘                                                         │
│                                                                                         │
└─────────────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                   RELEASE WORKFLOW                                      │
│                                                                                         │
│    Triggered by: GitHub release published (tag: <component>/<package>/v*)               │
│                                                                                         │
│      release.yml                                                                        │
│           │                                                                             │
│           ├──► queue-build.yml ────────┐                                                │
│           ├──► scu-at-build.yml ───────┤                                                │
│           ├──► scu-de-build.yml ───────┼──► queue-acceptance-tests.yml                  │
│           ├──► scu-es-build.yml ───────┤              │                                 │
│           └──► scu-it-build.yml ───────┘              │                                 │
│                                                       ▼                                 │
│                                                 package.yml                             │
│                                                       │                                 │
│                                                       ▼                                 │
│                                                 deploy.yml (sandbox)                    │
│                                                       │                                 │
│                                                       ▼                                 │
│                                                 deploy.yml (production)                 │
│                                                                                         │
└─────────────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                  STANDALONE WORKFLOWS                                   │
│                                                                                         │
│    Triggered by: workflow_dispatch, repository_dispatch                                 │
│                                                                                         │
│      prepare-release.yml   - Creates release branches                                   │
│      command-release.yml   - Creates tags and GitHub releases                           │
│      command-version.yml   - Updates version.json                                       │
│      command-merge.yml     - Merges release PRs                                         │
│                                                                                         │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

> | Trigger types | description |
> |---|---|
> | [workflow_call](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows#workflow_call) | Called by another workflow |
> | [workflow_dispatch](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows#workflow_dispatch) | Manual trigger through the github ui |
> | [repository_dispatch](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows#repository_dispatch) | Triggered by a slash command |


### Composite Actions

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                   COMPOSITE ACTIONS                                     │
│                                                                                         │
│    Located in: .github/actions/                                                         │
│                                                                                         │
│      build                  - Restores and builds .NET projects                         │
│      test                   - Runs tests and uploads results                            │
│      sign                   - Signs DLLs with Azure Key Vault                           │
│      package                - Creates v1 (NuGet) and v2 (zip) packages                  │
│      deploy-v1-packages     - Deploys NuGet packages to Azure Storage                   │
│      deploy-v2-packages     - Deploys zip packages to Azure Storage                     │
│      check-linked-issue     - Validates PR has linked issue                             │
│                                                                                         │
└─────────────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                              WORKFLOW → ACTION USAGE                                    │
│                                                                                         │
│    Build Workflows:                                                                     │
│                                                                                         │
│      queue-build.yml ─────────────► build, test                                         │
│      queue-acceptance-tests.yml ──► test                                                │
│      scu-at-build.yml ────────────► build, test                                         │
│      scu-de-build.yml ────────────► build, test                                         │
│      scu-es-build.yml ────────────► build, test                                         │
│      scu-it-build.yml ────────────► build, test                                         │
│      scu-be-build.yml ────────────► build, test                                         │
│                                                                                         │
│    Package/Deploy Workflows:                                                            │
│                                                                                         │
│      package.yml ─────────────────► build, sign, package                                │
│      deploy.yml ──────────────────► deploy-v1-packages, deploy-v2-packages              │
│                                                                                         │
│    Utility Workflows:                                                                   │
│                                                                                         │
│      check-linked-issue.yml ──────► check-linked-issue                                  │
│                                                                                         │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

### Workflow Call Summary

| Caller Workflow       | Calls                                                                                                                                                        |
|-----------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `slash-commands.yml`  | Dispatches: `command-run`, `command-package`, `command-release`, `command-version`, `command-merge`                                                          |
| `command-run.yml`     | `queue-build.yml`, `queue-acceptance-tests.yml`, `scu-at-build.yml`, `scu-de-build.yml`, `scu-es-build.yml`, `scu-it-build.yml`                              |
| `command-package.yml` | `package.yml`, `deploy.yml`                                                                                                                                  |
| `command-release.yml` | _(standalone - creates tags/releases)_                                                                                                                       |
| `command-version.yml` | _(standalone - updates version.json)_                                                                                                                        |
| `command-merge.yml`   | _(standalone - merges PRs)_                                                                                                                                  |
| `queue-package.yml`   | `package.yml`                                                                                                                                                |
| `scu-at-package.yml`  | `package.yml`                                                                                                                                                |
| `scu-de-package.yml`  | `package.yml`                                                                                                                                                |
| `scu-es-package.yml`  | `package.yml`                                                                                                                                                |
| `scu-it-package.yml`  | `package.yml`                                                                                                                                                |
| `scu-be-package.yml`  | `package.yml`                                                                                                                                                |
| `package.yml`         | `deploy.yml` (when `deploySandbox: true`)                                                                                                                    |
| `release.yml`         | `queue-build.yml`, `scu-at-build.yml`, `scu-de-build.yml`, `scu-es-build.yml`, `scu-it-build.yml`, `queue-acceptance-tests.yml`, `package.yml`, `deploy.yml` |
| `prepare-release.yml` | _(standalone - creates release branches)_                                                                                                                    |
