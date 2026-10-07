import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api/api.service';
import { CategoriesStore } from '../../core/data/categories.store';
import { I18nService } from '../../core/i18n/i18n.service';
import { ToastService } from '../../core/ui/toast.service';
import { RuleDto } from '../../core/models/api-models';
import { CategoryChip } from '../../shared/components/category-chip';
import { EmptyState } from '../../shared/components/empty-state';

@Component({
  selector: 'app-rules',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, CategoryChip, EmptyState],
  templateUrl: './rules.html',
  styleUrl: './rules.scss',
})
export class Rules implements OnInit {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  protected readonly i18n = inject(I18nService);
  protected readonly categoriesStore = inject(CategoriesStore);

  protected readonly rules = signal<RuleDto[]>([]);
  protected readonly loading = signal(true);
  protected readonly search = signal('');

  ngOnInit(): void {
    this.categoriesStore.ensureLoaded();
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.getRules(this.search() || undefined).subscribe({
      next: (r) => {
        this.rules.set(r);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  forget(rule: RuleDto): void {
    this.api.deleteRule(rule.ruleId).subscribe({
      next: () => {
        this.rules.update((list) => list.filter((x) => x.ruleId !== rule.ruleId));
        this.toast.show(`Forgot "${rule.subject}".`, 'info');
      },
    });
  }
}
