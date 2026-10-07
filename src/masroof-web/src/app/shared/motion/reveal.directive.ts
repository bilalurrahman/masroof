import { Directive, ElementRef, afterNextRender, inject, input } from '@angular/core';
import { animate } from 'motion';
import { EASE_OUT, prefersReducedMotion } from './motion.util';

/**
 * `appReveal` — fades and lifts an element into view on mount.
 *
 * Use `[appReveal]="i"` inside an `@for` to stagger a list: each item's index
 * becomes a small incremental delay. `revealDelay` adds a flat base delay (ms),
 * `revealY` overrides the travel distance (px).
 */
@Directive({ selector: '[appReveal]' })
export class RevealDirective {
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  /** Stagger index when used in a list; contributes index * 45ms of delay. */
  readonly appReveal = input<number | ''>('');
  /** Flat base delay in milliseconds. */
  readonly revealDelay = input(0);
  /** Vertical travel distance in pixels. */
  readonly revealY = input(10);

  constructor() {
    afterNextRender(() => {
      const el = this.host.nativeElement;
      if (prefersReducedMotion()) {
        el.style.opacity = '1';
        return;
      }
      const idx = typeof this.appReveal() === 'number' ? (this.appReveal() as number) : 0;
      // Cap the staggered portion so long lists don't animate for seconds.
      const staggered = Math.min(idx, 12) * 45;
      const delay = (this.revealDelay() + staggered) / 1000;
      el.style.opacity = '0';
      animate(
        el,
        { opacity: [0, 1], transform: [`translateY(${this.revealY()}px)`, 'translateY(0)'] },
        { duration: 0.5, delay, ease: EASE_OUT },
      );
    });
  }
}
