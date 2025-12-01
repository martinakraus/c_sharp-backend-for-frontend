# Backend-for-Frontend (BFF) in C#

Ein modular aufgebautes Backend-for-Frontend, das den OAuth 2.0 PKCE Flow mit Keycloak implementiert und als Proxy für API-Aufrufe dient.

## Architektur

Das BFF verwendet:
- **PKCE Flow** (Proof Key for Code Exchange) mit Keycloak als confidential client
- **HTTP-only Cookies** zum sicheren Speichern von Access Tokens
- **API Proxy** zur Weiterleitung von Anfragen mit Authorization Header
- **Modulare Struktur** mit Services und Controllern

## Projektstruktur

```
backend-for-frontend/
├── Controllers/
│   ├── AuthController.cs      # OAuth Login/Logout/Callback
│   └── ProxyController.cs     # API Proxy
├── Services/
│   ├── IOAuthService.cs       # OAuth-Operationen
│   ├── OAuthService.cs
│   ├── IPkceService.cs        # PKCE Code-Generierung
│   ├── PkceService.cs
│   ├── ISessionService.cs     # Session Management
│   ├── SessionService.cs
│   ├── ApiProxyService.cs     # API Forwarding
│   └── IApiProxyService.cs
├── Models/
│   ├── OAuthOptions.cs        # OAuth-Konfiguration
│   └── ApiProxyOptions.cs     # API-Konfiguration
└── Program.cs                 # Dependency Injection & Middleware
```

## Verwendete NuGet Packages

- `Microsoft.AspNetCore.Authentication.OpenIdConnect` - OpenID Connect Integration
- `Microsoft.AspNetCore.Authentication.Cookies` - Cookie Authentication
- `IdentityModel` - OAuth/OIDC Helper-Bibliothek

## Endpoints

### Authentication

- `GET /auth/login` - Startet den OAuth-Flow
- `GET /auth/callback` - Callback nach erfolgreicher Authentifizierung
- `POST /auth/logout` - Logout und Token-Revocation
- `GET /auth/status` - Prüft Authentication-Status

### API Proxy

- `GET/POST/PUT/DELETE/PATCH /api/{**path}` - Leitet Anfragen an die Backend-API weiter

## Konfiguration

In `appsettings.json`:

```json
{
  "OAuth": {
    "Authority": "http://localhost:8080/realms/bff-realm",
    "ClientId": "bff-client",
    "ClientSecret": "bff-secret",
    "Scopes": ["openid", "profile", "email", "roles"],
    "RedirectUri": "http://localhost:5000/auth/callback",
    "PostLogoutRedirectUri": "http://localhost:5000/"
  },
  "ApiProxy": {
    "ApiBaseUrl": "http://localhost:5001"
  }
}
```

## Verwendung

### 1. Keycloak starten

```bash
cd keycloak
docker-compose up -d
```

### 2. Backend API starten

```bash
cd api
dotnet run
```

### 3. BFF starten

```bash
cd backend-for-frontend
dotnet restore
dotnet run
```

Das BFF läuft auf `http://localhost:5000`

## Workflow

1. **Login**: Frontend ruft `GET /auth/login` auf
2. **Redirect**: BFF erstellt PKCE-Parameter und redirected zu Keycloak
3. **Callback**: Nach erfolgreicher Authentifizierung ruft Keycloak `/auth/callback` auf
4. **Token Exchange**: BFF tauscht Authorization Code gegen Access Token
5. **Cookie**: Access Token wird in HTTP-only Cookie gespeichert
6. **API Calls**: Frontend ruft `/api/*` auf, BFF extrahiert Token aus Cookie und fügt als Bearer Token hinzu

## Sicherheitsfeatures

- ✅ **PKCE** schützt vor Authorization Code Interception
- ✅ **HTTP-only Cookies** verhindern XSS-Angriffe auf Tokens
- ✅ **State Parameter** schützt vor CSRF-Angriffen
- ✅ **Confidential Client** mit Client Secret
- ✅ **SameSite Cookie Policy** für zusätzlichen CSRF-Schutz
- ✅ **Token Revocation** bei Logout

## Integration im Frontend

```typescript
// Login
window.location.href = 'http://localhost:5000/auth/login';

// API Call (Cookie wird automatisch mitgesendet)
const response = await fetch('http://localhost:5000/api/users', {
  credentials: 'include'
});

// Logout
await fetch('http://localhost:5000/auth/logout', {
  method: 'POST',
  credentials: 'include'
});
```

## Vorteile dieser Architektur

1. **Token-Sicherheit**: Access Token verlässt nie das BFF
2. **Einfaches Frontend**: Keine OAuth-Komplexität im Frontend
3. **Zentrale Authentifizierung**: Ein Auth-Flow für alle APIs
4. **Modularer Code**: Klare Trennung von Verantwortlichkeiten
5. **Idiomatisches C#**: Verwendet ASP.NET Core Best Practices
