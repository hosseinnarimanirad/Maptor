# ADR-0003: Use JWT for Authentication

## Status
Accepted

## Context
We need an authentication mechanism that:
- Works with REST API
- Supports stateless authentication
- Can be used by both API and desktop client
- Supports role-based authorization
- Is industry standard

## Decision
We will use JWT (JSON Web Tokens) Bearer token authentication.

## Consequences

### Positive
- **Stateless**: No server-side session storage needed
- **Scalable**: Works well with multiple API instances
- **Standard**: Industry-standard authentication method
- **Flexible**: Can include claims (roles, permissions) in token
- **Cross-Platform**: Works with any client that can send HTTP headers

### Negative
- **Token Size**: Tokens can be large if many claims included
- **Revocation**: Difficult to revoke tokens before expiration
- **Security**: Tokens must be stored securely on client
- **Expiration**: Requires token refresh mechanism for long sessions

## Implementation
- JWT tokens issued upon successful login
- Tokens include user ID, email, and roles
- Tokens expire after 24 hours
- Client stores token and includes in Authorization header
- API validates token on each request

## Security Considerations
- Tokens signed with secret key
- HTTPS required for token transmission
- Client must store tokens securely
- Consider refresh token mechanism for future

## References
- JWT.io
- ASP.NET Core JWT authentication

