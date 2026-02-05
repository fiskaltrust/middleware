## Prepare Release (`prepare-relese.yml`)

- manually

## CI (`<queue|scu-at|scu-de|scu-es|scu-it>-build.yml`)

- on push
  - paths: `<queue|scu-at|scu-de|scu-es|scu-it>/**`
- manually
- from `/run <queue|scu-at|scu-de|scu-es|scu-it> ci` comment

## Package (`package.yml`)

- manually
- from `/package <queue|scu-at|scu-de|scu-es|scu-it> <PACKAGE>` comment

## Queue Acceptance Tests (`queue-acceptance-tests.yml`)

- on push
  - branch: `main`
  - tags-ignore: `queue/**/v*`
- manually
- from `/run queue acceptance-tests` comment

## Enforce All Status Checks
- on pull_request update
- from `/check` comment

## Version

## Release (`release.yml`)

- on push
  - tags: `<queue|scu-at|scu-de|scu-es|scu-it>/**/v*`

## Deploy (`deploy.yml`)

- manually (via Package)
- from `/deploy <queue|scu-at|scu-de|scu-es|scu-it> <PACKAGE>` comment
