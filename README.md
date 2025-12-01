# C# Backend-for-Frontend (BFF) mit Angular & Keycloak

Dieses Projekt demonstriert das **Backend-for-Frontend (BFF) Pattern** mit C#, Angular und Keycloak. Das BFF handhabt die komplette OAuth/OIDC-Authentifizierung mit PKCE-Flow und agiert als Proxy für API-Aufrufe, wobei Access Tokens sicher in HTTP-only Cookies gespeichert werden.

## 🏗️ Architektur

```
┌──────────────────┐
│  Angular Client  │  Port 4200
│  (Frontend SPA)  │  - Keine Tokens!
└────────┬─────────┘  - Nur HTTP + Cookies
         │
         ▼
┌──────────────────┐
│   C# BFF         │  Port 5000
│   (ASP.NET Core) │  - PKCE Flow
│                  │  - Token in Cookie
└────┬────────┬────┘  - API Proxy
     │        │
     │        └──────────────┐
     ▼                       ▼
┌──────────────┐      ┌──────────────┐
│  Keycloak    │      │  C# API      │
│  (OAuth/OIDC)│      │  (Backend)   │
└──────────────┘      └──────────────┘
Port 8080             Port 5001
```

**Wichtig:** Das Frontend hat **niemals** direkten Zugriff auf Access Tokens. Alle OAuth-Operationen laufen über das BFF.

## 📁 Projektstruktur

```
c_sharp-backend-for-frontend/
├── docker-compose.yml          
├── README.md                   
│
├── api/                        # C# Backend API (Port 5001)
│   ├── Program.cs              # JWT Authentication Setup
│   ├── api.csproj
│   ├── Controllers/
│   │   └── ApiController.cs    # User CRUD Endpoints
│   ├── HasScopeHandler.cs      # Role-basierte Autorisierung
│   └── HasScopeRequirement.cs  
│
├── backend-for-frontend/       # ⭐ C# BFF (Port 5000)
│   ├── Program.cs              # DI & Middleware Setup
│   ├── Controllers/
│   │   ├── AuthController.cs   # OAuth Login/Logout/Callback
│   │   └── ProxyController.cs  # API Proxy mit Token Handling
│   ├── Services/
│   │   ├── OAuthService.cs     # OAuth/PKCE Implementierung
│   │   ├── PkceService.cs      # PKCE Code Generierung
│   │   ├── SessionService.cs   # Session Management
│   │   └── ApiProxyService.cs  # API Request Forwarding
│   ├── Models/
│   │   ├── OAuthOptions.cs     # OAuth Konfiguration
│   │   └── ApiProxyOptions.cs  
│   └── appsettings.json        # BFF & Keycloak Config
│
├── client/                     # Angular Frontend (Port 4200)
│   ├── src/app/
│   │   ├── auth/
│   │   │   ├── auth.service.ts # BFF Integration
│   │   │   └── auth.guard.ts   # Route Protection
│   │   ├── services/
│   │   │   └── user-api.service.ts # API via BFF
│   │   └── header/             # Login/Logout UI
│   └── src/environments/
│       └── environment.ts      # BFF URL (Port 5000)
│
└── keycloak/                   # Keycloak (Port 8080)
    └── realm-export.json       # Realm Config mit bff-client
```

## 🚀 Schnellstart

### Voraussetzungen

- Docker & Docker Compose (für Keycloak)
- Node.js 20+ 
- .NET 10.0 SDK

### 0. Hosts-Datei konfigurieren

**Wichtig:** Damit Browser und Container denselben Keycloak-Hostname verwenden können, muss `keycloak` in der Hosts-Datei eingetragen werden:

#### Windows
1. Als **Administrator** Notepad öffnen
2. Datei öffnen: `C:\Windows\System32\drivers\etc\hosts`
3. Folgende Zeile hinzufügen:
   ```
   127.0.0.1 keycloak
   ```
4. Speichern

#### macOS / Linux
Terminal öffnen und folgenden Befehl ausführen:
```bash
sudo sh -c 'echo "127.0.0.1 keycloak" >> /etc/hosts'
```

Oder manuell bearbeiten:
```bash
sudo nano /etc/hosts
```
Und diese Zeile hinzufügen:
```
127.0.0.1 keycloak
```

#### Überprüfung
```bash
# Test ob keycloak auflösbar ist
ping keycloak
```

