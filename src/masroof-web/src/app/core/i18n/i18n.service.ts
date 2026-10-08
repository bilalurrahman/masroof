import { Injectable, computed, effect, signal } from '@angular/core';

export type Locale = 'en' | 'ar';

const DICT: Record<Locale, Record<string, string>> = {
  en: {
    'app.title': 'Masroof',
    'nav.capture': 'Capture',
    'nav.ledger': 'Ledger',
    'nav.insights': 'Insights',
    'nav.ask': 'Ask',
    'nav.rules': 'Learned rules',
    'nav.accounts': 'Accounts',
    'capture.heading': 'Add an expense',
    'capture.placeholder': 'Paste a bank SMS or type a note…',
    'capture.batch': 'Batch paste (one per blank line)',
    'capture.parse': 'Parse',
    'capture.parsing': 'Parsing…',
    'capture.recent': 'Recently parsed',
    'capture.monthTotal': 'This month',
    'capture.learned': 'Will remember',
    'ledger.heading': 'Ledger',
    'ledger.search': 'Search merchant or text',
    'ledger.all': 'All',
    'ledger.needsReview': 'Needs review',
    'ledger.empty': 'No transactions match these filters.',
    'ledger.debit': 'Debit',
    'ledger.credit': 'Credit',
    'ledger.prev': 'Previous',
    'ledger.next': 'Next',
    'ledger.page': 'Page',
    'insights.heading': 'Insights',
    'insights.byCategory': 'Spend by category',
    'insights.trend': 'Monthly trend',
    'insights.vsLast': 'vs last month',
    'insights.spent': 'Spent',
    'insights.received': 'Received',
    'insights.empty': 'No data for this month yet.',
    'ask.heading': 'Ask',
    'ask.placeholder': 'How much did I spend on groceries this month?',
    'ask.send': 'Ask',
    'ask.thinking': 'Thinking…',
    'ask.dataUsed': 'Data used',
    'ask.suggestions': 'Try asking',
    'rules.heading': 'What Masroof has learned',
    'rules.search': 'Search rules',
    'rules.empty': 'Nothing learned yet. Correct a category and it will appear here.',
    'rules.hits': 'hits',
    'rules.forget': 'Forget',
    'accounts.heading': 'Your accounts',
    'accounts.intro': 'Mark the accounts that are yours. Transfers between your own accounts are tracked but not counted as spending.',
    'accounts.empty': 'No accounts yet. They appear automatically as transactions are captured.',
    'accounts.mine': 'Mine',
    'accounts.nickname': 'Nickname',
    'accounts.nicknamePh': 'e.g. Salary, Budget',
    'accounts.ibanTail': 'IBAN last 4',
    'accounts.txns': 'transactions',
    'accounts.add': 'Add account',
    'accounts.saved': 'Account saved',
    'common.category': 'Category',
    'common.amount': 'Amount',
    'common.date': 'Date',
    'common.merchant': 'Merchant',
    'common.save': 'Save',
    'common.cancel': 'Cancel',
    'common.delete': 'Delete',
    'common.confidence': 'Confidence',
    'common.theme': 'Theme',
    'common.language': 'العربية',
    'auth.login': 'Sign in',
    'auth.logout': 'Sign out',
  },
  ar: {
    'app.title': 'مصروف',
    'nav.capture': 'إضافة',
    'nav.ledger': 'السجل',
    'nav.insights': 'تحليلات',
    'nav.ask': 'اسأل',
    'nav.rules': 'القواعد المتعلمة',
    'nav.accounts': 'الحسابات',
    'capture.heading': 'أضف مصروفاً',
    'capture.placeholder': 'الصق رسالة البنك أو اكتب ملاحظة…',
    'capture.batch': 'لصق متعدد (واحد لكل سطر فارغ)',
    'capture.parse': 'تحليل',
    'capture.parsing': 'جارٍ التحليل…',
    'capture.recent': 'أحدث ما تم تحليله',
    'capture.monthTotal': 'هذا الشهر',
    'capture.learned': 'سيتم التذكّر',
    'ledger.heading': 'السجل',
    'ledger.search': 'ابحث عن متجر أو نص',
    'ledger.all': 'الكل',
    'ledger.needsReview': 'يحتاج مراجعة',
    'ledger.empty': 'لا توجد عمليات مطابقة.',
    'ledger.debit': 'مدين',
    'ledger.credit': 'دائن',
    'ledger.prev': 'السابق',
    'ledger.next': 'التالي',
    'ledger.page': 'صفحة',
    'insights.heading': 'تحليلات',
    'insights.byCategory': 'الإنفاق حسب الفئة',
    'insights.trend': 'الاتجاه الشهري',
    'insights.vsLast': 'مقارنة بالشهر الماضي',
    'insights.spent': 'المصروف',
    'insights.received': 'الوارد',
    'insights.empty': 'لا توجد بيانات لهذا الشهر بعد.',
    'ask.heading': 'اسأل',
    'ask.placeholder': 'كم أنفقت على البقالة هذا الشهر؟',
    'ask.send': 'اسأل',
    'ask.thinking': 'جارٍ التفكير…',
    'ask.dataUsed': 'البيانات المستخدمة',
    'ask.suggestions': 'جرّب أن تسأل',
    'rules.heading': 'ما تعلّمه مصروف',
    'rules.search': 'ابحث في القواعد',
    'rules.empty': 'لم يُتعلَّم شيء بعد. صحّح فئة لتظهر هنا.',
    'rules.hits': 'مرات',
    'rules.forget': 'نسيان',
    'accounts.heading': 'حساباتك',
    'accounts.intro': 'حدّد الحسابات التي تخصّك. التحويلات بين حساباتك الخاصة تُسجَّل لكنها لا تُحتسب كإنفاق.',
    'accounts.empty': 'لا توجد حسابات بعد. تظهر تلقائياً عند تسجيل العمليات.',
    'accounts.mine': 'حسابي',
    'accounts.nickname': 'الاسم المختصر',
    'accounts.nicknamePh': 'مثال: الراتب، الميزانية',
    'accounts.ibanTail': 'آخر 4 من الآيبان',
    'accounts.txns': 'عملية',
    'accounts.add': 'إضافة حساب',
    'accounts.saved': 'تم حفظ الحساب',
    'common.category': 'الفئة',
    'common.amount': 'المبلغ',
    'common.date': 'التاريخ',
    'common.merchant': 'المتجر',
    'common.save': 'حفظ',
    'common.cancel': 'إلغاء',
    'common.delete': 'حذف',
    'common.confidence': 'الثقة',
    'common.theme': 'المظهر',
    'common.language': 'English',
    'auth.login': 'تسجيل الدخول',
    'auth.logout': 'خروج',
  },
};

const STORAGE_KEY = 'masroof.locale';

@Injectable({ providedIn: 'root' })
export class I18nService {
  readonly locale = signal<Locale>(this.initial());
  readonly dir = computed<'rtl' | 'ltr'>(() => (this.locale() === 'ar' ? 'rtl' : 'ltr'));

  constructor() {
    effect(() => {
      const loc = this.locale();
      document.documentElement.setAttribute('lang', loc);
      document.documentElement.setAttribute('dir', this.dir());
      try {
        localStorage.setItem(STORAGE_KEY, loc);
      } catch {
        /* ignore */
      }
    });
  }

  /** Reads the locale signal, so template bindings re-evaluate on toggle. */
  t(key: string): string {
    return DICT[this.locale()][key] ?? DICT.en[key] ?? key;
  }

  toggle(): void {
    this.locale.update((l) => (l === 'en' ? 'ar' : 'en'));
  }

  private initial(): Locale {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored === 'en' || stored === 'ar') return stored;
    } catch {
      /* ignore */
    }
    return 'en';
  }
}
