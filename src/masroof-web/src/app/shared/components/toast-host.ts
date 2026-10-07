import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ToastService } from '../../core/ui/toast.service';

@Component({
  selector: 'app-toast-host',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="host" aria-live="polite">
      @for (t of toast.toasts(); track t.id) {
        <div class="toast" [class]="t.kind" (click)="toast.dismiss(t.id)">{{ t.message }}</div>
      }
    </div>
  `,
  styles: [
    `
      .host {
        position: fixed;
        inset-block-end: 1rem;
        inset-inline-end: 1rem;
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
        z-index: 1000;
        max-width: min(90vw, 360px);
      }
      .toast {
        padding: 0.7rem 1rem;
        border-radius: 10px;
        color: #fff;
        font-size: 0.88rem;
        cursor: pointer;
        box-shadow: 0 6px 20px rgba(0, 0, 0, 0.18);
        animation: slide 0.18s ease-out;
      }
      .success {
        background: #2e7d32;
      }
      .error {
        background: #c62828;
      }
      .info {
        background: #374151;
      }
      @keyframes slide {
        from {
          opacity: 0;
          transform: translateY(8px);
        }
      }
    `,
  ],
})
export class ToastHost {
  readonly toast = inject(ToastService);
}