**Warum?** Damit haben Browser und Docker-Container dieselbe Keycloak-URL (`http://keycloak:8080`), was zu konsistenten Token-Issuern führt und Issuer-Mismatch-Fehler beim Token-Refresh verhindert.

### 1. Keycloak starten

```bash
docker-compose up -d
```

Keycloak läuft auf: http://keycloak:8080 (bzw. http://localhost:8080)
- Admin User: `admin` / `admin`
- Realm: `bff-realm`
- Client: `bff-client` (confidential mit PKCE)

### 2. Backend API starten

```bash
cd api
dotnet run
```

API läuft auf: http://localhost:5001

### 3. BFF starten

```bash
cd backend-for-frontend
dotnet restore
dotnet run
```

BFF läuft auf: http://localhost:5000

### 4. Frontend starten

```bash
cd client
npm install
npm start
```

Frontend läuft auf: http://localhost:4200

## 🔐 Authentifizierungs-Flow

### PKCE Flow mit BFF

1. **Login**: User klickt auf "Login" im Frontend
   ```typescript
   authService.login(); // → window.location.href = 'http://localhost:5000/auth/login'
   ```

2. **BFF generiert PKCE**:
   - Code Verifier (zufälliger String)
   - Code Challenge (SHA256 des Verifiers)
   - State (CSRF-Schutz)

3. **Redirect zu Keycloak**: BFF redirected mit Challenge
   ```
   http://localhost:8080/realms/bff-realm/protocol/openid-connect/auth
     ?client_id=bff-client
     &code_challenge=...
     &code_challenge_method=S256
     &state=...
   ```

4. **User Login**: User authentifiziert sich bei Keycloak

5. **Callback**: Keycloak redirected zu `/auth/callback?code=...&state=...`

6. **Token Exchange**: BFF tauscht Code gegen Token
   - Validiert State Parameter
   - Sendet Code + Verifier an Keycloak
   - Erhält Access Token

7. **Cookie setzen**: BFF speichert Token in HTTP-only Cookie
   ```csharp
   Response.Cookies.Append("access_token", accessToken, new CookieOptions
   {
       HttpOnly = true,
       Secure = true,
       SameSite = SameSiteMode.Lax
   });
   ```

8. **Redirect**: User wird zurück zu Angular App redirected

### API Calls über BFF Proxy

1. Frontend macht Request:
   ```typescript
   http.get('http://localhost:5000/api/users', { withCredentials: true })
   ```

2. Browser sendet Cookie automatisch mit

3. BFF ProxyController:
   - Extrahiert `access_token` aus Cookie
   - Fügt `Authorization: Bearer <token>` Header hinzu
   - Leitet zu API weiter: `http://localhost:5001/api/users`

4. API validiert JWT und authorisiert Request

5. Response geht zurück durch BFF zu Frontend

## ⚙️ Konfiguration

### BFF (`backend-for-frontend/appsettings.json`)

```json
{
  "OAuth": {
    "Authority": "http://localhost:8080/realms/bff-realm",
    "ClientId": "bff-client",
    "ClientSecret": "bff-secret",
    "Scopes": ["openid", "profile", "email", "roles"],
    "RedirectUri": "http://localhost:5000/auth/callback"
  },
  "ApiProxy": {
    "ApiBaseUrl": "http://localhost:5001"
  }
}
```

### Frontend (`client/src/environments/environment.ts`)

```typescript
export const environment = {
  production: false,
  bffUrl: 'http://localhost:5000'
};
```

### API (`api/appsettings.json`)

```json
{
  "Authentication": {
    "Authority": "http://localhost:8080/realms/bff-realm",
    "Audience": "account"
  }
}
```

## 📡 API Endpoints

### BFF Endpoints (Port 5000)

#### Authentication
| Methode | Endpoint | Beschreibung |
|---------|----------|--------------|
| `GET` | `/auth/login` | Startet OAuth PKCE Flow |
| `GET` | `/auth/callback` | OAuth Callback (intern) |
| `POST` | `/auth/logout` | Logout & Token Revocation |
| `GET` | `/auth/status` | Prüft Authentication Status |

#### API Proxy (alle HTTP-Methoden)
| Pattern | Ziel | Beschreibung |
|---------|------|--------------|
| `/api/**` | `http://localhost:5001/api/**` | Proxy mit Bearer Token |

