# Angular BFF Integration - Quick Start

## Installation

Das Projekt ist bereits konfiguriert. Du musst nur:

```bash
cd client
npm install
npm start
```

## Verwendung im Code

### 1. Login implementieren

```typescript
import { Component, inject } from '@angular/core';
import { AuthService } from './auth/auth.service';

@Component({
  selector: 'app-header',
  template: `
    @if (authService.isAuthenticated()) {
      <button (click)="logout()">Logout</button>
    } @else {
      <button (click)="login()">Login</button>
    }
  `
})
export class HeaderComponent {
  protected readonly authService = inject(AuthService);

  login() {
    // Redirected zu BFF /auth/login
    this.authService.login();
  }

  async logout() {
    // POST zu BFF /auth/logout
    await this.authService.logout();
  }
}
```

### 2. API Calls über BFF

```typescript
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../environments/environment';

@Injectable({ providedIn: 'root' })
export class UserApiService {
  private readonly http = inject(HttpClient);
  
  getUsers() {
    // WICHTIG: withCredentials: true!
    return this.http.get(
      `${environment.bffUrl}/api/users`,
      { withCredentials: true }
    );
  }

  createUser(user: { name: string; email: string }) {
    return this.http.post(
      `${environment.bffUrl}/api/users`,
      user,
      { withCredentials: true }
    );
  }
}
```

### 3. Routes schützen

```typescript
import { Routes } from '@angular/router';
import { authGuard } from './auth/auth.guard';

export const routes: Routes = [
  {
    path: 'about',
    loadComponent: () => import('./about/about').then(m => m.About)
  },
  {
    path: 'profile',
    loadComponent: () => import('./profile/profile').then(m => m.Profile),
    canActivate: [authGuard]  // 🔒 Geschützt!
  },
  {
    path: 'user',
    loadComponent: () => import('./user/user').then(m => m.User),
    canActivate: [authGuard]  // 🔒 Geschützt!
  }
];
```

### 4. Auth Status reaktiv nutzen

```typescript
import { Component, inject, computed } from '@angular/core';
import { AuthService } from './auth/auth.service';

@Component({
  selector: 'app-root',
  template: `
    <p>Status: {{ authStatusText() }}</p>
  `
})
export class AppComponent {
  private readonly authService = inject(AuthService);
  
  // Signal-basiert - reaktiv!
  authStatusText = computed(() => 
    this.authService.isAuthenticated() 
      ? 'Eingeloggt' 
      : 'Nicht eingeloggt'
  );
}
```

## Wichtige Konzepte

### withCredentials ist Pflicht!

```typescript
// ✅ RICHTIG
http.get(url, { withCredentials: true })

// ❌ FALSCH - Cookie wird nicht mitgesendet!
http.get(url)
```

### Keine Tokens im Frontend

```typescript
// ❌ NIEMALS SO!
localStorage.setItem('access_token', token);
sessionStorage.setItem('access_token', token);

// ✅ Token ist im HTTP-only Cookie
// Frontend kann es nicht lesen oder manipulieren
```

### BFF URL aus Environment

```typescript
// ❌ NICHT hardcoded
const url = 'http://localhost:5000/api/users';

// ✅ Aus Environment Config
import { environment } from '../environments/environment';
const url = `${environment.bffUrl}/api/users`;
```

## Error Handling

### Unauthorized (401)

```typescript
http.get(url, { withCredentials: true }).subscribe({
  next: (data) => console.log(data),
  error: (err) => {
    if (err.status === 401) {
      // User nicht eingeloggt oder Session abgelaufen
      this.authService.login();
    }
  }
});
```

### Global HTTP Interceptor (optional)

```typescript
import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError } from 'rxjs/operators';
import { throwError } from 'rxjs';
import { AuthService } from './auth/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  return next(req).pipe(
    catchError((error) => {
      if (error.status === 401) {
        authService.login();
      }
      return throwError(() => error);
    })
  );
};

// In app.config.ts registrieren:
export const appConfig: ApplicationConfig = {
  providers: [
    provideHttpClient(
      withFetch(),
      withInterceptors([authInterceptor])
    )
  ]
};
```

## Debugging

### Browser DevTools

1. **Network Tab**
   - Prüfe ob `Cookie` Header mitgesendet wird
   - Bei `/auth/login`: Redirect Chain beobachten
   - Bei `/api/*`: Status 200 (nicht 401)

2. **Application Tab**
   - Cookies → `http://localhost:5000`
   - Sollte `access_token` Cookie sehen (Wert ist verborgen)

3. **Console**
   - AuthService Logs beobachten
   - Fehler beim init() oder logout()

### Häufige Probleme

**Problem:** Cookie nicht vorhanden nach Login

```typescript
// Prüfe:
// 1. BFF läuft auf Port 5000?
// 2. Keycloak läuft?
// 3. CORS im BFF korrekt?
```

**Problem:** API Call gibt 401

```typescript
// Prüfe:
// 1. withCredentials: true gesetzt?
// 2. Cookie existiert?
// 3. API validiert JWT korrekt?
```

**Problem:** Login-Loop (redirected immer zu Login)

```typescript
// Prüfe:
// 1. authService.init() wird in APP_INITIALIZER aufgerufen?
// 2. /auth/status Endpoint erreichbar?
// 3. Cookie Domain/Path korrekt?
```

## Testing

### Unit Tests

```typescript
import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { UserApiService } from './user-api.service';
import { provideHttpClient } from '@angular/common/http';

describe('UserApiService', () => {
  let service: UserApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    service = TestBed.inject(UserApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  it('should call BFF with credentials', () => {
    service.getUsers().subscribe();
    
    const req = httpMock.expectOne('http://localhost:5000/api/users');
    expect(req.request.withCredentials).toBe(true);
    
    req.flush([]);
  });
});
```

### E2E Tests (Playwright)

```typescript
import { test, expect } from '@playwright/test';

test('login flow', async ({ page, context }) => {
  // Start at home
  await page.goto('http://localhost:4200');
  
  // Click login
  await page.click('text=Login');
  
  // Should redirect to Keycloak
  await expect(page).toHaveURL(/localhost:8080/);
  
  // Login with test user
  await page.fill('input[name="username"]', 'testuser');
  await page.fill('input[name="password"]', 'password');
  await page.click('input[type="submit"]');
  
  // Should redirect back to app
  await expect(page).toHaveURL('http://localhost:4200/');
  
  // Should have cookie
  const cookies = await context.cookies();
  const accessTokenCookie = cookies.find(c => c.name === 'access_token');
  expect(accessTokenCookie).toBeDefined();
  expect(accessTokenCookie?.httpOnly).toBe(true);
});
```

## Best Practices

1. ✅ Immer `withCredentials: true`
2. ✅ Environment Config verwenden
3. ✅ Signals für reactive Auth State
4. ✅ AuthGuard für geschützte Routes
5. ✅ Error Handling bei 401
6. ✅ Loading States während API Calls
7. ✅ Logout bei kritischen Fehlern

## Weitere Ressourcen

- [AuthService Code](./src/app/auth/auth.service.ts)
- [AuthGuard Code](./src/app/auth/auth.guard.ts)
- [UserApiService Code](./src/app/services/user-api.service.ts)
- [BFF README](../backend-for-frontend/README.md)
