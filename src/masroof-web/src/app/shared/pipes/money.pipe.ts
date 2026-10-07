import { Pipe, PipeTransform, inject } from '@angular/core';
import { I18nService } from '../../core/i18n/i18n.service';

/** Formats an amount as localized currency via Intl.NumberFormat. Impure so it tracks locale. */
@Pipe({ name: 'money', pure: false })
export class MoneyPipe implements PipeTransform {
  private readonly i18n = inject(I18nService);

  transform(value: number | null | undefined, currency = 'SAR'): string {
    if (value == null) return '';
    const locale = this.i18n.locale() === 'ar' ? 'ar-SA' : 'en-US';
    try {
      return new Intl.NumberFormat(locale, {
        style: 'currency',
        currency,
        maximumFractionDigits: 2,
      }).format(value);
    } catch {
      return `${value.toFixed(2)} ${currency}`;
    }
  }
}