### Backend API Endpoints (Port 5001)

| Methode | Endpoint | Role | Beschreibung |
|---------|----------|------|--------------|
| `GET` | `/api/users` | `user:read` | Alle User abrufen |
| `GET` | `/api/users/{id}` | `user:read` | User by ID |
| `POST` | `/api/users` | `user:write` | User erstellen |

## 🔑 Test Users

Vorkonfiguriert in Keycloak:

| Username | Password | Rollen |
|----------|----------|--------|
| `testuser` | `password` | user:read, user:write |
| `readonly` | `password` | user:read |

## 🎯 Warum BFF Pattern?

### Probleme mit Token im Frontend
- ❌ XSS-Angriffe können Tokens stehlen
- ❌ LocalStorage/SessionStorage ist unsicher
- ❌ Komplexe OAuth-Logik im Frontend
- ❌ Token Refresh im Browser schwierig
- ❌ Mehrere APIs = mehrere Token

### BFF Lösung
- ✅ Tokens nur im Backend (HTTP-only Cookie)
- ✅ Einfaches Frontend (nur Cookie-based Auth)
- ✅ Zentrales Auth Management
- ✅ Token Refresh transparent
- ✅ Ein Cookie für alle APIs
- ✅ Backend kann Token rotieren

## 🔧 Technologie-Stack

### Frontend
| Technologie | Version | Zweck |
|-------------|---------|-------|
| Angular | 21 | SPA Framework |
| TypeScript | 5.7+ | Typsicheres JavaScript |
| RxJS | 7.8+ | Reactive Programming |
| Signals | Angular 21 | Reactive State Management |

### BFF (Backend-for-Frontend)
| Technologie | Version | Zweck |
|-------------|---------|-------|
| ASP.NET Core | 10.0 | Web Framework |
| C# | 14 | Programmiersprache |
| IdentityModel | 7.0 | OAuth/OIDC Helper Library |
| Session Middleware | 10.0 | PKCE State Management |

### Backend API
| Technologie | Version | Zweck |
|-------------|---------|-------|
| ASP.NET Core | 10.0 | Web API Framework |
| JWT Bearer Auth | 10.0 | Token Validation |

### Identity Provider
| Technologie | Version | Zweck |
|-------------|---------|-------|
| Keycloak | 26.0 | OAuth/OIDC Server |
| Docker | - | Containerisierung |

## 🛡️ Security Features

### BFF Pattern Vorteile
- ✅ **Zero Token Exposure**: Tokens verlassen nie das Backend
- ✅ **HTTP-only Cookies**: Nicht mit JavaScript zugreifbar
- ✅ **PKCE mit S256**: Auch für confidential clients
- ✅ **State Parameter**: CSRF-Schutz im OAuth Flow
- ✅ **SameSite Cookies**: Zusätzlicher CSRF-Schutz
- ✅ **Token Revocation**: Proper Logout

### Frontend Security
- ✅ **Keine Token-Speicherung**: Niemals im LocalStorage/SessionStorage
- ✅ **CORS mit Credentials**: `withCredentials: true`
- ✅ **Route Guards**: `AuthGuard` für geschützte Bereiche
- ✅ **Keine direkten OAuth-Calls**: Alles über BFF

### Backend Security
- ✅ **JWT Validation**: RSA256 mit Keycloak Public Keys
- ✅ **Role-based Authorization**: `HasScopeHandler`
- ✅ **CORS Policy**: Restriktiv konfiguriert
- ✅ **Secure Cookies**: Production-ready

## 💻 Frontend Integration

### AuthService Verwendung

```typescript
// Login
authService.login();
// → Redirect zu http://localhost:5000/auth/login

// Logout
await authService.logout();
// → POST zu /auth/logout, dann Redirect

// Status prüfen
if (authService.isAuthenticated()) {
  // User ist eingeloggt
}
```

### API Calls mit BFF

```typescript
// WICHTIG: withCredentials: true ist essential!
http.get('http://localhost:5000/api/users', { 
  withCredentials: true 
})
```

Der BFF:
1. Empfängt Request mit Cookie
2. Extrahiert Access Token
3. Fügt `Authorization: Bearer <token>` hinzu
4. Leitet zu API weiter

### Route Protection

```typescript
// In app.routes.ts
{
  path: 'user',
  component: UserComponent,
  canActivate: [authGuard]  // Schützt Route
}
```

