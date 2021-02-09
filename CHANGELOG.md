# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added
- New `forced` bool parameter to force a re-authentication when using `Authenticate()`.
### Changed
- `Authenticate()` now do not authenticate if `isAuthenticated` is already true and
- `Authenticate()` also now blocks any authentication attempt if there is already an attempt going on.
- `isAuthenticating` is also available for checking the authentication process.

## 0.3.1 - 2021-02-09
### Changed
- Simplified TrophyData object initialization.

## 0.3.0 - 2021-02-09
### Added
- `GrantTrophy()` for granting trophies and
- `RevokeTrophy()` for revoking them.
- `FetchTrophy()` for getting informations about a trophy.
- `ListTrophies()` to get a list of trophies.
- `OnTrophy` event.
- Documented methods with summaries.

## 0.2.0 - 2021-02-08
### Added
- Basic user authentication.
- `OnAuthenticate` event.
- Username and token caching.
- Error check for API calls.

### Removed
- Redundant `Debug.Log()`s when making API calls.

## 0.1.0 - 2021-02-08
### Added
- First implementation of the `GetServerTime()` method.
