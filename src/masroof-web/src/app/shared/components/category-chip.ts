import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** A small category pill with the taxonomy color. */
@Component({
  selector: 'app-category-chip',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="chip" [style.--chip]="color() ?? '#90A4AE'">
      <span class="dot"></span>{{ name() }}
    </span>
  `,
  styles: [
    `
      .chip {
        display: inline-flex;
        align-items: center;
        gap: 0.4rem;
        padding: 0.15rem 0.6rem;
        border-radius: 999px;
        font-size: 0.8rem;
        font-weight: 600;
        background: color-mix(in srgb, var(--chip) 15%, transparent);
        color: color-mix(in srgb, var(--chip) 70%, var(--text));
        white-space: nowrap;
      }
      .dot {
        width: 0.5rem;
        height: 0.5rem;
        border-radius: 50%;
        background: var(--chip);
      }
    `,
  ],
})
export class CategoryChip {
  readonly name = input.required<string>();
  readonly color = input<string | null>(null);
}
