# Release process for the middleware

## Concepts

### Versioning

This repo contains the code for the fiskaltrust.Middleware versions >= 1.3.x.

New Middleware is released semi regularly as a new version (E.g. `v1.3.68`, `v1.3.69`, `v1.3.71`, ...). 
When a new release is done only the Middleware packages that have changes are released. 
All released packages will have the same new version. 
This can lead to gaps in the versions for some components that don't have changes in a certain release which is totally acceptable.

All packages of a certain Middleware version shall be compatible with one another. 
If a package is not released a Middleware new version the latest released version of said package shall be compatible with the Middleware new version.

All PRs shall have the relevant `queue-*` and `scu-*` labels set to have an overview of which packages and features will be released in a new Middleware version. 
Releases are tracked in github [Milestones](https://github.com/fiskaltrust/middleware/milestones).
All labeled PRs since the last release will be added to the release milestone.

#### Pre Release Versions

The SemVer v1 suffixes `-ci.X` and `-rc.X` are allowed (E.g. `-ci.1`, `-rc.2`, ...).

- `-ci.X` is used for internal sandbox releases. E.g. to test a specific branch or feature.
- `-rc.X` is for Release Candidates used for testing new Middleware versions.
  Before a full release an RC version is published to the sandbox and used for End2End testing this new Middleware version.
  An RC version can be released to give to a customer to test out fixes or features.

For untagged commits the commit hash is appended to the prerelease identifier of the version like this `<major>.<minor>.<patch>-ci.<commit-height>.<commit-hash>` (e.g. `1.3.68-ci.2.a1b2c3`).

> ***Note:** A note on SemVer v1 and v2. The release process only deals with SemVer v2 versions.*
> *That means `<version>-<prerelase>.<number>` with the `-` separating the version and the prerelease identifiers and the prerelease identifiers separated by `.`.*
> *In SemVer v1 this was not supported and would have been multiple `-` like `<version>-<prerelase>-<number>`.*
> *Since the nuget packages need SemVer v1 versions the package artifacts are versioned like that.*
> *So a release `1.3.68-rc.1` will be packaged as `1.3.68-rc-1`.*

### Tags

All versions released to production shall have a corresponding tag. 
Tags are prefixed with a path specifying the package that is being released (E.g. `queue/sqlite/v1.3.71`, `scu-de/swissbitcloudv2/v1.3.71`, `scu-it/epsonrtprinter/v1.3.71`, ...).

Tags should be created through the release process specified below but can also be created via github releases or created locally in the git repo and then pushed. 

### Production

In sandbox there's no restrictions on what can be released.  
In production only `-rcX` and full versions shall be released and both shall be End2End tested before doing so.

---

## Releasing a ci version to sandbox

When working on a PR it's possible to quickly create a sandbox release of the PR. 

Just comment `/deploy <compoment> <package>`.
So to release the SQLite queue that's `/deploy queue SQLite` or for the german SwissbitCloudV2 `/deploy scu-de SwissbitCloudV2`. 
This will build the middleware with a `ci` prerelease label.

Once ready the comment will be updated with an approval link where you can approve the sandbox deployment.

## Releasing the middleware 

This section describes how a new Middleware release is done.

### Fill Github Milestone

> Done by the Middleware Lead Engineer

First the relevant github Milestone is maintained. 
All relevant PRs since the last release are added to the milestone and they are given the needed `queue-*` and `scu-*` labels. 
PRs that have a customer facing impact get the `meta-needs-release-notes` and if needed the `meta-needs-migration-guide` label.
Relevant issues should be linked to the PRs.

### Call for Release Notes

A relese notes PR is created and a call for release notes comments goes out to the developers of PRs marked with the `meta-needs-release-notes` label.

### Create the Release PR

Manually run the [Prepare Release](https://github.com/fiskaltrust/middleware/actions/workflows/prepare-release.yml) action on the main branch.

This will create a `/release/vX` branch where the middleware version prerelease identifier is changed from `ci` to `rc`. 
Tis also updates the version in the main branch to the next middleware version with the `-ci` identifier.

### Deploy Pre Release Versions to Sandbox

The `/deploy <compoment> <package>` command now deploys `rc` versions from the Release PR so commenting `/deploy queue SQLite` deploys the sqlite queue with an `rc` version.
This is used to deploy all packages that need to be released to sandbox.

### Internal Release Notes Review

The release notes are given to the market lead engineers for review. 
A post in the releases channel is made.

### Feature Tests

All new features and bug fixes are tested in the sandbox.
The feature or bugfix is tested for functionality using a relevant configuration (E.g. a `SQLite` Queue and a `FiskalyCertified` SCU for a bugfix in the DE Localization or an `AzureTableStorage` Queue and a `SwissbitCloudV2` SCU for a new feature in the `SwissbitCloudV2`).

### Launcher End2End Tests

All Packages are End2End tested on all relevant launchers. Relevant Smoketests can be used for that.
The released packages are tested to start and sign on all launcher and OS combinations.
The specific features/bugfixes are already tested in the Feature Tests and don't need to be tested on all combinations again only the basic functionality of the packages is tested here.

### Fix Issues

Issues found during the testing phase these are fixed in the release branch and a new `-rc` version for these packages is released with the `/deploy` command and tested again.

### Deploy a Release Canditate to production

If a Release Candidate needs to be released to production this can be done with the `/tag <component> <package>` command.
It works similariy to the `/deploy` command but creates a new `rc` version tag so the release can also be released to production.

### Deploy Full Versions to Sandbox

Once everything is tested and ready for the full release version the `/version` command is used to remove the `rc` identifier.

By commenting `/version` in the release branch the version file is updated to the full (non prerelease) version.

After that the `/tag` command is used to create tags for the full versions.
Github releases should be created for these tags where the relevant PRs are listed for each package (This is partly done by githubs "Generate release notes" but the output needs to be filtered for relevance to the package).

After that the release PR is merged.
For that the merge conflict in the `version.json` file needs to be resolved in favour of the main branch. 
(E.g. The version in the release branch is `1.3.68` and the version in the main branch is `1.3.69-ci`. after the merge it should still be `1.3.69-ci`)

### Publish Release Notes

The release notes are published as soon as the version is available in production.
