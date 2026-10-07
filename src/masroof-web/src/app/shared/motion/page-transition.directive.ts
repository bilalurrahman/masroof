import { Directive, ElementRef, inject } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter } from 'rxjs';
import { animate } from 'motion';
import { EASE_OUT, prefersReducedMotion } from './motion.util';

/**
 * `appPageTransition` — replays a short fade-and-lift on the routed content each
 * time navigation completes, giving the SPA a sense of place as screens change.
 */
@Directive({ selector: '[appPageTransition]' })
export class PageTransitionDirective {
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly router = inject(Router);

  constructor() {
    this.router.events
      .pipe(
        filter((e) => e instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe(() => this.play());
  }

  private play(): void {
    if (prefersReducedMotion()) return;
    animate(
      this.host.nativeElement,
      { opacity: [0, 1], transform: ['translateY(8px)', 'translateY(0)'] },
      { duration: 0.42, ease: EASE_OUT },
    );
  }
}
