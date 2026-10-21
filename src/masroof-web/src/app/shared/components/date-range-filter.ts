import { ChangeDetectionStrategy, Component, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { I18nService } from '../../core/i18n/i18n.service';
import { PressDirective } from '../motion/press.directive';

/** An inclusive from/to date range. Empty strings mean "no bound on this side". */
export interface DateRange {
  from: string;
  to: string;
}

/**
 * A From/To date-range control with quick presets (this month, last 30 days, year-to-date).
 * Owns its own from/to state and emits {@link rangeChange} on every edit; the parent just reacts.
 * Shared by the Ledger and Insights screens so both read the same way.
 */
@Component({
  selector: 'app-date-range-filter',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, PressDirective],
  templateUrl: './date-range-filter.html',
  styleUrl: './date-range-filter.scss',
})
export class DateRangeFilter {
  protected readonly i18n = inject(I18nService);

  /** Seed values (yyyy-MM-dd); applied once on init. */
  readonly initialFrom = input<string>('');
  readonly initialTo = input<string>('');

  readonly rangeChange = output<DateRange>();

  protected readonly from = signal('');
  protected readonly to = signal('');
  /** Which preset is active, for highlighting. '' = none (custom or cleared). */
  protected readonly active = signal<'' | 'month' | 'last30' | 'ytd'>('');

  constructor() {
    queueMicrotask(() => {
      this.from.set(this.initialFrom());
      this.to.set(this.initialTo());
    });
  }

  setFrom(v: string): void {
    this.from.set(v);
    this.active.set('');
    this.emit();
  }

  setTo(v: string): void {
    this.to.set(v);
    this.active.set('');
    this.emit();
  }

  preset(kind: 'month' | 'last30' | 'ytd'): void {
    const today = new Date();
    let start: Date;
    const end = today;
    if (kind === 'month') {
      start = new Date(today.getFullYear(), today.getMonth(), 1);
    } else if (kind === 'ytd') {
      start = new Date(today.getFullYear(), 0, 1);
    } else {
      start = new Date(today);
      start.setDate(start.getDate() - 29);
    }
    this.from.set(iso(start));
    this.to.set(iso(end));
    this.active.set(kind);
    this.emit();
  }

  clear(): void {
    this.from.set('');
    this.to.set('');
    this.active.set('');
    this.emit();
  }

  private emit(): void {
    this.rangeChange.emit({ from: this.from(), to: this.to() });
  }
}

/** Local-date → yyyy-MM-dd (no UTC shift, so the picked day is preserved). */
function iso(d: Date): string {
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${d.getFullYear()}-${m}-${day}`;
}
