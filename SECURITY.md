# Security Policy

*Last updated: 2026-08-08*

ImageGlass opens files that users receive from anywhere, so security reports are taken seriously. ImageGlass is maintained by one person, I will try to read and answer every report. Thank you for helping keep users safe.


## Supported versions

| Version | Supported |
| ------- | --------- |
| 10.x | ✅ Actively supported (security and bug fixes) |
| 9.x | ✅ Critical hotfixes only |
| ≤ [8.12](https://github.com/d2phap/ImageGlass/releases/tag/8.12.4.30) | ❌ Not supported |

Notes:

- The 10.x line covers both editions, **Classic** and **Pro**, and every official channel: the installers and portable packages from [imageglass.org/download](https://imageglass.org/download), the Microsoft Store package, and the Flatpak build. Security fixes ship to all of them.
- Every report is handled the same way whatever edition you use. A Pro license is not needed to report a vulnerability or to receive the fix.
- Fixes land in the current 10.x line. There are no backports to 9.x outside a paid support agreement.


## Reporting a vulnerability

**Please do not open a public issue, discussion, or Discord message for a security problem.**

Report it privately, either way works:

- **GitHub**: open a private report from the [Security tab](https://github.com/d2phap/ImageGlass/security/advisories) of this repository ("Report a vulnerability").
- **Email**: phap@imageglass.org, with `SECURITY` in the subject line.

Please include as much of this as you can:

- Affected version, edition, install channel (installer, portable, Microsoft Store, Flatpak), and OS version
- Steps to reproduce, and the sample file that triggers it if the issue is format-specific
- What an attacker gains: crash, memory corruption, code execution, file overwrite, information disclosure
- Any proof of concept, crash dump, or stack trace you have

If you send a sample file, please say clearly that it is a malicious or malformed sample.


## What to expect

These are best-effort targets from a single maintainer, not contractual commitments:

- **Acknowledgement**: within 5 business days
- **Initial assessment**: within 14 days, including whether the report is in scope and how severe it looks
- **Fix**: in the next scheduled release where practical, sooner for a serious, actively exploitable issue
- **Disclosure**: coordinated. The advisory is published after the fix is released, and you are credited in the release notes and the advisory unless you prefer to stay anonymous.

If you do not hear back within the acknowledgement window, please send a follow-up. Do not assume the report was received and ignored.


## Scope

**In scope**

- The ImageGlass 10 application code in [source/](source/)
- Memory-safety issues reached by decoding, rendering, or reading metadata from an untrusted image file
- Path traversal, unsafe file writes, or privilege issues in file operations (save, convert, delete, external tools)
- Issues in the update check, the license file handling, or the plugin and tool host that a remote or local attacker can abuse
- Official installers, packages, and the signing or packaging pipeline
- Official ImageGlass plugins, tools, and themes

**Out of scope**

- Anything downloaded from an unofficial mirror, fork, or Gist. Only [imageglass.org](https://imageglass.org), this repository, the [ImageGlass organization](https://github.com/ImageGlass), and the Microsoft Store listing are official sources. Impersonating repositories and fake "portable patch" builds are a known, active problem; please report those so they can be taken down, rather than as a vulnerability.
- Third-party plugins, codecs, or tools published by someone else. Report those to their author.
- Vulnerabilities in upstream dependencies (SkiaSharp, Magick.NET, Avalonia, and so on). Report them upstream first, then send a note here so the updated version can be picked up. A report that ImageGlass ships an outdated, vulnerable build is always welcome.
- Out-of-memory conditions, hangs, or slowdowns caused by deliberately oversized images, unless they lead to memory corruption
- Missing hardening or a scanner finding with no demonstrated impact
- Social engineering, physical access, or attacks that require an already compromised machine or account
- **Pro licensing bypass.** The license file signature check is a local integrity check, not DRM. There is no activation server, no device lock, and no account requirement. Reports about unlocking Pro features are not treated as security vulnerabilities. Genuine flaws in *how* the signature is verified, for example a bug that lets a crafted license file execute code or corrupt config, are in scope and welcome.


## Privacy and data

ImageGlass does not upload your images or file paths. The app performs an update check that carries anonymous, aggregate information, and it can be turned off. The full detail is in the [privacy policy](https://imageglass.org/privacy). If you find a case where the app discloses more than that policy describes, report it as a vulnerability.


## Safe harbor

Security research carried out in good faith under this policy will not be met with legal action: report privately, allow reasonable time for a fix, do not access or modify other people's data, and do not degrade the service for others. There is no paid bug bounty program at this time.
