import { Directive, ElementRef, HostListener, inject } from '@angular/core';
import { animate } from 'motion';
import { prefersReducedMotion } from './motion.util';

/**
 * `appPress` — tactile scale feedback on pointer press. Attach to buttons and
 * clickable cards for a crisp, physical response. No-op under reduced motion.
 */
@Directive({ selector: '[appPress]' })
export class PressDirective {
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  @HostListener('pointerdown')
  down(): void {
    if (prefersReducedMotion()) return;
    animate(this.host.nativeElement, { scale: 0.96 }, { duration: 0.08, ease: 'easeOut' });
  }

  @HostListener('pointerup')
  @HostListener('pointerleave')
  up(): void {
    if (prefersReducedMotion()) return;
    animate(this.host.nativeElement, { scale: 1 }, { type: 'spring', stiffness: 420, damping: 18 });
  }
}
