## Queue CI (`queue-build.yml`)

- on push
  - paths: `queue/**`
- manually
- from `/run queue ci` comment

## Queue Package (`queue-package.yml`)

- manually
- from `/package queue <QUEUE>` comment

## Queue Acceptance Tests (`queue-acceptance-tests.yml`)

- on push
  - branch: `main`
  - tags-ignore: `queue/**/v*`
- manually
- from `/run queue acceptance-tests` comment

## Queue Release (`queue-release.yml`)

- on push
  - tags: `queue/**/v*`

## Deploy (`deploy.yml`)

- manually (via Queue Package)
- from `/deploy queue <QUEUE>` comment
