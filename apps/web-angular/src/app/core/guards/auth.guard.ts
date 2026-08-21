import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
export const authGuard: CanActivateFn = (_, state) => inject(AuthService).isAuthenticated() || inject(Router).createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
