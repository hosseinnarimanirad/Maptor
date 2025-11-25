# Security Documentation - MakanNegarSaba

## Overview

This document describes the security architecture, authentication, authorization, and security best practices for the MakanNegarSaba application.

---

## Table of Contents

1. [Security Architecture](#security-architecture)
2. [Authentication](#authentication)
3. [Authorization](#authorization)
4. [Data Protection](#data-protection)
5. [Security Best Practices](#security-best-practices)
6. [Security Checklist](#security-checklist)

---

## Security Architecture

### Defense in Depth

The application implements multiple layers of security:

1. **Network Security**: HTTPS/TLS encryption
2. **Authentication**: JWT token-based authentication
3. **Authorization**: Role-based access control (RBAC)
4. **Data Encryption**: RSA encryption for sensitive communications
5. **Input Validation**: Server-side validation
6. **Audit Logging**: Security event logging

---

## Authentication

### User Registration

**Process**:
1. User provides email and password
2. Request encrypted with RSA public key
3. Server validates email format and uniqueness
4. Password hashed using MD5 with unique stamp
5. User account created with `IsActive = false`
6. Verification email sent

**Security Measures**:
- Email validation
- Password strength requirements (minimum 8 characters)
- Password hashing (MD5 with user-specific stamp)
- RSA encryption for transmission
- Email verification required before activation

### User Login

**Process**:
1. User provides email and password
2. Request encrypted with RSA public key
3. Server validates credentials
4. Server checks email verification status
5. Server checks account active status
6. Server checks account lock status
7. JWT token issued upon success
8. Login attempt logged

**Security Measures**:
- Encrypted credentials transmission
- Account lockout after 5 failed attempts
- Lock duration: 30 minutes
- Failed attempt tracking
- Login audit logging (IP address, timestamp)

### JWT Token Authentication

**Token Structure**:
- **Header**: Algorithm (HS256), type (JWT)
- **Payload**: User ID, email, roles, expiration
- **Signature**: HMAC SHA256

**Token Claims**:
```json
{
  "sub": "123",           // User ID
  "email": "user@example.com",
  "roles": ["User", "Manager"],
  "exp": 1234567890,      // Expiration timestamp
  "iat": 1234567890       // Issued at timestamp
}
```

**Token Lifecycle**:
- Issued upon successful login
- Valid for 24 hours
- Included in `Authorization: Bearer {token}` header
- Validated on each API request
- Expired tokens rejected

**Security Considerations**:
- Tokens stored securely on client
- HTTPS required for token transmission
- Token expiration enforced
- No server-side token storage (stateless)

---

## Authorization

### Role-Based Access Control (RBAC)

**Roles**:
- **SuperAdmin**: Full system access
- **Admin**: User and role management
- **Manager**: Operational data management
- **User**: View and basic operations
- **Viewer**: Read-only access

**Permission System**:
- Permissions organized by category
- Roles assigned multiple permissions
- Users assigned multiple roles
- Permissions checked at API level

**Permission Categories**:
- User Management
- Role Management
- Substations
- Transmission Lines
- Power Stations
- Communications
- Layer Settings
- Reports

### Authorization Flow

1. User makes API request with JWT token
2. API extracts user ID and roles from token
3. API checks user's permissions
4. API verifies required permission for endpoint
5. Request allowed or denied (403 Forbidden)

### Permission Checking

**Example**:
```csharp
[Authorize(Policy = "SubstationsViewAll")]
[HttpGet]
public async Task<ActionResult> GetSubstations()
{
    // Only users with SubstationsViewAll permission can access
}
```

---

## Data Protection

### Password Storage

**Hashing Algorithm**: MD5 with user-specific stamp

**Process**:
1. User provides plain password
2. System generates unique stamp (GUID) for user
3. Password hashed: `MD5(password + stamp)`
4. Hash stored in database
5. Stamp stored in user record

**Verification**:
```csharp
public bool ValidatePassword(string plainPassword)
{
    var hash = MD5(plainPassword + this.Stamp);
    return hash == this.PasswordHash;
}
```

### Data Encryption

**RSA Encryption**:
- Used for registration and login requests
- Server has public/private key pair
- Client encrypts sensitive data with public key
- Server decrypts with private key

**HTTPS/TLS**:
- All API communication over HTTPS
- TLS 1.2 or later required
- Certificate validation enforced

### Database Security

- Connection strings stored in configuration (not in code)
- SQL injection prevention via parameterized queries (EF Core)
- Database access restricted to application
- Regular backups
- Access logging

---

## Security Best Practices

### For Developers

1. **Never Commit Secrets**:
   - No passwords in code
   - No API keys in code
   - Use configuration files or environment variables

2. **Validate Input**:
   - Validate all user input
   - Use parameterized queries
   - Sanitize output

3. **Use HTTPS**:
   - Always use HTTPS in production
   - Enforce HTTPS redirects

4. **Keep Dependencies Updated**:
   - Regularly update NuGet packages
   - Check for security vulnerabilities

5. **Log Security Events**:
   - Log authentication attempts
   - Log authorization failures
   - Log suspicious activity

### For Administrators

1. **User Management**:
   - Review new user registrations
   - Deactivate inactive accounts
   - Monitor failed login attempts

2. **Role Management**:
   - Assign minimal required permissions
   - Review role assignments regularly
   - Remove unused roles

3. **Monitoring**:
   - Monitor security logs
   - Review user activity
   - Investigate suspicious behavior

4. **Updates**:
   - Keep system updated
   - Apply security patches promptly
   - Review security advisories

---

## Security Checklist

### Development

- [ ] No secrets in code or repository
- [ ] Input validation implemented
- [ ] Output encoding implemented
- [ ] SQL injection prevention (parameterized queries)
- [ ] XSS prevention (output encoding)
- [ ] CSRF protection (if web forms)
- [ ] HTTPS enforced
- [ ] Error messages don't leak information
- [ ] Security headers configured
- [ ] Dependencies updated

### Deployment

- [ ] HTTPS configured
- [ ] Database credentials secure
- [ ] Connection strings in configuration
- [ ] Firewall rules configured
- [ ] Security logging enabled
- [ ] Backup procedures in place
- [ ] Access controls configured
- [ ] Monitoring configured

### Operations

- [ ] User accounts reviewed regularly
- [ ] Roles and permissions reviewed
- [ ] Security logs monitored
- [ ] Failed login attempts investigated
- [ ] System updates applied
- [ ] Security patches applied
- [ ] Backup tested
- [ ] Incident response plan ready

---

## Security Incident Response

### If Security Breach Suspected

1. **Immediate Actions**:
   - Isolate affected systems
   - Preserve logs
   - Notify security team

2. **Investigation**:
   - Review security logs
   - Identify affected users/data
   - Determine attack vector

3. **Remediation**:
   - Patch vulnerabilities
   - Reset compromised credentials
   - Notify affected users

4. **Prevention**:
   - Update security measures
   - Review and improve procedures
   - Document lessons learned

---

## Security References

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [ASP.NET Core Security](https://docs.microsoft.com/en-us/aspnet/core/security/)
- [JWT Best Practices](https://tools.ietf.org/html/rfc8725)

---

**Last Updated**: 2024  
**Security Contact**: [Contact Information]

