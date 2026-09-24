# Security baseline

## Authentication and authorization

- The API uses JWT bearer authentication with issuer, audience, lifetime, and signing-key validation enabled.
- Role claims are mapped to the `role` claim type and enforced through `Authorize(Roles = ...)` and policy registration in the application layer.
- Production configuration requires a non-default `Jwt:Key` value and rejects the development placeholder value.

## CORS policy

- The default CORS policy is explicit and environment-aware.
- Allowed origins are set from `Cors:AllowedOrigins` in configuration.
- Local development may allow a broader origin set for convenience, but production requires an explicit allow list.

## Rate limiting

- Fixed and sliding-window rate limits are configured in the API entrypoint.
- The default policy is designed to protect public endpoints and expensive write actions.

## Secret loading

- Secrets are expected to come from environment variables or external secret stores.
- The repository uses `.env` only as a local developer convenience and never as a substitute for real deployment secrets.
- The runtime rejects default test secrets in non-development environments.

## Key rotation

- JWT signing keys should rotate on a planned schedule.
- Every rotation must be re-issued and communicated to clients before or together with deployment.
- The key rotation process should be documented in the incident and release runbooks.
