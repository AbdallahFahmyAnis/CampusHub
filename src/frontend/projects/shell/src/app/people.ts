/** SDD CH-S02 / MDP-13 — invites and people. /people */
import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { SessionService } from './session';

interface CampusMember {
  id: string;
  email: string;
  displayName: string;
  roles: string[];
}

interface PendingInvite {
  email: string;
  displayName: string;
  role: string;
  token: string;
  expiresAt: string;
}

interface CampusPeople {
  tenantName: string;
  plan: string;
  seatCap: number;
  seatsUsed: number;
  members: CampusMember[];
  invites: PendingInvite[];
}

@Component({
  selector: 'app-people',
  imports: [FormsModule, RouterLink, DatePipe],
  template: `
    <div class="page-head">
      <div>
        <h1>People</h1>
        <p class="page-kicker">Invite teachers and students to {{ people()?.tenantName || 'this campus' }}.</p>
      </div>
    </div>
    @if (error()) {
      <p class="error">{{ error() }}</p>
    }
    @if (message()) {
      <p class="notice">{{ message() }}</p>
    }
    @if (inviteUrl()) {
      <p class="notice">Invite link: <a [href]="inviteUrl()">{{ inviteUrl() }}</a></p>
    }
    @if (people(); as campus) {
      <section class="panel profile-panel">
        <p class="card-kicker">{{ campus.plan }} plan</p>
        <h2>{{ campus.tenantName }}</h2>
        <p class="muted">{{ campus.seatsUsed }} / {{ campus.seatCap === 2147483647 ? 'unlimited' : campus.seatCap }} student seats</p>
      </section>
      <form class="form stacked" (submit)="invite($event)">
        <h2>Invite someone</h2>
        <label>Name
          <input name="displayName" [(ngModel)]="displayName" required placeholder="Jordan Lee" />
        </label>
        <label>Email
          <input name="email" type="email" [(ngModel)]="email" required />
        </label>
        <label>Role
          <select name="role" [(ngModel)]="role">
            <option value="Student">Student</option>
            <option value="Teacher">Teacher</option>
            <option value="Administrator">Administrator</option>
          </select>
        </label>
        <p class="muted">They open the invite link, set a password, then sign in. No email is sent in this demo.</p>
        <button class="btn" type="submit" [disabled]="busy()">Send invite</button>
      </form>
      <section class="panel">
        <h2>Members</h2>
        @for (member of campus.members; track member.id) {
          <p>{{ member.displayName }} · {{ member.email }} · {{ member.roles.join(', ') }}</p>
        } @empty {
          <p class="muted">No members yet.</p>
        }
      </section>
      @if (campus.invites.length) {
        <section class="panel">
          <h2>Pending invites</h2>
          @for (item of campus.invites; track item.token) {
            <p>
              {{ item.displayName }} · {{ item.email }} · {{ item.role }}
              · expires {{ item.expiresAt | date: 'mediumDate' }}
              · <a [href]="linkFor(item.token)">Copy link</a>
            </p>
          }
        </section>
      }
    }
  `,
})
export class PeoplePage {
  private readonly http = inject(HttpClient);
  readonly session = inject(SessionService);
  readonly people = signal<CampusPeople | null>(null);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly inviteUrl = signal<string | null>(null);
  readonly busy = signal(false);
  displayName = '';
  email = '';
  role = 'Student';

  constructor() {
    void this.load();
  }

  linkFor(token: string): string {
    return `${window.location.origin}/invite/${token}`;
  }

  async invite(event: Event): Promise<void> {
    event.preventDefault();
    this.busy.set(true);
    this.error.set(null);
    this.message.set(null);
    this.inviteUrl.set(null);
    try {
      const created = await firstValueFrom(this.http.post<{ inviteUrl: string }>(
        '/api/campus/invites',
        { email: this.email.trim(), displayName: this.displayName.trim(), role: this.role },
      ));
      this.inviteUrl.set(created.inviteUrl);
      this.message.set(`Invite created for ${this.email.trim()}. Share the link — email is not sent in this demo.`);
      this.email = '';
      this.displayName = '';
      await this.load();
    } catch (err: unknown) {
      const message = (err as { error?: { error?: string } })?.error?.error;
      this.error.set(message || 'Could not create the invite.');
    } finally {
      this.busy.set(false);
    }
  }

  private async load(): Promise<void> {
    try {
      const campus = await firstValueFrom(this.http.get<CampusPeople>('/api/campus/members'));
      this.people.set(campus);
    } catch (err: unknown) {
      const message = (err as { error?: { error?: string } })?.error?.error;
      this.error.set(message || 'Could not load campus members.');
    }
  }
}
