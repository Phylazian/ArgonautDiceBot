# Security Policy

## Supported Versions

This project is currently in early public testing (v0.1).
Security updates will be applied to the latest version only.

---

## Reporting a Vulnerability

If you discover a security vulnerability in ArgonautDiceBot, please do NOT open a public issue.

Instead, contact the maintainer directly.

Please include:

- A detailed description of the vulnerability
- Steps to reproduce the issue
- Any relevant logs or screenshots

Vulnerabilities will be reviewed and addressed as quickly as possible.

---

## Security Design Principles

ArgonautDiceBot follows these principles:

- Bot token is never stored in source code
- No persistent user data storage (v0.1)
- No message scraping
- No background unsolicited messaging
- All actions are command-invoked only
- Minimal permission footprint
