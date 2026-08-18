import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { SessionService } from './session';

interface Profile {
  id: string;
  email: string;
  displayName: string;
  roles: string[];
}

@Component({
  selector: 'app-profile',
  imports: [FormsModule, RouterLink],
  template: `
    <div class="page-head">
      <div>
        <h1>Your profile</h1>
        <p class="page-kicker">Update how your name appears on CampusHub, and change your password.</p>
      </div>
    </div>
    @if (error()) {
      <p class="error">{{ error() }}</p>
    }
    @if (message()) {
      <p class="notice">{{ message() }}</p>
    }
    @if (profile(); as item) {
      <section class="panel profile-panel">
        <p class="card-kicker">{{ session.roleLabel() }}</p>
        <h2>{{ item.displayName }}</h2>
        <p class="muted">{{ item.email }}</p>
        @if (session.session().tenantName) {
          <p class="muted">{{ session.session().tenantName }} · {{ session.session().plan }} plan</p>
        }
        <p class="muted">Signed in as {{ item.roles.join(', ') || 'Student' }}</p>
        <div class="actions">
          <a class="btn secondary" routerLink="/learn/inbox">Inbox</a>
          <a class="btn secondary" routerLink="/enroll">Enrollments</a>
          <a class="btn secondary" routerLink="/learn">Course pass</a>
        </div>
      </section>
      <form class="form" (submit)="saveName($event)">
        <h2>Display name</h2>
        <label>
          Name
          <input name="displayName" [(ngModel)]="displayName" required maxlength="200" />
        </label>
        <button class="btn" type="submit" [disabled]="saving()">Save name</button>
      </form>
      <form class="form" (submit)="savePassword($event)">
        <h2>Password</h2>
        <label>
          Current password
          <input name="currentPassword" type="password" [(ngModel)]="currentPassword" required />
        </label>
        <label>
          New password
          <input name="newPassword" type="password" [(ngModel)]="newPassword" required />
        </label>
        <p class="muted">At least 10 characters, with a digit, uppercase letter, and symbol.</p>
        <button class="btn secondary" type="submit" [disabled]="saving()">Change password</button>
      </form>
    }
  `,
})
export class ProfilePage {
  private readonly http = inject(HttpClient);
  readonly session = inject(SessionService);
  readonly profile = signal<Profile | null>(null);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly saving = signal(false);
  displayName = '';
  currentPassword = '';
  newPassword = '';

  constructor() {
    void this.load();
  }

  private async load(): Promise<void> {
    try {
      const profile = await firstValueFrom(this.http.get<Profile>('/api/account/me'));
      this.profile.set(profile);
      this.displayName = profile.displayName;
    } catch {
      this.error.set('Could not load your profile.');
    }
  }

  async saveName(event: Event): Promise<void> {
    event.preventDefault();
    this.saving.set(true);
    this.error.set(null);
    this.message.set(null);
    try {
      await firstValueFrom(this.http.put('/api/account/me', { displayName: this.displayName.trim() }));
      this.session.patchName(this.displayName.trim());
      this.message.set('Display name saved. Sign out and in again if the top bar still shows the old token name.');
      await this.load();
    } catch {
      this.error.set('Could not save your name.');
    } finally {
      this.saving.set(false);
    }
  }

  async savePassword(event: Event): Promise<void> {
    event.preventDefault();
    this.saving.set(true);
    this.error.set(null);
    this.message.set(null);
    try {
      await firstValueFrom(this.http.post('/api/account/password', {
        currentPassword: this.currentPassword,
        newPassword: this.newPassword,
      }));
      this.currentPassword = '';
      this.newPassword = '';
      this.message.set('Password changed. Use it the next time you sign in.');
    } catch {
      this.error.set('Could not change the password. Check the current password and the new password rules.');
    } finally {
      this.saving.set(false);
    }
  }
}
