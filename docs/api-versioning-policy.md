# TMS API Versioning Policy
## What counts as a breaking change
A breaking change is anything that causes existing clients to fail without code changes:
- Removing a field from a response
- Renaming a field in a response
- Changing a status code for an existing scenario
- Adding a new required field to a request body
- Tightening validation rules on an existing field like making an optional field mandatory
- Changing the default sort order of a collection

Any of these require a new API version.

## What counts as additive (non-breaking)
These changes are safe to ship without a new version:
- Adding a new optional field to a response
- Adding a new endpoint
- Adding a new optional query parameter
- Loosening validation rules
- Adding a new error code for a previously unhandled case

## Sunset window
When a new version ships, the previous version runs for a minimum of 6 months.
This gives rural training centres on quarterly maintenance schedules time to migrate.
The sunset date is communicated from day one via the Sunset response header.

## Communication
From the moment V2 ships, every V1 response carries:
- Deprecation: true
- Sunset: <RFC 7231 date of shutdown>
- Link: <V2 URL>; rel="successor-version"

In addition: a CHANGELOG entry, an email to every team holding an API key,
and a calendar invite for the V1 shutdown date.

## Skipping versions
Clients are not required to migrate through every intermediate version.
V1 clients may migrate directly to V3 when V3 ships, skipping V2 entirely.