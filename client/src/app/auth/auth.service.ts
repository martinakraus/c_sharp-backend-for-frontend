import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';

/**
 * Authentication status from BFF
 */
export interface AuthStatus {
  authenticated: boolean;
}

/**
 * AuthService - Handles authentication via Backend for Frontend (BFF)
 * 
 * With the BFF pattern, the Angular app NEVER handles OAuth tokens directly.
 * All authentication state is managed via secure HttpOnly cookies.
 */
@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly isAuthenticatedSignal = signal(false);

  /**
   * Initialize auth service by checking session status
   */
  async init(): Promise<void> {
    try {
      const status = await firstValueFrom(
        this.http.get<AuthStatus>(`${environment.bffUrl}/auth/status`, { withCredentials: true })
      );
      this.isAuthenticatedSignal.set(status.authenticated);
    } catch (error) {
      console.error('Failed to check auth status', error);
      this.isAuthenticatedSignal.set(false);
    }
  }

  /**
   * Check if user is authenticated
   */
  isAuthenticated(): boolean {
    return this.isAuthenticatedSignal();
  }

  /**
   * Get authentication signal for reactive updates
   */
  authStatus() {
    return this.isAuthenticatedSignal.asReadonly();
  }

  /**
   * Redirect to BFF login endpoint
   */
  login(returnUrl?: string): void {
    // Redirect to BFF login - it will handle OAuth flow
    window.location.href = `${environment.bffUrl}/auth/login`;
  }

  /**
   * Logout via BFF
   * Redirects to BFF logout endpoint which will handle Keycloak logout
   */
  logout(): void {
    // Direct browser navigation - BFF will redirect to Keycloak logout
    // which will then redirect back to the app
    this.isAuthenticatedSignal.set(false);
    window.location.href = `${environment.bffUrl}/auth/logout`;
  }

  /**
   * Check if user has any of the required roles
   * Note: Role checking would need to be implemented in BFF if needed
   */
  hasAnyRole(roles: string[]): boolean {
    // For now, return true if authenticated
    // In a real app, you'd get roles from the BFF
    return this.isAuthenticated();
  }
}
