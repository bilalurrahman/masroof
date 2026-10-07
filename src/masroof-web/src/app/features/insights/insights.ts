import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import type { EChartsCoreOption } from 'echarts';
import { ApiService } from '../../core/api/api.service';
import { CategoriesStore } from '../../core/data/categories.store';
import { I18nService } from '../../core/i18n/i18n.service';
import { ThemeService } from '../../core/theme/theme.service';
import { MonthlySummary, TrendPoint } from '../../core/models/api-models';
import { EchartDirective } from '../../shared/charts/echart.directive';
import { EmptyState } from '../../shared/components/empty-state';
import { RevealDirective } from '../../shared/motion/reveal.directive';
import { CountUpDirective } from '../../shared/motion/count-up.directive';

@Component({
  selector: 'app-insights',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [EchartDirective, EmptyState, DecimalPipe, RevealDirective, CountUpDirective],
  templateUrl: './insights.html',
  styleUrl: './insights.scss',
})
export class Insights implements OnInit {
  private readonly api = inject(ApiService);
  private readonly categoriesStore = inject(CategoriesStore);
  protected readonly i18n = inject(I18nService);
  protected readonly theme = inject(ThemeService);

  protected readonly summary = signal<MonthlySummary | null>(null);
  protected readonly trend = signal<TrendPoint[]>([]);
  protected readonly loading = signal(true);
  protected readonly currency = signal('SAR');


  protected readonly deltaPct = computed(() => {
    const s = this.summary();
    if (!s || s.previousMonthDebit <= 0) return null;
    return ((s.totalDebit - s.previousMonthDebit) / s.previousMonthDebit) * 100;
  });

  protected readonly hasData = computed(() => (this.summary()?.transactionCount ?? 0) > 0);

  private readonly MONO = "'Geist Mono', ui-monospace, monospace";
  private readonly DEBIT = '#f87171';
  private readonly CREDIT = '#4ade80';

  protected readonly donutOption = computed<EChartsCoreOption | null>(() => {
    const s = this.summary();
    if (!s || s.byCategory.length === 0) return null;
    return {
      textStyle: { fontFamily: this.MONO },
      tooltip: { trigger: 'item', valueFormatter: (v: number) => this.fmt(v) },
      legend: { bottom: 0, textStyle: { color: this.labelColor(), fontFamily: this.MONO, fontSize: 10 } },
      series: [
        {
          type: 'pie',
          radius: ['52%', '74%'],
          avoidLabelOverlap: true,
          itemStyle: { borderRadius: 2, borderColor: this.bgColor(), borderWidth: 2 },
          label: { show: false },
          data: s.byCategory.map((c) => ({
            name: c.categoryName,
            value: c.total,
            itemStyle: { color: this.categoriesStore.colorFor(c.categoryCode) ?? undefined },
          })),
        },
      ],
    };
  });

  protected readonly trendOption = computed<EChartsCoreOption | null>(() => {
    const t = this.trend();
    if (t.length === 0) return null;
    const grid = this.gridColor();
    const area = (hex: string) => ({
      type: 'linear' as const,
      x: 0,
      y: 0,
      x2: 0,
      y2: 1,
      colorStops: [
        { offset: 0, color: hex + '40' },
        { offset: 1, color: hex + '00' },
      ],
    });
    return {
      textStyle: { fontFamily: this.MONO },
      tooltip: { trigger: 'axis', valueFormatter: (v: number) => this.fmt(v) },
      legend: {
        data: [this.i18n.t('insights.spent'), this.i18n.t('insights.received')],
        textStyle: { color: this.labelColor(), fontFamily: this.MONO, fontSize: 10 },
        icon: 'rect',
        itemWidth: 10,
        itemHeight: 2,
      },
      grid: { left: 52, right: 18, top: 40, bottom: 28 },
      xAxis: {
        type: 'category',
        data: t.map((p) => p.month),
        boundaryGap: false,
        axisLine: { lineStyle: { color: grid } },
        axisTick: { show: false },
        axisLabel: { color: this.labelColor(), fontFamily: this.MONO, fontSize: 10 },
      },
      yAxis: {
        type: 'value',
        splitLine: { lineStyle: { color: grid, type: 'dashed' } },
        axisLabel: { color: this.labelColor(), fontFamily: this.MONO, fontSize: 10 },
      },
      series: [
        {
          name: this.i18n.t('insights.spent'),
          type: 'line',
          smooth: true,
          symbol: 'none',
          lineStyle: { width: 2, color: this.DEBIT },
          itemStyle: { color: this.DEBIT },
          areaStyle: { color: area(this.DEBIT) },
          data: t.map((p) => p.debit),
        },
        {
          name: this.i18n.t('insights.received'),
          type: 'line',
          smooth: true,
          symbol: 'none',
          lineStyle: { width: 2, color: this.CREDIT },
          itemStyle: { color: this.CREDIT },
          areaStyle: { color: area(this.CREDIT) },
          data: t.map((p) => p.credit),
        },
      ],
    };
  });

  ngOnInit(): void {
    this.categoriesStore.ensureLoaded();
    this.api.getSummary().subscribe({
      next: (s) => {
        this.summary.set(s);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
    this.api.getTrend(6).subscribe({ next: (t) => this.trend.set(t) });
  }

  private labelColor(): string {
    return this.theme.theme() === 'dark' ? '#8c8e94' : '#5c5e66';
  }
  private gridColor(): string {
    return this.theme.theme() === 'dark' ? 'rgba(255,255,255,0.08)' : 'rgba(0,0,0,0.08)';
  }
  private bgColor(): string {
    return this.theme.theme() === 'dark' ? '#0f1012' : '#ffffff';
  }

  private fmt(v: number): string {
    return `${v.toFixed(2)} ${this.currency()}`;
  }
}
