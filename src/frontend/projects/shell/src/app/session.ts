import { Injectable, computed, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export interface Session {
  authenticated: boolean;
  name: string | null;
  email: string | null;
  sub: string | null;
  roles: string[];
  tenantId: string | null;
  tenantName: string | null;
  plan: string | null;
}

const anonymousSession: Session = {
  authenticated: false,
  name: null,
  email: null,
  sub: null,
  roles: [],
  tenantId: null,
  tenantName: null,
  plan: null,
};

@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly state = signal<Session>(anonymousSession);

  readonly session = this.state.asReadonly();
  readonly isTeacher = computed(() => {
    const roles = this.state().roles;
    return roles.includes('Teacher') || roles.includes('Administrator');
  });
  readonly isAdmin = computed(() => this.state().roles.includes('Administrator'));
  readonly roleLabel = computed(() => {
    if (!this.state().authenticated) {
      return null;
    }
    if (this.isAdmin()) {
      return 'Administrator';
    }
    if (this.isTeacher()) {
      return 'Teacher';
    }
    return 'Student';
  });

  constructor(private readonly http: HttpClient) {}

  async load(): Promise<Session> {
    try {
      const session = await firstValueFrom(this.http.get<Session>('/whoami'));
      this.state.set(session);
      return session;
    } catch {
      const anonymous = { ...anonymousSession };
      this.state.set(anonymous);
      return anonymous;
    }
  }

  login(): void {
    const returnUrl = window.location.pathname + window.location.search;
    window.location.href = `/login?returnUrl=${encodeURIComponent(returnUrl || '/catalog')}`;
  }

  logout(): void {
    window.location.assign('/logout');
  }

  patchName(name: string): void {
    this.state.update((current) => ({ ...current, name }));
  }
}
