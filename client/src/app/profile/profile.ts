import { ChangeDetectionStrategy, Component, inject, computed } from '@angular/core';
import { AuthService } from '../auth/auth.service';

@Component({
  selector: 'app-profile',
  imports: [],
  templateUrl: './profile.html',
  styleUrl: './profile.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Profile {
  // private readonly authService = inject(AuthService);
  
  // /** User profile from BFF session */
  // protected readonly profile = computed(() => {
  //   const user = this.authService.user();
  //   if (!user) return null;
    
  //   return {
  //     username: user.name,
  //     firstName: this.authService.getClaim('given_name') ?? '',
  //     lastName: this.authService.getClaim('family_name') ?? '',
  //     email: this.authService.getClaim('email') ?? '',
  //     emailVerified: this.authService.getClaim('email_verified') === 'True',
  //     roles: this.authService.roles(),
  //   };
  // });
}
