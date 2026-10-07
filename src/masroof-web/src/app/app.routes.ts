import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'capture' },
  {
    path: 'capture',
    canActivate: [authGuard],
    loadComponent: () => import('./features/capture/capture').then((m) => m.Capture),
    title: 'Masroof · Capture',
  },
  {
    path: 'ledger',
    canActivate: [authGuard],
    loadComponent: () => import('./features/ledger/ledger').then((m) => m.Ledger),
    title: 'Masroof · Ledger',
  },
  {
    path: 'insights',
    canActivate: [authGuard],
    loadComponent: () => import('./features/insights/insights').then((m) => m.Insights),
    title: 'Masroof · Insights',
  },
  {
    path: 'ask',
    canActivate: [authGuard],
    loadComponent: () => import('./features/ask/ask').then((m) => m.Ask),
    title: 'Masroof · Ask',
  },
  {
    path: 'rules',
    canActivate: [authGuard],
    loadComponent: () => import('./features/rules/rules').then((m) => m.Rules),
    title: 'Masroof · Rules',
  },
  { path: '**', redirectTo: 'capture' },
];
