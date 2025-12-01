import { Routes } from '@angular/router';
import { authGuard } from './auth/auth.guard';
import { About } from './about/about';
import { Profile } from './profile/profile';
import { User } from './user/user';
import { CreateUser } from './create-user/create-user';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/about',
    pathMatch: 'full',
  },
  {
    path: 'about',
    component: About,
  },
  {
    path: 'profile',
    component: Profile,
    canActivate: [authGuard],
  },
  {
    path: 'user',
    component: User,
    canActivate: [authGuard],
    data: { roles: ['user:read'] },
  },
  {
    path: 'create-user',
    component: CreateUser,
    canActivate: [authGuard],
    data: { roles: ['user:write'] },
  },
];
