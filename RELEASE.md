# Release Process for the Middleware

This document describes the release process for the fiskaltrust.Middleware. For workflow-specific documentation, see the [Workflows README](.github/workflows/README.md).

---

## Concepts

### Versioning

This repo contains the code for the fiskaltrust.Middleware versions >= 1.3.x.

New Middleware is released semi-regularly as a new version (e.g., `v1.3.68`, `v1.3.69`, `v1.3.71`, ...). When a new release is done, only the Middleware packages that have changes are released. All released packages will have the same new version. This can lead to gaps in the versions for some components that don't have changes in a certain release, which is totally acceptable.

**Compatibility Rules:**
- All packages of a certain Middleware version shall be compatible with one another
- If a package is not released in a new Middleware version, the latest released version of said package shall be compatible with the new version

**Labeling:**
- All PRs shall have the relevant `queue-*` and `scu-*` labels set to provide an overview of which packages and features will be released
- Releases are tracked in GitHub [Milestones](https://github.com/fiskaltrust/middleware/milestones)
- All labeled PRs since the last release will be added to the release milestone

### Pre-Release Versions

The SemVer v2 suffixes `-ci.X` and `-rc.X` are used:

| Suffix  | Purpose                                                             | Example       | Deployed To                     |
|---------|---------------------------------------------------------------------|---------------|---------------------------------|
| `-ci.X` | Internal sandbox releases for testing specific branches or features | `1.3.68-ci.2` | Sandbox only                    |
| `-rc.X` | Release Candidates for End2End testing before a full release        | `1.3.68-rc.1` | Sandbox, Production (if needed) |

For untagged commits, the commit hash is appended to the prerelease identifier:
```
<major>.<minor>.<patch>-ci.<commit-height>.<commit-hash>
```
Example: `1.3.68-ci.2.a1b2c3`

> **Note on SemVer v1 vs v2:**  
> The release process uses SemVer v2 versions (`<version>-<prerelease>.<number>` with `.` separating prerelease identifiers).  
> Since NuGet packages require SemVer v1, package artifacts use `-` instead of `.` for prerelease identifiers.  
> Example: A release `1.3.68-rc.1` is packaged as `1.3.68-rc-1`.

### Tags

All versions released to production shall have a corresponding tag. Tags are prefixed with a path specifying the package being released:

> Example: 
> | Component | Tag Format                    | Example                          |
> |-----------|-------------------------------|----------------------------------|
> | Queue     | `queue/<package>/v<version>`  | `queue/sqlite/v1.3.71`           |
> | SCU-DE    | `scu-de/<package>/v<version>` | `scu-de/swissbitcloudv2/v1.3.71` |
> | SCU-IT    | `scu-it/<package>/v<version>` | `scu-it/epsonrtprinter/v1.3.71`  |
> | SCU-AT    | `scu-at/<package>/v<version>` | `scu-at/atrust/v1.3.71`          |
> | SCU-ES    | `scu-es/<package>/v<version>` | `scu-es/verifactu/v1.3.71`       |

Tags are typically created through the release process using the `/release` command but can also be created via GitHub releases or locally.

### Environments

| Environment    | Allowed Versions             | Requirements           |
|----------------|------------------------------|------------------------|
| **Sandbox**    | Any (`-ci`, `-rc`, full)     | No restrictions        |
| **Production** | `-rc` and full versions only | Must be End2End tested |

---

## Slash Commands Reference

All slash commands are triggered by commenting on a Pull Request. The following commands are available:

| Command                          | Description                                        | Example                                        |
|----------------------------------|----------------------------------------------------|------------------------------------------------|
| `/run <component> <action>`      | Run CI builds or tests                             | `/run queue ci`, `/run queue acceptance-tests` |
| `/package <component> <package>` | Build a package without deploying                  | `/package queue SQLite`                        |
| `/deploy <component> <package>`  | Build and deploy to sandbox                        | `/deploy queue SQLite`                         |
| `/release <component> <package>` | Create a tag and GitHub release                    | `/release scu-de SwissbitCloudV2`              |
| `/version`                       | Remove `-rc` suffix (release branches only)        | `/version`                                     |
| `/merge`                         | Merge release PR (resolves version.json conflicts) | `/merge`                                       |

**Component values:** `queue`, `scu-at`, `scu-de`, `scu-es`, `scu-it`, `scu-be`, `scu-gr`, `scu-me`, `scu-pt`

---

## Development Workflow

### Testing Changes in Sandbox

When working on a PR, you can quickly create a sandbox release to test your changes:

1. **Comment on your PR:** `/deploy <component> <package>`
   - Example: `/deploy queue SQLite` or `/deploy scu-de SwissbitCloudV2`
2. **Wait for build:** The comment will be updated with an approval link
3. **Approve deployment:** Click the approval link to deploy to sandbox

The deployed version will have a `-ci` prerelease label based on your branch.

### Running Tests Manually

- **Run CI build:** `/run <component> ci` (e.g., `/run queue ci`)
- **Run acceptance tests:** `/run queue acceptance-tests`
- **Run specific acceptance test:** `/run queue acceptance-tests <package>` (e.g. `/run queue acceptance-tests SQLite`)

---

## Release Workflow

This section describes how a new Middleware release is performed.

### Phase 1: Preparation

#### 1.1 Fill GitHub Milestone

> *Performed by: Middleware Lead Engineer*

1. Maintain the relevant GitHub Milestone
2. Add all relevant PRs since the last release to the milestone
3. Apply `queue-*` and `scu-*` labels to categorize packages
4. Add `meta-needs-release-notes` label to customer-facing changes
5. Add `meta-needs-migration-guide` label if migration steps are required
6. Link relevant issues to the PRs

#### 1.2 Call for Release Notes

1. Create a release notes PR
2. Notify developers of PRs marked with `meta-needs-release-notes`

#### 1.3 Create the Release PR

1. Manually run the [Prepare Release](https://github.com/fiskaltrust/middleware/actions/workflows/prepare-release.yml) action on the `main` branch

This action will:
- Create a `release/vX.Y` branch with `-rc` prerelease identifier
- Update `main` branch to the next version with `-ci` identifier
- Create a Pull Request from the release branch to `main`

### Phase 2: Testing

#### 2.1 Deploy RC Versions to Sandbox

From the release PR, use `/deploy` commands to deploy packages with `-rc` versions:

```
/deploy queue SQLite
/deploy queue EFCore
/deploy scu-de SwissbitCloudV2
```

#### 2.2 Internal Release Notes Review

1. Share release notes with market lead engineers
2. Post in the releases slack channel for visibility

#### 2.3 Feature Testing

Test all new features and bug fixes in sandbox with relevant configurations:
- Example: For a DE Localization bugfix, use a `SQLite` Queue + `FiskalyCertified` SCU
- Example: For a `SwissbitCloudV2` feature, use a CloudCashbox + `SwissbitCloudV2` SCU

#### 2.4 Launcher End2End Tests

1. Run smoketests on all relevant launcher/OS combinations
2. Verify packages start and sign correctly
3. Basic functionality testing only (specific features already tested in 2.3)

#### 2.5 Fix Issues

If issues are found during testing:
1. Fix issues in the release branch
2. Deploy new `-rc` version with `/deploy`
3. Re-test the fix

### Phase 3: Production Release

#### 3.1 Deploy Release Candidate to Production (Optional)

If an RC needs to be released to production for customer testing:

```
/release <component> <package>
```

This creates a tag and GitHub release for the `-rc` version.

#### 3.2 Finalize Version

Once testing is complete, remove the `-rc` suffix:

```
/version
```

This updates `version.json` in the release branch to the full version.

#### 3.3 Create Release Tags

For each package to be released:

```
/release <component> <package>
```

This will:
- Create a version tag (e.g., `queue/sqlite/v1.3.71`)
- Create a GitHub release with release notes
- Trigger the production deployment workflow

#### 3.4 Merge Release PR

After all packages are tagged:

```
/merge
```

This merges the release branch while preserving the newer version in `main`.

> **Note:** The `/merge` command resolves the `version.json` file in favor of `main`.  
> Example: Release branch has `1.3.68`, main has `1.3.69-ci` → after merge: `1.3.69-ci`

#### 3.5 Publish Release Notes

Publish the release notes to [docs.fiskaltrust.cloud](https://docs.fiskaltrust.cloud/changelog/middleware/) once packages are available in production.
