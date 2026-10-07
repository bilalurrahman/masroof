import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth/auth.service';
import { I18nService } from './core/i18n/i18n.service';
import { ThemeService } from './core/theme/theme.service';
import { ToastHost } from './shared/components/toast-host';
import { PageTransitionDirective } from './shared/motion/page-transition.directive';
import { PressDirective } from './shared/motion/press.directive';

@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    ToastHost,
    PageTransitionDirective,
    PressDirective,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  protected readonly i18n = inject(I18nService);
  protected readonly theme = inject(ThemeService);
  protected readonly auth = inject(AuthService);

  protected readonly nav = [
    { path: '/capture', key: 'nav.capture', icon: '➕' },
    { path: '/ledger', key: 'nav.ledger', icon: '📒' },
    { path: '/insights', key: 'nav.insights', icon: '📊' },
    { path: '/ask', key: 'nav.ask', icon: '💬' },
    { path: '/rules', key: 'nav.rules', icon: '🧠' },
  ];
}
