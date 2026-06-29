# Security Policy

## Supported Versions

| Version | Supported          |
|---------|--------------------|
| 1.0.x   | :white_check_mark: |

## Reporting a Vulnerability

We take the security of WebDevSecOps seriously. If you believe
you have found a security vulnerability, please report it to us
privately.

**Do not report security vulnerabilities through public GitHub issues.**

Instead, please report via email to:
  security@webdevsecops.dev

You should receive a response within 48 hours. If for some
reason you do not, please follow up to ensure we received
your report.

We ask that you:
- Provide a detailed description of the vulnerability
- Include steps to reproduce the issue
- Share any proof-of-concept code if available

We will acknowledge receipt of your report within 2 business
days and provide an estimated timeline for a fix.

## Disclosure Policy

We follow a coordinated disclosure process:
1. Report received and acknowledged
2. Investigation and validation
3. Fix developed and tested
4. Fix released (typically within 14 days for critical issues)
5. Public disclosure after the fix is available

## Security Scanning

This project uses the following security tools in CI/CD:
- **SAST**: Roslyn analyzers (SecurityCodeScan + SonarAnalyzer),
  CodeQL, SonarCloud Quality Gate
- **SCA**: NuGet Audit, Snyk
- **Secret Scanning**: Gitleaks
- **DAST**: OWASP ZAP
- **Container Scanning**: Trivy
