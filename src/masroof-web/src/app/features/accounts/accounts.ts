import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api/api.service';
import { I18nService } from '../../core/i18n/i18n.service';
import { ToastService } from '../../core/ui/toast.service';
import { AccountDto } from '../../core/models/api-models';
import { EmptyState } from '../../shared/components/empty-state';
import { RevealDirective } from '../../shared/motion/reveal.directive';

@Component({
  selector: 'app-accounts',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, EmptyState, RevealDirective],
  templateUrl: './accounts.html',
  styleUrl: './accounts.scss',
})
export class Accounts implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  protected readonly i18n = inject(I18nService);

  protected readonly accounts = signal<AccountDto[]>([]);
  protected readonly loading = signal(true);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.getAccounts().subscribe({
      next: (a) => {
        this.accounts.set(a);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  /** A readable label for an account, e.g. "RAJHI ····1234". */
  label(a: AccountDto): string {
    const bank = a.bankCode && a.bankCode !== 'GENERIC' ? a.bankCode : 'Card';
    return a.last4 ? `${bank} ····${a.last4}` : bank;
  }

  save(a: AccountDto): void {
    this.api
      .updateAccount(a.accountId, {
        nickname: a.nickname?.trim() || null,
        ibanTail: a.ibanTail?.trim() || null,
        isOwn: a.isOwn,
      })
      .subscribe({
        next: (updated) => {
          this.accounts.update((list) => list.map((x) => (x.accountId === updated.accountId ? updated : x)));
          this.toast.success(this.i18n.t('accounts.saved'));
        },
      });
  }
}