## 🔧 Troubleshooting

### Problem: "Unauthorized" bei API-Calls

**Ursache:** Cookie wird nicht mitgesendet

**Lösung:**
```typescript
// IMMER withCredentials: true verwenden!
http.get(url, { withCredentials: true })
```

### Problem: Cookie wird nicht gesetzt nach Login

**Ursache:** CORS oder Cookie-Settings

**Lösung:**
1. Prüfe CORS im BFF (`Program.cs`):
   ```csharp
   .WithOrigins("http://localhost:4200")
   .AllowCredentials();
   ```
2. Prüfe Cookie-Options:
   ```csharp
   HttpOnly = true,
   Secure = false, // true nur mit HTTPS
   SameSite = SameSiteMode.Lax
   ```

### Problem: State mismatch bei Callback

**Ursache:** Session verloren oder CSRF-Angriff

**Lösung:**
- Sessions müssen aktiviert sein (`app.UseSession()`)
- Distributed Cache für Multi-Instance Deployments

### Problem: Token Exchange schlägt fehl

**Ursache:** Keycloak Config oder PKCE-Problem

**Lösung:**
1. Prüfe `realm-export.json`:
   - `redirectUris` muss `http://localhost:5000/auth/callback` enthalten
   - `pkce.code.challenge.method` muss `S256` sein
2. Prüfe Keycloak Logs: `docker-compose logs keycloak`

## 📝 Entwicklung

### Hot Reload aktivieren

```bash
# BFF mit Watch Mode
cd backend-for-frontend
dotnet watch run

# API mit Watch Mode
cd api
dotnet watch run --urls "http://localhost:5001"

# Frontend
cd client
npm start  # Hat bereits Hot Reload
```

### Logging aktivieren

**BFF Logs:**
```json
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "BackendForFrontend": "Debug",
      "BackendForFrontend.Services": "Trace"
    }
  }
}
```

### Testing

```bash
# Frontend Tests
cd client
npm test

# Backend Tests
cd api
dotnet test

# BFF Tests
cd backend-for-frontend
dotnet test
```

## 🚀 Production Deployment

### Checkliste

- [ ] **HTTPS aktivieren**
  ```csharp
  app.UseHttpsRedirection();
  // Cookie mit Secure = true
  ```

- [ ] **Secrets externalisieren**
  ```bash
  # Azure Key Vault, AWS Secrets Manager, etc.
  dotnet user-secrets set "OAuth:ClientSecret" "..."
  ```

- [ ] **CORS restriktiv**
  ```csharp
  .WithOrigins("https://yourapp.com") // Keine Wildcards!
  ```

- [ ] **Rate Limiting**
  ```csharp
  builder.Services.AddRateLimiter(...);
  ```

- [ ] **Distributed Session Store**
  ```csharp
  // Redis, SQL Server, etc.
  builder.Services.AddStackExchangeRedisCache(...);
  ```

- [ ] **Logging & Monitoring**
  ```csharp
  builder.Services.AddApplicationInsightsTelemetry();
  ```

## 📚 Weitere Dokumentation

- [BFF Detailed README](./backend-for-frontend/README.md)
- [Frontend Integration Guide](./client/BFF-INTEGRATION.md)
- [Keycloak Setup](./keycloak/README.md)

## 🤝 Best Practices

1. ✅ **Niemals Tokens im Frontend speichern**
2. ✅ **Immer PKCE verwenden** (auch bei confidential clients)
3. ✅ **State Parameter validieren** (CSRF-Schutz)
4. ✅ **HTTP-only Cookies** für Token Storage
5. ✅ **Token Revocation** beim Logout implementieren
6. ✅ **HTTPS in Production** (Secure Cookies)
7. ✅ **Minimale Token Lifetime** mit Refresh
8. ✅ **Scope-based Authorization** in API

## 📖 Weiterführende Links

- [OAuth 2.0 BFF Pattern](https://datatracker.ietf.org/doc/html/draft-ietf-oauth-browser-based-apps)
- [PKCE RFC 7636](https://datatracker.ietf.org/doc/html/rfc7636)
- [Keycloak Documentation](https://www.keycloak.org/documentation)
- [ASP.NET Core Security](https://learn.microsoft.com/aspnet/core/security/)
- [Angular Security Guide](https://angular.dev/best-practices/security)
