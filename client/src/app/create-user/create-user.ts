import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { UserApiService } from '../services/user-api.service';

@Component({
  selector: 'app-create-user',
  imports: [ReactiveFormsModule],
  templateUrl: './create-user.html',
  styleUrl: './create-user.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CreateUser {
  private readonly userApiService = inject(UserApiService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  protected readonly errorMessage = signal('');
  protected readonly isSubmitting = signal(false);

  protected readonly userForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
  });

  createUser() {
    this.errorMessage.set('');
    
    if (this.userForm.invalid) {
      this.errorMessage.set('Bitte fülle alle Felder korrekt aus.');
      this.userForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);

    this.userApiService.createUser(this.userForm.getRawValue())
      .subscribe({
        next: () => {
          this.router.navigate(['/user']);
        },
        error: (error) => {
          console.error('Fehler beim Erstellen des Nutzers:', error);
          this.errorMessage.set(error.error?.Message || 'Fehler beim Erstellen des Nutzers');
          this.isSubmitting.set(false);
        }
      });
  }

  cancel() {
    this.router.navigate(['/user']);
  }
}
