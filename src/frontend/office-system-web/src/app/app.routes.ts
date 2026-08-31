import { Routes } from '@angular/router';
import { requireNoSession, requireSession } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'sign-in',
    canActivate: [requireNoSession],
    title: 'Sign in · Office Days',
    loadComponent: () => import('./features/auth/sign-in.page').then((m) => m.SignInPage),
  },
  {
    path: 'sign-up',
    canActivate: [requireNoSession],
    title: 'Create an account · Office Days',
    loadComponent: () => import('./features/auth/sign-up.page').then((m) => m.SignUpPage),
  },
  {
    path: '',
    canActivate: [requireSession],
    loadComponent: () => import('./layout/shell').then((m) => m.Shell),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'schedule' },
      {
        path: 'schedule',
        title: 'Team week · Office Days',
        loadComponent: () => import('./features/schedule/schedule.page').then((m) => m.SchedulePage),
      },
      {
        path: 'my-month',
        title: 'My month · Office Days',
        loadComponent: () => import('./features/schedule/my-month.page').then((m) => m.MyMonthPage),
      },
      {
        path: 'teams',
        title: 'Your teams · Office Days',
        loadComponent: () => import('./features/teams/teams.page').then((m) => m.TeamsPage),
      },
      {
        path: 'team',
        title: 'Team · Office Days',
        loadComponent: () => import('./features/teams/team-detail.page').then((m) => m.TeamDetailPage),
      },
      {
        path: 'profile',
        title: 'Your profile · Office Days',
        loadComponent: () => import('./features/profile/profile.page').then((m) => m.ProfilePage),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
