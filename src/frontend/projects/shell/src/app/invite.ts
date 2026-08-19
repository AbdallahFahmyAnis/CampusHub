/** SDD CH-S02 invite register + CH-S17 login after accept. /invite/:token */
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

interface OpenInvite {
  email: string;
  displayName: string;
  role: string;
  campusName: string;
  expiresAt: string;
}

@Component({
  selector: 'app-invite',
  imports: [FormsModule, RouterLink],
  template: `
    <div class="page-head">
      <div>
        <h1>Join {{ invite()?.campusName || 'a campus' }}</h1>
        <p class="page-kicker">Set a password to accept this invite, then sign in.</p>
      </div>
    </div>
    @if (error()) {
      <p class="error">{{ error() }}</p>
    }
    @if (invite(); as item) {
      <section class="panel profile-panel">
        <p class="card-kicker">{{ item.role }}</p>
        <h2>{{ item.displayName }}</h2>
        <p class="muted">{{ item.email }}</p>
        <form class="form stacked" (submit)="accept($event)">
          <label>Password
            <input name="password" type="password" [(ngModel)]="password" required minlength="10" />
          </label>
          <p class="muted">At least 10 characters, with a digit, uppercase letter, and symbol.</p>
          <div class="actions">
            <button class="btn" type="submit" [disabled]="busy()">Join campus</button>
            <a class="btn secondary" routerLink="/signup">Create a campus instead</a>
          </div>
        </form>
      </section>
    }
  `,
})
export class InvitePage {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  readonly invite = signal<OpenInvite | null>(null);
  readonly error = signal<string | null>(null);
  readonly busy = signal(false);
  password = '';

  constructor() {
    const token = this.route.snapshot.paramMap.get('token');
    if (!token) {
      this.error.set('This invite is invalid or has expired.');
      return;
    }
    void this.load(token);
  }

  async accept(event: Event): Promise<void> {
    event.preventDefault();
    const token = this.route.snapshot.paramMap.get('token');
    if (!token) {
      return;
    }
    this.busy.set(true);
    this.error.set(null);
    try {
      await firstValueFrom(this.http.post(`/api/invites/${encodeURIComponent(token)}/accept`, {
        password: this.password,
        displayName: this.invite()?.displayName,
      }));
      window.location.href = `/login?returnUrl=${encodeURIComponent('/catalog')}`;
    } catch (err: unknown) {
      const message = (err as { error?: { error?: string } })?.error?.error;
      this.error.set(message || 'Could not accept the invite.');
      this.busy.set(false);
    }
  }

  private async load(token: string): Promise<void> {
    try {
      const invite = await firstValueFrom(this.http.get<OpenInvite>(`/api/invites/${encodeURIComponent(token)}`));
      this.invite.set(invite);
    } catch {
      this.error.set('This invite is invalid or has expired.');
    }
  }
}
