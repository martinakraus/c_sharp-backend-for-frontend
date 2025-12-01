# Keycloak Setup

Dieser Ordner enthält die Keycloak-Konfiguration für das Backend-for-Frontend Beispiel.

## Keycloak Admin-Zugang

- **URL**: http://localhost:8080
- **Admin-Benutzername**: `admin`
- **Admin-Passwort**: `admin`

## Realm: bff-realm

### Client-Konfiguration

**Client ID**: `bff-client`
- **Typ**: Confidential Client
- **Flow**: Authorization Code Flow mit PKCE (S256)
- **Client Secret**: Wird automatisch generiert (siehe Keycloak Admin Console)
- **Redirect URIs**: 
  - `http://localhost:4000/*`
  - `http://localhost:3000/*`
- **Web Origins**: 
  - `http://localhost:4000`
  - `http://localhost:3000`

### Rollen

Das Realm definiert zwei Rollen für die Benutzerverwaltung:

1. **user:read** - Lesezugriff auf Benutzerressourcen
2. **user:write** - Schreibzugriff auf Benutzerressourcen

### Test-Benutzer

Es werden zwei Benutzer vorkonfiguriert:

#### 1. testuser (volle Rechte)
- **Benutzername**: `testuser`
- **Passwort**: `password`
- **E-Mail**: testuser@example.com
- **Rollen**: `user:read`, `user:write`

#### 2. readonly (nur Lesezugriff)
- **Benutzername**: `readonly`
- **Passwort**: `password`
- **E-Mail**: readonly@example.com
- **Rollen**: `user:read`

## PKCE Flow

Der Client ist als **Confidential Client** konfiguriert und erfordert PKCE mit der S256 Challenge-Methode. Dies bietet zusätzliche Sicherheit für den Authorization Code Flow.

### PKCE-Parameter:
- **Code Challenge Method**: S256 (SHA-256)
- **Erforderlich**: Ja, für alle Authorization Requests

## Integration

### OpenID Connect Endpoints

Nach dem Start von Keycloak sind die OIDC-Endpoints unter folgender URL verfügbar:

```
http://localhost:8080/realms/bff-realm/.well-known/openid-configuration
```

### Wichtige Endpoints:

- **Authorization**: `http://localhost:8080/realms/bff-realm/protocol/openid-connect/auth`
- **Token**: `http://localhost:8080/realms/bff-realm/protocol/openid-connect/token`
- **UserInfo**: `http://localhost:8080/realms/bff-realm/protocol/openid-connect/userinfo`
- **Logout**: `http://localhost:8080/realms/bff-realm/protocol/openid-connect/logout`

## Client Secret abrufen

Das Client Secret kann in der Keycloak Admin Console abgerufen werden:

1. Öffne http://localhost:8080
2. Melde dich mit `admin` / `admin` an
3. Wähle das Realm `bff-realm`
4. Gehe zu **Clients** → **bff-client**
5. Öffne den Tab **Credentials**
6. Kopiere das **Client Secret**

## Token-Konfiguration

- **Access Token Lifespan**: 5 Minuten (300 Sekunden)
- **Refresh Token**: Aktiviert
- **Token Format**: JWT

## Verwendung im Code

### C# API Integration

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "http://localhost:8080/realms/bff-realm";
        options.Audience = "bff-client";
        options.RequireHttpsMetadata = false; // Nur für Entwicklung!
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("UserRead", policy => 
        policy.RequireRole("user:read"));
    options.AddPolicy("UserWrite", policy => 
        policy.RequireRole("user:write"));
});
```

### Angular Integration

```typescript
export const authConfig: AuthConfig = {
  issuer: 'http://localhost:8080/realms/bff-realm',
  redirectUri: window.location.origin,
  clientId: 'bff-client',
  responseType: 'code',
  scope: 'openid profile email',
  showDebugInformation: true,
  strictDiscoveryDocumentValidation: false,
  requireHttps: false, // Nur für Entwicklung!
  usePkce: true // PKCE aktivieren
};
```

## Hinweise

- Diese Konfiguration ist **nur für Entwicklungszwecke** geeignet
- In Produktion sollte HTTPS aktiviert sein (`sslRequired: "external"`)
- Die Passwörter sollten in Produktion sicher und komplex sein
- Das Client Secret sollte als Umgebungsvariable verwaltet werden
