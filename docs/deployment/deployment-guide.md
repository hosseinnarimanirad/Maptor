# Deployment Guide - MakanNegarSaba

## Overview

This guide provides step-by-step instructions for deploying the MakanNegarSaba application to production, staging, and development environments.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Environment Setup](#environment-setup)
3. [Database Deployment](#database-deployment)
4. [API Deployment](#api-deployment)
5. [Client Deployment](#client-deployment)
6. [Configuration](#configuration)
7. [Verification](#verification)
8. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Server Requirements

- **Operating System**: Windows Server 2016+ or Linux (Ubuntu 20.04+)
- **.NET Runtime**: .NET 8.0 Runtime or SDK
- **Database**: SQL Server 2016 or later
- **Web Server**: IIS (Windows) or Nginx/Kestrel (Linux)
- **SSL Certificate**: For HTTPS

### Development Machine Requirements

- **.NET SDK**: .NET 8.0 SDK
- **SQL Server Management Studio**: For database management
- **Git**: For source control

---

## Environment Setup

### 1. Install .NET Runtime

**Windows**:
```powershell
# Download and install .NET 8.0 Runtime from:
# https://dotnet.microsoft.com/download
```

**Linux**:
```bash
wget https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --runtime dotnet --version 8.0.0
```

### 2. Install SQL Server

Install SQL Server 2016 or later with spatial data support.

### 3. Configure Firewall

**Windows**:
```powershell
# Allow HTTP (port 80) and HTTPS (port 443)
New-NetFirewallRule -DisplayName "MakanNegarSaba API" -Direction Inbound -Protocol TCP -LocalPort 80,443 -Action Allow
```

**Linux**:
```bash
# Allow HTTP and HTTPS
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
```

---

## Database Deployment

### 1. Create Database

```sql
CREATE DATABASE Barg;
GO

USE Barg;
GO

-- Verify spatial support
SELECT SERVERPROPERTY('ProductVersion') AS Version;
```

### 2. Run Migrations

**Option A: Using EF Core CLI**

```bash
cd src/IRI.App/Barg/Presentation/IRI.App.MakanNegarSaba.Api
dotnet ef database update --project ../../Infrastructure/IRI.App.MakanNegarSaba.Ef --connection "Server=YOUR_SERVER;Database=Barg;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True"
```

**Option B: Generate SQL Script**

```bash
dotnet ef migrations script --project Infrastructure/IRI.App.MakanNegarSaba.Ef --output migrations.sql
```

Then execute `migrations.sql` in SQL Server Management Studio.

### 3. Seed Initial Data

Seed data for roles and permissions is included in migrations. Verify:

```sql
SELECT * FROM Roles;
SELECT * FROM RolePermissions;
```

### 4. Create Database User (Optional)

```sql
CREATE LOGIN makannegarsaba WITH PASSWORD = 'StrongPassword123!';
USE Barg;
CREATE USER makannegarsaba FOR LOGIN makannegarsaba;
ALTER ROLE db_datareader ADD MEMBER makannegarsaba;
ALTER ROLE db_datawriter ADD MEMBER makannegarsaba;
```

---

## API Deployment

### Option 1: Windows IIS Deployment

#### 1. Publish API

```bash
cd src/IRI.App/Barg/Presentation/IRI.App.MakanNegarSaba.Api
dotnet publish -c Release -o C:\inetpub\wwwroot\makannegarsaba-api
```

#### 2. Configure IIS

1. Open IIS Manager
2. Create new Application Pool:
   - Name: `MakanNegarSabaApi`
   - .NET CLR Version: No Managed Code
   - Managed Pipeline Mode: Integrated
3. Create new Website:
   - Name: `MakanNegarSaba API`
   - Physical Path: `C:\inetpub\wwwroot\makannegarsaba-api`
   - Binding: HTTP (port 80) or HTTPS (port 443)
   - Application Pool: `MakanNegarSabaApi`

#### 3. Configure web.config

Ensure `web.config` is present with correct settings.

### Option 2: Linux Deployment (Systemd Service)

#### 1. Publish API

```bash
cd src/IRI.App/Barg/Presentation/IRI.App.MakanNegarSaba.Api
dotnet publish -c Release -o /var/www/makannegarsaba-api
```

#### 2. Create Systemd Service

Create `/etc/systemd/system/makannegarsaba-api.service`:

```ini
[Unit]
Description=MakanNegarSaba API
After=network.target

[Service]
Type=notify
ExecStart=/usr/bin/dotnet /var/www/makannegarsaba-api/IRI.App.MakanNegarSaba.Api.dll
Restart=always
RestartSec=10
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

#### 3. Start Service

```bash
sudo systemctl enable makannegarsaba-api
sudo systemctl start makannegarsaba-api
sudo systemctl status makannegarsaba-api
```

#### 4. Configure Nginx Reverse Proxy

Create `/etc/nginx/sites-available/makannegarsaba-api`:

```nginx
server {
    listen 80;
    server_name api.makannegarsaba.local;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }
}
```

Enable site:
```bash
sudo ln -s /etc/nginx/sites-available/makannegarsaba-api /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

### Option 3: Docker Deployment

#### 1. Build Docker Image

```bash
cd src/IRI.App/Barg/Presentation/IRI.App.MakanNegarSaba.Api
docker build -t makannegarsaba-api:latest .
```

#### 2. Run Container

```bash
docker run -d \
  --name makannegarsaba-api \
  -p 5000:80 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal;Database=Barg;User Id=sa;Password=Password;TrustServerCertificate=True" \
  makannegarsaba-api:latest
```

---

## Client Deployment

### 1. Build Installer

**Using Visual Studio**:
1. Right-click on `IRI.App.MakanNegarSaba` project
2. Select "Publish"
3. Choose "Folder" or "ClickOnce"
4. Configure settings
5. Publish

**Using Command Line**:
```bash
cd src/IRI.App/Barg/IRI.App.MakanNegarSaba
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish
```

### 2. Create Installer Package

Use tools like:
- WiX Toolset
- Inno Setup
- Advanced Installer

### 3. Distribute Installer

- Share installer file with users
- Or host on file server
- Or use ClickOnce deployment

### 4. Client Configuration

Users need to configure API endpoint in application settings:
- Settings file: `appsettings.json` or application settings
- API Base URL: `https://api.makannegarsaba.local`

---

## Configuration

### API Configuration (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=Barg;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True"
  },
  "Jwt": {
    "SecretKey": "YOUR_SECRET_KEY_HERE",
    "Issuer": "MakanNegarSaba",
    "Audience": "MakanNegarSabaUsers",
    "ExpirationMinutes": 1440
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

### Environment Variables

Set environment variables for sensitive data:

**Windows**:
```powershell
$env:ConnectionStrings__DefaultConnection = "Server=..."
$env:Jwt__SecretKey = "..."
```

**Linux**:
```bash
export ConnectionStrings__DefaultConnection="Server=..."
export Jwt__SecretKey="..."
```

---

## Verification

### 1. Verify API

```bash
# Health check
curl http://localhost:5000/health

# Swagger UI
# Open browser: http://localhost:5000/swagger
```

### 2. Verify Database

```sql
-- Check tables exist
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';

-- Check spatial support
SELECT SERVERPROPERTY('ProductVersion');
```

### 3. Verify Authentication

```bash
# Register user
curl -X POST http://localhost:5000/api/User/SignUp \
  -H "Content-Type: application/json" \
  -d '{"emailAddress":"test@example.com","plainPassword":"Password123!"}'

# Login
curl -X POST http://localhost:5000/api/User/Login \
  -H "Content-Type: application/json" \
  -d '{"emailAddress":"test@example.com","plainPassword":"Password123!"}'
```

### 4. Verify Client

1. Launch WPF application
2. Configure API endpoint
3. Test login
4. Verify map loads
5. Test feature access

---

## Troubleshooting

### API Won't Start

**Check**:
- .NET runtime installed
- Port not in use
- Configuration file correct
- Database accessible

**Logs**:
```bash
# Windows
Get-Content C:\inetpub\wwwroot\makannegarsaba-api\logs\*.log

# Linux
sudo journalctl -u makannegarsaba-api -f
```

### Database Connection Fails

**Check**:
- SQL Server running
- Connection string correct
- Firewall allows connection
- User has permissions

**Test Connection**:
```bash
sqlcmd -S YOUR_SERVER -U sa -P YOUR_PASSWORD -Q "SELECT @@VERSION"
```

### Client Can't Connect to API

**Check**:
- API URL correct in settings
- API is running
- Network connectivity
- Firewall rules
- CORS settings (if web client)

---

## Backup and Recovery

### Database Backup

```sql
-- Full backup
BACKUP DATABASE Barg TO DISK = 'C:\Backups\Barg.bak';

-- Schedule regular backups
```

### Application Backup

- Backup configuration files
- Backup published application
- Document configuration changes

### Recovery Procedure

1. Restore database from backup
2. Restore application files
3. Update configuration
4. Restart services
5. Verify functionality

---

## Monitoring

### Application Logs

- **Location**: Configured in `appsettings.json`
- **Format**: Structured logging (Serilog)
- **Rotation**: Configure log rotation

### Performance Monitoring

- Monitor API response times
- Monitor database performance
- Monitor server resources (CPU, memory, disk)

### Health Checks

Implement health check endpoint:
```csharp
[HttpGet("health")]
public IActionResult Health()
{
    return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
}
```

---

## Security Checklist

- [ ] HTTPS configured
- [ ] SSL certificate valid
- [ ] Database credentials secure
- [ ] JWT secret key strong and secure
- [ ] Firewall configured
- [ ] Regular backups scheduled
- [ ] Security updates applied
- [ ] Monitoring configured

---

**Last Updated**: 2024

