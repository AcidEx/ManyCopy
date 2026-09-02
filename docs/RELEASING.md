# Releasing ManyCopy

The release workflow is the only process that should create or update a
GitHub release. Do not create the release manually after pushing a tag.

## Signing configuration

Real tag releases fail before upload unless these repository secrets exist:

- `CODESIGN_PFX_BASE64`: base64-encoded PFX containing the code-signing
  certificate and private key.
- `CODESIGN_PFX_PASSWORD`: password protecting the PFX.
- `CODESIGN_SUBJECT` (optional): certificate subject, normally
  `CN=ManyCopy`.

Never commit the PFX, its password, or the private key. The public `.cer`
file is not required by the workflow.

## Validate before tagging

Run the **Release** workflow manually from GitHub Actions. You may enter the
expected project version so the workflow can catch a mismatch.

The manual run builds, tests, publishes, signs when secrets are available,
and uploads a workflow artifact. It never creates a public GitHub release.
If signing secrets are unavailable, the artifact name ends in
`-UNSIGNED.zip`.

## Publish a release

1. Update the version in `ManyCopy.csproj`, the changelog, and the README.
2. Merge the release change into `main`.
3. Run the manual dry run and inspect its artifact.
4. Create an annotated `v<version>` tag on the tested commit in `main`.
5. Push the tag.

The tag workflow verifies that the tag matches the project version and points
to a commit contained in `main`. It then builds, runs the tests, publishes,
requires a valid SHA-256 Authenticode signature, and uploads one canonical
`ManyCopy-<version>-win-x64.zip` package.

The package contains:

- `ManyCopy.exe`
- `ManyCopy.exe.sha256`

Concurrent runs for the same ref are serialized, and a rerun replaces the
canonical package instead of adding duplicate release assets.
