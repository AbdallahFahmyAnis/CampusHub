import { DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { SessionService } from './session';

interface CampusDashboard {
  tenantId: string;
  tenantName: string;
  plan: string;
  isPlatformAdmin: boolean;
  memberCount: number;
  studentSeats: number;
  seatCap: number;
  pendingInvites: number;
  allowsModelAi: boolean;
  allowsChat: boolean;
  monthlyPrice: number;
  nextPlan: string | null;
}

@Component({
  selector: 'app-campus-dashboard',
  imports: [RouterLink, DecimalPipe],
  template: `
    <div class="page-head">
      <div>
        <h1>{{ info()?.tenantName || 'My campus' }}</h1>
        <p class="page-kicker">Campus administration overview.</p>
      </div>
    </div>
    @if (error()) {
      <p class="error">{{ error() }}</p>
    }
    @if (info(); as d) {
      <div class="catalog-filters" style="margin-bottom: 2rem;">
        <div class="profile-panel" style="display:inline-block; padding: 1rem 1.5rem; margin-right: 1rem;">
          <p class="card-kicker">Plan</p>
          <p style="font-weight: 700; font-size: 1.2rem;">{{ d.plan }}</p>
          <p class="muted">{{ d.monthlyPrice | number:'1.0-0' }} USD / month</p>
        </div>
        <div class="profile-panel" style="display:inline-block; padding: 1rem 1.5rem; margin-right: 1rem;">
          <p class="card-kicker">Members</p>
          <p style="font-weight: 700; font-size: 1.2rem;">{{ d.memberCount }}</p>
          <p class="muted">{{ d.pendingInvites }} pending invite{{ d.pendingInvites === 1 ? '' : 's' }}</p>
        </div>
        <div class="profile-panel" style="display:inline-block; padding: 1rem 1.5rem; margin-right: 1rem;">
          <p class="card-kicker">Student seats</p>
          <p style="font-weight: 700; font-size: 1.2rem;">{{ d.studentSeats }} / {{ d.seatCap === 2147483647 ? '∞' : d.seatCap }}</p>
          <p class="muted">{{ d.seatCap === 2147483647 ? 'Unlimited' : (d.seatCap - d.studentSeats) + ' remaining' }}</p>
        </div>
        <div class="profile-panel" style="display:inline-block; padding: 1rem 1.5rem;">
          <p class="card-kicker">Features</p>
          <p class="muted">Ask AI: {{ d.allowsModelAi ? '✓ model' : '✗ catalog text only' }}</p>
          <p class="muted">Live chat: {{ d.allowsChat ? '✓ enabled' : '✗ upgrade required' }}</p>
        </div>
      </div>
      <section class="panel">
        <h2>Quick actions</h2>
        <p class="d-flex flex-wrap gap-2" style="display: flex; gap: 0.75rem; flex-wrap: wrap;">
          <a class="btn" routerLink="/people">People &amp; invites</a>
          <a class="btn secondary" routerLink="/billing">Billing &amp; plans</a>
          <a class="btn secondary" routerLink="/catalog/mine">My courses</a>
          <a class="btn secondary" routerLink="/enroll">Enrollments</a>
          @if (d.nextPlan) {
            <a class="btn secondary" routerLink="/billing">Upgrade to {{ d.nextPlan }}</a>
          }
        </p>
      </section>
      @if (d.isPlatformAdmin) {
        <p class="muted" style="margin-top: 1rem;">
          You are signed in as a platform admin. Use <a href="/ops">Ops</a> for the full platform console.
        </p>
      }
    }
  `,
})
export class CampusDashboardPage {
  private readonly http = inject(HttpClient);
  readonly session = inject(SessionService);
  readonly info = signal<CampusDashboard | null>(null);
  readonly error = signal<string | null>(null);

  constructor() {
    void this.load();
  }

  private async load(): Promise<void> {
    try {
      const d = await firstValueFrom(this.http.get<CampusDashboard>('/api/campus/dashboard'));
      this.info.set(d);
    } catch {
      this.error.set('Could not load campus dashboard.');
    }
  }
}
