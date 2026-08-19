import { DecimalPipe, TitleCasePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { SessionService } from './session';

interface PlanOption {
  id: string;
  name: string;
  monthlyPrice: number;
  seatCap: number;
  allowsModelAi: boolean;
  allowsChat: boolean;
}

interface CampusBilling {
  tenantName: string;
  plan: string;
  seatCap: number;
  monthlyPrice: number;
  allowsModelAi: boolean;
  allowsChat: boolean;
  nextPlan: string | null;
  nextPlanPrice: number | null;
  options: PlanOption[];
}

interface UpgradeResponse {
  upgraded: boolean;
  plan: string;
  message: string;
}

@Component({
  selector: 'app-billing',
  imports: [RouterLink, DecimalPipe, TitleCasePipe],
  template: `
    <div class="page-head">
      <div>
        <h1>Billing</h1>
        <p class="page-kicker">Manage the plan for {{ billing()?.tenantName || session.session().tenantName || 'this campus' }}.</p>
      </div>
    </div>
    @if (error()) {
      <p class="error">{{ error() }}</p>
    }
    @if (message()) {
      <p class="notice">{{ message() }}</p>
    }
    @if (billing(); as info) {
      <section class="panel profile-panel">
        <p class="card-kicker">Current plan</p>
        <h2>{{ info.plan | titlecase }}</h2>
        <p class="muted">
          {{ info.monthlyPrice | number:'1.0-0' }} USD / month ·
          {{ info.seatCap === 2147483647 ? 'unlimited' : info.seatCap }} student seats
        </p>
        <ul class="muted">
          <li>Ask AI model: {{ info.allowsModelAi ? 'enabled' : 'catalog text only' }}</li>
          <li>Live chat: {{ info.allowsChat ? 'enabled' : 'upgrade required' }}</li>
        </ul>
        @if (info.nextPlan) {
          <button class="btn" type="button" [disabled]="busy()" (click)="upgrade()">
            Upgrade to {{ info.nextPlan }} ({{ info.nextPlanPrice | number:'1.0-0' }} USD / mo)
          </button>
          <p class="muted">Demo billing only — no card is charged. Sign in again after upgrading.</p>
        } @else {
          <p class="muted">This campus is on the highest plan.</p>
        }
      </section>
      <section class="panel">
        <h2>Plans</h2>
        @for (option of info.options; track option.id) {
          <div class="profile-panel" style="margin-bottom: 1rem;">
            <p class="card-kicker">{{ option.name }}</p>
            <p>{{ option.monthlyPrice | number:'1.0-0' }} USD / month ·
              {{ option.seatCap === 2147483647 ? 'unlimited' : option.seatCap }} seats</p>
            <p class="muted">
              Ask AI {{ option.allowsModelAi ? 'with model' : 'from catalog text' }} ·
              Chat {{ option.allowsChat ? 'included' : 'not included' }}
            </p>
          </div>
        }
      </section>
    }
  `,
})
export class BillingPage {
  private readonly http = inject(HttpClient);
  readonly session = inject(SessionService);
  readonly billing = signal<CampusBilling | null>(null);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly busy = signal(false);

  constructor() {
    void this.load();
  }

  async upgrade(): Promise<void> {
    this.busy.set(true);
    this.error.set(null);
    this.message.set(null);
    try {
      const result = await firstValueFrom(this.http.post<UpgradeResponse>('/api/campus/billing/upgrade', {}));
      this.message.set(result.message);
      await this.load();
    } catch (err: unknown) {
      const msg = (err as { error?: { error?: string } })?.error?.error;
      this.error.set(msg || 'Could not upgrade the plan.');
    } finally {
      this.busy.set(false);
    }
  }

  private async load(): Promise<void> {
    try {
      const info = await firstValueFrom(this.http.get<CampusBilling>('/api/campus/billing'));
      this.billing.set(info);
    } catch {
      this.error.set('Could not load billing details.');
    }
  }
}
