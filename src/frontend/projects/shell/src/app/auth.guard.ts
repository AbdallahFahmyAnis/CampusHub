import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { SessionService } from './session';

export const authGuard: CanActivateFn = async () => {
  const session = inject(SessionService);
  const current = session.session().authenticated ? session.session() : await session.load();
  if (!current.authenticated) {
    session.login();
    return false;
  }
  return true;
};

export const teacherGuard: CanActivateFn = async () => {
  const session = inject(SessionService);
  if (!session.session().authenticated) {
    await session.load();
  }
  if (!session.session().authenticated) {
    session.login();
    return false;
  }
  return session.isTeacher();
};

export const adminGuard: CanActivateFn = async () => {
  const session = inject(SessionService);
  const router = inject(Router);
  if (!session.session().authenticated) {
    await session.load();
  }
  if (!session.session().authenticated) {
    session.login();
    return false;
  }
  return session.isAdmin() ? true : router.parseUrl('/catalog');
};
