# Release process for the middleware

## Concepts

### Versioning

This repo contains the code for the fiskaltrust.Middleware versions >= 1.3.x.

New Middleware is relesed semi regularly as a new version (E.g. `v1.3.68`, `v1.3.69`, `v1.3.71`, ...). 
When a new release is done only the Middleware packages that have changes are released. 
All released packages will have the same new version. 
This can lead to gaps in the versions for some components that don't have changes in a certain release which is totally acceptable.

All packages of a certain Middleware version shall be compatible with one another. 
If a package is not released a Middleware new version the latest released version of said package shall be compatible with the Middleware new version.

Releases are tracked in github [Milestones](https://github.com/fiskaltrust/middleware/milestones). 
All PRs and issues in a Milestone have the relevant `queue-*` and `scu-*` labels set to have an overview of which packages shall be released in a new Middleware version.

#### Pre Release Versions

The SemVer v1 suffixes `-ciX` and `-rcX` are allowed (E.g. `-ci1`, `-rc2`, ...).

- `-ciX` is used for internal sandbox releases. E.g. to test a specific branch or feature.
- `-rcX` is for Release Candidates used for testing new Middleware versions.
  Before a full release an RC version is published to the sandbox and used for End2End testing this new Middleware version.
  An RC version can be released to give to a customer to test out fixes or features.

### Tags

All released versions shall have a corresponding tag. 
Tags are prefixed with a path specifying the package that is being released (E.g. `queue/sqlite/v1.3.71`, `scu-de/swissbitcloudv2/v1.3.71`, `scu-it/epsonrtprinter/v1.3.71`, ...).

Tags can be created via github releases or created locally in the git repo and then pushed (E.g. using the `git` cli or your favourite IDE). 

### Production

In sandbox there's no restrictions on what can be released.  
In production only `-rcX` and full versions shall be released and both shall be End2End tested before doing so.

---

## Regular Releases 

This section describes how a regular new Middleware (E.g. `v1.3.71`) release is done.

### Fill Github Milestone
> Done by the Middleware Lead Engineer

First the relevant github Milestone is maintained. 
All relevant issues and all PRs since the last release are added to the milestone and they are given the needed `queue-*` and `scu-*` labels. 
PRs or issues that have a customer facing impact get the `meta-needs-release-notes` and if needed the `meta-neets-migration-guide` label.

### Deploy Pre Release Versions to Sandbox

`-rcX` tags for the new version are created (The `Set as a pre-release` Checkbox is checked if created through github releases). 
This triggers the release pipeline where the Sandbox deployment is approved and deployed.

### Write Release Notes

A PR with the release notes for the new version is created in the [release-notes](https://github.com/fiskaltrust/release-notes) repo. 
All items in the release Milestone with the `meta-needs-release-notes` label have a related a section in the release notes and the relevant issue or PR is linked. 

### Internal Release Notes Review

The release notes are given to the market lead engineers for review.

### Feature Tests

All new features and bug fixes are tested in the sandbox.
The feature or bugfix is tested for functionality using a relevant configuration (E.g. a `SQLite` Queue and a `FiskalyCertified` SCU for a bugfix in the DE Localization or an `AzureTableStorage` Queue and a `SwissbitCloudV2` SCU for a new feature in the `SwissbitCloudV2`).

### Launcher End2End Tests

All Packages are End2End tested on all relevant launchers.
The released packages are tested to start and sign on all launcher and OS combinations.
The specific features/bugfixes are already tested in the Feature Tests and don't need to be tested on all combinations again only the basic functionality of the packages is tested here.

### Fix Issues

Issues found during the testing phase these are fixed and a new `-rcX` version for these packages is released and tested again.

### Deploy Full Versions to Sandbox

The full version tag is created and the sandbox is deployed.
Github releases are created for these versions where the relevant PRs are listed for each package (This is partly done by githubs "Generate release notes" but the output needs to be filtered for relevance to the package).

### Deploy Full Versions to Production

If no issues are reported production is also deployed.

### Publish Release Notes

The release notes are published as soon as the version is available in production.

## Hotfix Releases

If a hotfix needs to be released for a package the process is simplified.

### Deploy Pre Release Version to Sandbox

First an `-rcX` version tag is created and released to sandbox.

### Feature Tests

The `-rcX` version is tested in the relevant Queue, SCU, Launcher and OS configurations.

### Deploy Pre Release Version to Production

The tested package is then released as an RC version to production.
