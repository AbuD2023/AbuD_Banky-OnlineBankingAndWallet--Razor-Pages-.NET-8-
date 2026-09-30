# Contributing to AbuD Banky ASP.NET

Thank you for helping improve the Banky API and administration portal. Contributions should keep the API contract reliable, protect financial and identity data, and include enough context for maintainers to review the change.

## Scope

This repository contains `Banky.API` and `Banky.Web`. The Flutter client lives in a [separate repository](https://github.com/AbuD2023/AbuD_Banky-OnlineBankingAndWallet--Flutter_Dart_Mobile_Phone). Changes to API behavior may affect that client, so call out breaking changes and update the relevant documentation.

## Development setup

1. Fork and clone this repository.
2. Install the .NET 8 SDK and make a SQL Server instance available.
3. Configure a local database connection and a strong development-only JWT key. Keep secrets out of source control.
4. Restore, build, and run the solution:

   ```bash
   dotnet restore Banky.slnx
   dotnet build Banky.slnx
   dotnet run --project Banky.API
   dotnet run --project Banky.Web
   ```

Run the API and web portal in separate terminals. Configure `ApiSettings:BaseUrl` for the web portal to point to the API instance.

## Contribution workflow

- Create a focused branch from the repository's default branch, for example `feature/fee-preview` or `fix/wallet-validation`.
- Follow the existing C# and ASP.NET Core patterns. Keep API, persistence, and UI responsibilities in their existing projects.
- Add or update tests when behavior changes. Do not claim test coverage that has not been run.
- Update README or API-facing documentation when setup, configuration, or client-visible behavior changes.
- Open a pull request with the problem, the solution, verification performed, and any API compatibility impact. Link related issues.

## Security and financial behavior

Never commit credentials, signing keys, real connection strings, customer records, or identity documents. Report suspected security vulnerabilities privately to the repository owner instead of publishing exploit details in an issue. Changes to authentication, authorization, fees, transfers, balances, or KYC need particular care and clear tests.

## License

By contributing, you agree that your contributions are made available under the repository's [MIT License](LICENSE).
