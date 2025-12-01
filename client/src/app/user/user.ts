import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { UserApiService, User as UserData } from '../services/user-api.service';

@Component({
  selector: 'app-user',
  imports: [],
  templateUrl: './user.html',
  styleUrl: './user.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class User implements OnInit {
  private readonly userApiService = inject(UserApiService);
  private readonly router = inject(Router);

  protected readonly users = signal<UserData[]>([]);
  protected readonly isLoading = signal(true);
  protected readonly errorMessage = signal('');
  protected readonly hasError = computed(() => this.errorMessage() !== '');
  protected readonly showUsersList = computed(() => !this.isLoading() && !this.hasError());
  protected readonly hasUsers = computed(() => this.users().length > 0);

  ngOnInit() {
    this.loadUsers();
  }

  loadUsers() {
    this.isLoading.set(true);
    this.errorMessage.set('');

    this.userApiService.getUsers()
      .subscribe({
        next: (data) => {
          this.users.set(data);
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Fehler beim Laden der Nutzer:', error);
          this.errorMessage.set('Fehler beim Laden der Nutzer. Bitte versuche es später erneut.');
          this.isLoading.set(false);
        }
      });
  }

  navigateToCreateUser() {
    this.router.navigate(['/create-user']);
  }

  viewUserDetails(userId: number) {
    console.log('Nutzer Details für ID:', userId);
    // Hier kannst du später eine Detail-View implementieren
  }

  trackByUserId(_index: number, user: UserData): number {
    return user.id;
  }
}
