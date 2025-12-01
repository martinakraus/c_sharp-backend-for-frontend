# Angular Frontend mit BFF Integration

Dieses Angular Frontend ist für die Verwendung mit dem C# Backend-for-Frontend (BFF) konfiguriert.

## Architektur

Das Frontend kommuniziert **ausschließlich** mit dem BFF. Es hat **keinen direkten Zugriff** auf OAuth-Tokens:

```
┌─────────────┐      ┌──────────────┐      ┌──────────────┐      ┌──────────┐
│   Angular   │ ───> │  C# BFF      │ ───> │  Keycloak    │      │   API    │
│   Frontend  │ <─── │  (Port 5000) │ <─── │  (Port 8080) │      │(Port 5001)│
└─────────────┘      └──────────────┘      └──────────────┘      └──────────┘
     HTTP              HTTP + Cookies        OAuth/OIDC          Bearer Token
```

## Wichtige Komponenten

### AuthService (`auth/auth.service.ts`)
- **`init()`**: Prüft beim App-Start den Auth-Status via BFF
- **`login()`**: Redirected zu `/auth/login` am BFF
- **`logout()`**: Ruft `/auth/logout` am BFF auf
- **`isAuthenticated()`**: Gibt aktuellen Auth-Status zurück
- Nutzt **Signals** für reaktive Updates

### UserApiService (`services/user-api.service.ts`)
- Alle API-Calls gehen durch den BFF: `http://localhost:5000/api/*`
- **`withCredentials: true`** sorgt dafür, dass Cookies mitgesendet werden
- BFF extrahiert Access Token aus Cookie und fügt als Bearer Token hinzu

### AuthGuard (`auth/auth.guard.ts`)
- Schützt Routes vor unauthentifizierten Zugriffen
- Redirected zu `authService.login()` wenn nicht eingeloggt
- Unterstützt Role-basierte Zugriffskontrolle

## Authentifizierungs-Flow

### 1. Login
```typescript
// User klickt auf Login Button
authService.login();

// → Redirect zu http://localhost:5000/auth/login
// → BFF startet PKCE Flow mit Keycloak
// → User authentifiziert sich bei Keycloak
// → Keycloak redirected zu /auth/callback
// → BFF tauscht Code gegen Token
// → BFF setzt Access Token in HTTP-only Cookie
// → Redirect zurück zu Angular App (/)
```

### 2. API Calls
```typescript
// Angular macht Request
http.get('http://localhost:5000/api/users', { withCredentials: true })

// → Browser sendet Cookie automatisch mit
// → BFF extrahiert Access Token aus Cookie
// → BFF fügt Authorization: Bearer <token> hinzu
// → BFF leitet Request an API weiter
// → Response wird zurück an Angular gesendet
```

### 3. Logout
```typescript
// User klickt auf Logout
authService.logout();

// → POST zu http://localhost:5000/auth/logout
// → BFF revoked Token bei Keycloak
// → BFF löscht Cookie
// → Redirect zu /
```

## Konfiguration

### Environment Files
```typescript
// src/environments/environment.ts
export const environment = {
  production: false,
  bffUrl: 'http://localhost:5000'
};
```

### CORS & Credentials
Alle HTTP-Requests müssen mit `withCredentials: true` erfolgen:
```typescript
this.http.get(url, { withCredentials: true })
```

Dies stellt sicher, dass:
- Cookies automatisch mitgesendet werden
- CORS Pre-flight Requests korrekt gehandhabt werden

## Installation & Start

```bash
# Dependencies installieren
npm install

# Development Server starten
npm start
# oder
ng serve
```

Die App läuft auf `http://localhost:4200`

## Wichtige Hinweise

### ✅ DO's
- Immer `withCredentials: true` bei HTTP-Calls verwenden
- BFF-URL aus Environment-Config verwenden
- Auth-Status über `AuthService` prüfen
- Guards für geschützte Routes verwenden

### ❌ DON'Ts
- **Niemals** OAuth-Tokens im Frontend speichern
- **Niemals** direkt mit Keycloak kommunizieren
- **Niemals** direkt zur API connecten (immer über BFF)
- **Keine** Tokens in LocalStorage/SessionStorage

## Sicherheitsfeatures

1. **Keine Token-Exposition**: Access Tokens verlassen nie das BFF
2. **HTTP-only Cookies**: Nicht mit JavaScript zugreifbar
3. **CORS**: Strict Origin Policy
4. **CSRF Protection**: SameSite Cookie Policy
5. **State Parameter**: CSRF-Schutz im OAuth Flow

## Entwicklung

### Neue API Endpoints hinzufügen
```typescript
// In einem Service
export class MyApiService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.bffUrl}/api/myendpoint`;

  getData(): Observable<MyData[]> {
    return this.http.get<MyData[]>(this.apiUrl, { withCredentials: true });
  }
}
```

### Protected Routes
```typescript
// In app.routes.ts
{
  path: 'protected',
  component: ProtectedComponent,
  canActivate: [authGuard]
}
```

## Testing

### BFF muss laufen
Für die Entwicklung muss das C# BFF laufen:
```bash
cd ../backend-for-frontend
dotnet run
```

### Keycloak muss laufen
```bash
cd ../keycloak
docker-compose up -d
```

### Test Users
- **testuser** / **password** (hat user:read und user:write)
- **readonly** / **password** (hat nur user:read)
