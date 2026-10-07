import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api/api.service';
import { I18nService } from '../../core/i18n/i18n.service';
import { ToolInvocation } from '../../core/models/api-models';
import { RevealDirective } from '../../shared/motion/reveal.directive';
import { PressDirective } from '../../shared/motion/press.directive';

interface ChatTurn {
  id: number;
  role: 'user' | 'assistant';
  text: string;
  tools?: ToolInvocation[];
  showData?: boolean;
}

@Component({
  selector: 'app-ask',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, RevealDirective, PressDirective],
  templateUrl: './ask.html',
  styleUrl: './ask.scss',
})
export class Ask {
  private readonly api = inject(ApiService);
  protected readonly i18n = inject(I18nService);

  private seq = 0;
  protected readonly turns = signal<ChatTurn[]>([]);
  protected readonly question = signal('');
  protected readonly thinking = signal(false);

  protected readonly suggestions = [
    'How much did I spend on groceries this month?',
    'What are my top 5 merchants this month?',
    'Compare my spending this month vs last month',
  ];

  ask(text?: string): void {
    const q = (text ?? this.question()).trim();
    if (!q || this.thinking()) return;

    this.turns.update((t) => [...t, { id: ++this.seq, role: 'user', text: q }]);
    this.question.set('');
    this.thinking.set(true);

    this.api.ask(q).subscribe({
      next: (res) => {
        this.turns.update((t) => [
          ...t,
          { id: ++this.seq, role: 'assistant', text: res.answer, tools: res.toolsUsed },
        ]);
        this.thinking.set(false);
      },
      error: () => this.thinking.set(false),
    });
  }

  toggleData(turn: ChatTurn): void {
    this.turns.update((t) => t.map((x) => (x.id === turn.id ? { ...x, showData: !x.showData } : x)));
  }
}
