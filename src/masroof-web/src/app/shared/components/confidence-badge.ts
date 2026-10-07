import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** Shows parse confidence as a colored percentage chip. */
@Component({
  selector: 'app-confidence-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (confidence() != null) {
      <span class="badge" [class]="level()">{{ pct() }}%</span>
    }
  `,
  styles: [
    `
      .badge {
        font-size: 0.72rem;
        font-weight: 700;
        padding: 0.1rem 0.45rem;
        border-radius: 6px;
      }
      .high {
        background: color-mix(in srgb, #2e7d32 18%, transparent);
        color: #2e7d32;
      }
      .mid {
        background: color-mix(in srgb, #f59e0b 20%, transparent);
        color: #b45309;
      }
      .low {
        background: color-mix(in srgb, #ef4444 18%, transparent);
        color: #b91c1c;
      }
    `,
  ],
})
export class ConfidenceBadge {
  readonly confidence = input<number | null>(null);
  readonly pct = computed(() => Math.round((this.confidence() ?? 0) * 100));
  readonly level = computed(() => {
    const c = this.confidence() ?? 0;
    return c >= 0.85 ? 'high' : c >= 0.7 ? 'mid' : 'low';
  });
}
