/** SDD CH-S01 / MDP-12 — campus signup. /signup */
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-signup',
  imports: [FormsModule, RouterLink],
  template: `
    <div class="page-head">
      <div>
        <h1>Create a campus</h1>
        <p class="page-kicker">Start a Free campus for your school. You can sign in after the campus is created.</p>
      </div>
    </div>
    @if (error()) {
      <p class="error">{{ error() }}</p>
    }
    <section class="panel profile-panel">
      <form class="form stacked" (submit)="submit($event)">
        <label>Campus name
          <input name="campusName" [(ngModel)]="campusName" required minlength="3" placeholder="Northridge High" />
        </label>
        <label>Your name
          <input name="displayName" [(ngModel)]="displayName" required placeholder="Alex Rivera" />
        </label>
        <label>Email
          <input name="email" type="email" [(ngModel)]="email" required />
        </label>
        <label>Password
          <input name="password" type="password" [(ngModel)]="password" required minlength="8" />
        </label>
        <p class="muted">Free plan: 25 seats. Ask AI uses course materials only.</p>
        <div class="actions">
          <button class="btn" type="submit" [disabled]="busy()">Create campus</button>
          <a class="btn secondary" routerLink="/catalog">Cancel</a>
        </div>
      </form>
    </section>
  `,
})
export class SignupPage {
  private readonly http = inject(HttpClient);
  readonly error = signal<string | null>(null);
  readonly busy = signal(false);
  campusName = '';
  displayName = '';
  email = '';
  password = '';

  async submit(event: Event): Promise<void> {
    event.preventDefault();
    this.busy.set(true);
    this.error.set(null);
    try {
      await firstValueFrom(this.http.post('/api/tenants/register', {
        campusName: this.campusName.trim(),
        displayName: this.displayName.trim(),
        email: this.email.trim(),
        password: this.password,
      }));
      window.location.href = `/login?returnUrl=${encodeURIComponent('/catalog')}`;
    } catch (err: unknown) {
      const message = (err as { error?: { error?: string } })?.error?.error;
      this.error.set(message || 'Could not create the campus. Try a different email.');
      this.busy.set(false);
    }
  }
}
