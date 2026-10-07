import { Directive, ElementRef, effect, inject, input } from '@angular/core';
import { animate } from 'motion';
import { prefersReducedMotion } from './motion.util';

/**
 * `appCountUp` — animates a numeral from its previous value to the current one,
 * writing a locale-formatted number into the host element each frame. Used for
 * money totals and headline figures so they "roll up" when data arrives.
 */
@Directive({ selector: '[appCountUp]' })
export class CountUpDirective {
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  readonly appCountUp = input.required<number | null | undefined>();
  readonly countUpDecimals = input(2);
  readonly countUpDuration = input(0.9);

  private previous = 0;

  constructor() {
    effect(() => {
      const target = this.appCountUp();
      if (target == null || isNaN(target)) return;
      const el = this.host.nativeElement;
      const from = this.previous;
      this.previous = target;

      if (prefersReducedMotion() || from === target) {
        el.textContent = this.format(target);
        return;
      }
      animate(from, target, {
        duration: this.countUpDuration(),
        ease: [0.16, 1, 0.3, 1],
        onUpdate: (latest) => {
          el.textContent = this.format(latest);
        },
      });
    });
  }

  private format(value: number): string {
    return value.toLocaleString(undefined, {
      minimumFractionDigits: this.countUpDecimals(),
      maximumFractionDigits: this.countUpDecimals(),
    });
  }
}
