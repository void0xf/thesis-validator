import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'validate',
    children: [],
  },
  {
    path: 'results',
    children: [],
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'validate',
  },
  {
    path: '**',
    redirectTo: 'validate',
  },
];
