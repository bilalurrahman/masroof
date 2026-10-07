import { Injectable, inject, signal } from '@angular/core';
import { ApiService } from '../api/api.service';
import { CategoryDto } from '../models/api-models';

/** Loads the taxonomy once and caches it for pickers and color lookups. */
@Injectable({ providedIn: 'root' })
export class CategoriesStore {
  private readonly api = inject(ApiService);
  readonly categories = signal<CategoryDto[]>([]);
  private loaded = false;

  ensureLoaded(): void {
    if (this.loaded) return;
    this.loaded = true;
    this.api.getCategories().subscribe({
      next: (c) => this.categories.set(c),
      error: () => (this.loaded = false),
    });
  }

  colorFor(code: string): string | null {
    return this.categories().find((c) => c.code === code)?.color ?? null;
  }
}
