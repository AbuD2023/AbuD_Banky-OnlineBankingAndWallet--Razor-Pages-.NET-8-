# Contributing to AbuD-Banky

Thank you for your interest in contributing to AbuD-Banky. This document explains how to set up a development environment, the preferred workflow, and guidelines to make contributions straightforward.

## Getting started
1. Fork the repository and clone your fork.
2. Create a branch for your work:
   - Feature: `feature/<short-description>`
   - Fix: `fix/<short-description>`
3. Run the solution locally:
   ```bash
   dotnet restore
   dotnet build
   dotnet run --project Banky.API # if applicable
   dotnet run --project Banky.Web
   ```

## Coding standards
- C# 11 (as part of .NET 8)
- Follow existing code style in the repository.
- Keep Razor Pages organized under `Pages` or `Views` depending on project layout.
- Prefer dependency injection and configuration via `IConfiguration` and `IOptions<T>`.

## Tests
- Add unit tests where applicable.
- Ensure tests pass locally before opening a PR.

## Pull request process
1. Keep changes focused and small.
2. Rebase or merge from `upgrede_one` (main development branch in this repo) to keep history clean.
3. Provide a clear description of what the PR changes and why.
4. Link related issues if any.

## Issues
- Open an issue to discuss large features before implementing.
- Use clear titles and provide reproduction steps for bugs.

## Code review
- Be responsive to review feedback.
- Add tests and documentation for non-trivial changes.

## Security
- Do not commit secrets, connection strings, or credentials.
- Use GitHub Secrets for CI/CD and environment variables for local development.

Thank you for helping improve AbuD-Banky!
