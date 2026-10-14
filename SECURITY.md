# Security policy

## Supported versions

Security fixes are provided for the latest published ZipNest release.

## Reporting a vulnerability

Please use GitHub's private **Report a vulnerability** form in the repository Security tab. Do not open a public issue for an unpatched archive-handling vulnerability.

Include the affected ZipNest version, Windows version, archive format, reproduction steps, and a minimal sample archive when it is safe to share one. Reports should receive an initial response within seven days.

## Archive threat model

ZipNest validates archive paths and declared sizes before extraction, extracts only into a new staging directory, blocks links and reparse points, verifies the resulting tree, and moves the original only after success. These controls reduce common archive traversal, link, overwrite, and partial-extraction risks; they do not make untrusted extracted executables safe to run.
