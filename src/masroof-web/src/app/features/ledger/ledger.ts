import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api/api.service';
import { CategoriesStore } from '../../core/data/categories.store';
import { I18nService } from '../../core/i18n/i18n.service';
import { ToastService } from '../../core/ui/toast.service';
import { Direction, PagedResult, TransactionDto } from '../../core/models/api-models';
import { CategoryChip } from '../../shared/components/category-chip';
import { ConfidenceBadge } from '../../shared/components/confidence-badge';
import { EmptyState } from '../../shared/components/empty-state';
import { MoneyPipe } from '../../shared/pipes/money.pipe';
import { RevealDirective } from '../../shared/motion/reveal.directive';
import { PressDirective } from '../../shared/motion/press.directive';

@Component({
  selector: 'app-ledger',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    CategoryChip,
    ConfidenceBadge,
    EmptyState,
    MoneyPipe,
    RevealDirective,
    PressDirective,
  ],
  templateUrl: './ledger.html',
  styleUrl: './ledger.scss',
})
export class Ledger implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  protected readonly i18n = inject(I18nService);
  protected readonly categoriesStore = inject(CategoriesStore);

  protected readonly month = signal('');
  protected readonly category = signal('');
  protected readonly direction = signal<'' | Direction>('');
  protected readonly search = signal('');
  protected readonly needsReview = signal(false);
  protected readonly page = signal(1);

  protected readonly loading = signal(false);
  protected readonly result = signal<PagedResult<TransactionDto> | null>(null);
  protected readonly editingId = signal<number | null>(null);

  protected readonly hasPrev = computed(() => (this.result()?.page ?? 1) > 1);
  protected readonly hasNext = computed(() => {
    const r = this.result();
    return r ? r.page < r.totalPages : false;
  });

  ngOnInit(): void {
    this.categoriesStore.ensureLoaded();
    this.load();
  }

  applyFilters(): void {
    this.page.set(1);
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api
      .getLedger({
        month: this.month() || undefined,
        category: this.category() || undefined,
        direction: this.direction() || undefined,
        q: this.search() || undefined,
        needsReview: this.needsReview() || undefined,
        page: this.page(),
        pageSize: 25,
      })
      .subscribe({
        next: (r) => {
          this.result.set(r);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }

  changePage(delta: number): void {
    this.page.update((p) => Math.max(1, p + delta));
    this.load();
  }

  correct(row: TransactionDto, code: string): void {
    this.editingId.set(null);
    if (!code || code === row.category.code) return;
    this.api.patchTransaction(row.id, { categoryCode: code }).subscribe({
      next: (updated) => {
        this.patchRow(updated);
        this.toast.success(`${this.i18n.t('capture.learned')}: ${updated.category.name}`);
      },
    });
  }

  remove(row: TransactionDto): void {
    this.api.deleteTransaction(row.id).subscribe({
      next: () => {
        this.result.update((r) =>
          r ? { ...r, items: r.items.filter((x) => x.id !== row.id), total: r.total - 1 } : r,
        );
      },
    });
  }

  private patchRow(updated: TransactionDto): void {
    this.result.update((r) =>
      r ? { ...r, items: r.items.map((x) => (x.id === updated.id ? updated : x)) } : r,
    );
  }
}
