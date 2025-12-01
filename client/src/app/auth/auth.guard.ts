import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/**
 * Auth guard that uses BFF session for authentication
 * 
 * With BFF pattern, we check the session state that was already
 * loaded during app initialization. If not authenticated,
 * we redirect to the BFF login endpoint.
 */
export const authGuard: CanActivateFn = async (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Check if user is authenticated
  if (!authService.isAuthenticated()) {
    // Redirect to BFF login
    authService.login(window.location.origin + state.url);
    return false;
  }

  // Check required roles from route data
  const requiredRoles = route.data['roles'] as string[] | undefined;
  if (requiredRoles && requiredRoles.length > 0) {
    if (!authService.hasAnyRole(requiredRoles)) {
      // User doesn't have required role - redirect to home
      router.navigate(['/']);
      return false;
    }
  }

  return true;
};
