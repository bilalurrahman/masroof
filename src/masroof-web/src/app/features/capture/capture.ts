import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api/api.service';
import { CategoriesStore } from '../../core/data/categories.store';
import { I18nService } from '../../core/i18n/i18n.service';
import { ToastService } from '../../core/ui/toast.service';
import { ParseResponse } from '../../core/models/api-models';
import { CategoryChip } from '../../shared/components/category-chip';
import { ConfidenceBadge } from '../../shared/components/confidence-badge';
import { MoneyPipe } from '../../shared/pipes/money.pipe';
import { RevealDirective } from '../../shared/motion/reveal.directive';
import { PressDirective } from '../../shared/motion/press.directive';
import { CountUpDirective } from '../../shared/motion/count-up.directive';

@Component({
  selector: 'app-capture',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    CategoryChip,
    ConfidenceBadge,
    MoneyPipe,
    RevealDirective,
    PressDirective,
    CountUpDirective,
  ],
  templateUrl: './capture.html',
  styleUrl: './capture.scss',
})
export class Capture implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  protected readonly i18n = inject(I18nService);
  protected readonly categoriesStore = inject(CategoriesStore);

  protected readonly text = signal('');
  protected readonly batch = signal(false);
  protected readonly parsing = signal(false);
  protected readonly recent = signal<ParseResponse[]>([]);
  protected readonly monthTotal = signal<number | null>(null);
  protected readonly currency = signal('SAR');

  ngOnInit(): void {
    this.categoriesStore.ensureLoaded();
    this.refreshMonthTotal();
  }

  submit(): void {
    const value = this.text().trim();
    if (!value || this.parsing()) return;
    this.parsing.set(true);

    if (this.batch()) {
      this.api.parseBatch(value).subscribe({
        next: (job) => {
          this.toast.success(`Queued ${job.count} messages (job ${job.jobId}).`);
          this.text.set('');
          this.parsing.set(false);
        },
        error: () => this.parsing.set(false),
      });
      return;
    }

    this.api.parse(value).subscribe({
      next: (row) => {
        this.recent.update((r) => [row, ...r].slice(0, 5));
        this.currency.set(row.currency);
        this.text.set('');
        this.parsing.set(false);
        this.refreshMonthTotal();
      },
      error: (err) => {
        if (err?.status === 409) {
          this.toast.show('That message is already in your ledger.', 'info');
          this.text.set('');
        }
        this.parsing.set(false);
      },
    });
  }

  correct(row: ParseResponse, code: string): void {
    if (!code || code === row.category.code) return;
    this.api.patchTransaction(row.id, { categoryCode: code }).subscribe({
      next: (updated) => {
        this.recent.update((list) =>
          list.map((x) => (x.id === row.id ? { ...x, category: updated.category } : x)),
        );
        const name = updated.category.name;
        this.toast.success(`${this.i18n.t('capture.learned')}: ${row.counterparty ?? ''} → ${name}`);
      },
    });
  }

  private refreshMonthTotal(): void {
    this.api.getSummary().subscribe({
      next: (s) => {
        this.monthTotal.set(s.totalDebit);
      },
    });
  }
}
