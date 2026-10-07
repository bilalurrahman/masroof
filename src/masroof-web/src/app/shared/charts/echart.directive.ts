import { Directive, ElementRef, OnDestroy, effect, inject, input } from '@angular/core';
import type { EChartsType } from 'echarts';
import * as echarts from 'echarts';

/**
 * Renders an ECharts option onto the host element and keeps it sized to its container.
 * Usage: <div appEchart [option]="chartOption()" [theme]="theme()"></div>
 */
@Directive({ selector: '[appEchart]' })
export class EchartDirective implements OnDestroy {
  private readonly host = inject(ElementRef<HTMLElement>);
  private chart?: EChartsType;
  private resizeObserver?: ResizeObserver;

  readonly option = input<echarts.EChartsCoreOption | null>(null);
  readonly theme = input<string | null>(null);

  constructor() {
    effect(() => {
      const theme = this.theme();
      const option = this.option();
      // Re-create on theme change so colors follow light/dark.
      this.ensureChart(theme);
      if (this.chart && option) {
        this.chart.setOption(option, true);
      }
    });

    this.resizeObserver = new ResizeObserver(() => this.chart?.resize());
    this.resizeObserver.observe(this.host.nativeElement);
  }

  private ensureChart(theme: string | null): void {
    const el = this.host.nativeElement;
    if (this.chart && this.chart.getOption() && (this.chart as unknown as { _theme?: string })._theme === theme) {
      return;
    }
    this.chart?.dispose();
    this.chart = echarts.init(el, theme ?? undefined, { renderer: 'canvas' });
    (this.chart as unknown as { _theme?: string })._theme = theme ?? undefined;
  }

  ngOnDestroy(): void {
    this.resizeObserver?.disconnect();
    this.chart?.dispose();
  }
}
