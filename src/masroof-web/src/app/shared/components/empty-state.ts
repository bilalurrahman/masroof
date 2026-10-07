import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="empty">
      <div class="icon">{{ icon() }}</div>
      <p>{{ message() }}</p>
    </div>
  `,
  styles: [
    `
      .empty {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 0.5rem;
        padding: 2.5rem 1rem;
        color: var(--text-muted);
        text-align: center;
      }
      .icon {
        font-size: 2rem;
        opacity: 0.6;
      }
    `,
  ],
})
export class EmptyState {
  readonly message = input.required<string>();
  readonly icon = input<string>('🗂️');
}
