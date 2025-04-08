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

## Release (`release.yml`)

- on push
  - tags: `<queue|scu-at|scu-de|scu-es|scu-it>/**/v*`

## Deploy (`deploy.yml`)

- manually (via Package)
- from `/deploy <queue|scu-at|scu-de|scu-es|scu-it> <PACKAGE>` comment
