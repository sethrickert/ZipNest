<p align="center">
  <img src="assets/ZipNest.png" width="128" alt="ZipNest icon">
</p>

# ZipNest

**Archives in. Tidy folders out.**

ZipNest is a deliberately small Windows archive extractor for people who want one predictable result: a ZIP, 7z, or RAR archive becomes a normal folder in the same place, and the original archive moves safely inside that folder.

No file manager. No destination picker. No success dialog. Explorer stays exactly where it is.

## The two commands

| Command | Result |
| --- | --- |
| **Extract & Nest** | Always extracts into a new same-name folder, then moves the original archive inside. |
| **Extract & Organize** | Does the same, but removes one redundant same-name wrapper folder when an archive already contains one. |

Example: `Photos.zip` beside your files becomes `Photos\`, containing the extracted files and `Photos.zip`. If `Photos\` already exists, ZipNest creates `Photos (2)\`; it never merges into or overwrites an existing folder.

Double-click uses the mode selected in ZipNest settings. The default is **Extract & Nest**.

## Installable and portable editions

### Installer

Run `ZipNest-<version>-setup-x64.exe`. Setup lets you choose whether the two Explorer commands are installed. At the end, it can open ZipNest's page in Windows Default Apps so **you** can choose ZIP, 7z, and RAR associations. ZipNest does not bypass Windows' protected default-app controls.

The normal Windows ZIP icon is retained when ZipNest is selected. ZipNest never writes to Windows' built-in `CompressedFolder` registration.

On Windows 11, the lightweight registry-based commands appear under **Show more options**. This keeps the installation unpackaged and avoids a resident Explorer extension.

Initial community builds are not Authenticode-signed, so Windows SmartScreen may show an **Unknown publisher** warning. The SHA-256 file beside each release artifact lets you verify that the download matches the published build.

### Portable

Extract `ZipNest-<version>-portable-x64.zip` anywhere and run `ZipNest.exe`. Settings stay beside the executable. Explorer integration is optional, but remember to turn it off before moving the portable folder because Windows stores its current path.

## Quiet by design

- Double-click an archive, use either right-click command, drag archives onto `ZipNest.exe`, or pass several archive paths at once.
- Successful extraction opens no windows and does not navigate Explorer.
- Password-protected archives show one focused password prompt.
- Errors are reported together after a multi-file operation.
- ZIP and 7z are supported for extraction; RAR is extraction-only, as provided by 7-Zip.

## Safety and rollback

ZipNest treats archives as untrusted input:

- extracts into a unique hidden staging folder on the same volume;
- rejects absolute paths, `..` traversal, alternate data streams, symbolic links, hard links, and reparse points;
- defaults to limits of 250,000 entries and 50 GB expanded data;
- never merges with an existing destination or silently overwrites a duplicate;
- leaves the original archive untouched unless extraction and validation both finish;
- removes the staged extraction if the final archive move cannot complete.

The original archive is moved, not deleted. Logs are written to `%LOCALAPPDATA%\ZipNest\Logs\ZipNest.log` only when ZipNest runs.

## Small, self-contained stack

ZipNest is a 64-bit .NET Framework 4.8 Windows Forms application with no NuGet runtime dependencies. Supported Windows 10 and 11 releases already include that framework. Archive work is delegated to the official x64 7-Zip 26.04 command-line engine (`7z.exe` and `7z.dll`), which is bundled in both editions.

The build downloads official 7-Zip files over HTTPS and checks pinned SHA-256 hashes before packaging them. See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

## Build

Requirements:

- Windows 10 or 11 x64
- .NET SDK 8 or newer
- Internet access on the first build (the scripts fetch hash-pinned 7-Zip and, for installer builds, the official portable-mode Inno Setup compiler)

```powershell
.\build\Build.ps1 -Version 0.1.0
```

For a portable-only local build:

```powershell
.\build\Build.ps1 -Version 0.1.0 -SkipInstaller
```

The script regenerates both icons, verifies its downloaded build inputs, builds the app, runs unit-style and end-to-end smoke tests (including encrypted archives and rollback), and writes packages plus SHA-256 files to `artifacts\`.

## Command line

```text
ZipNest.exe [--nest|--organize] archive1.zip [archive2.7z ...]
ZipNest.exe --settings
ZipNest.exe --install-integration
ZipNest.exe --remove-integration
ZipNest.exe --open-default-apps
```

## License

ZipNest is released under the [MIT License](LICENSE). Bundled 7-Zip components retain their own licenses and unRAR restriction; see the included `7zip\License.txt` and [7-zip.org](https://www.7-zip.org/).
