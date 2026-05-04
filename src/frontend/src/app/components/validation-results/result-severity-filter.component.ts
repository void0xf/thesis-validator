import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { SeverityFilter } from './result-filter.models';

@Component({
  selector: 'app-result-severity-filter',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="grid grid-cols-3 gap-1 rounded-lg border border-parchment-300/70 bg-parchment-100 p-1"
    >
      <button
        type="button"
        class="rounded-md px-3 py-2 font-sans text-sm transition-colors flex items-center justify-center gap-2"
        [class.bg-white]="selected() === 'all'"
        [class.text-ink-900]="selected() === 'all'"
        [class.shadow-sm]="selected() === 'all'"
        [class.text-ink-600]="selected() !== 'all'"
        [attr.aria-pressed]="selected() === 'all'"
        (click)="filterSelected.emit('all')"
      >
        <lucide-icon name="list-filter" class="w-4 h-4"></lucide-icon>
        <span>All</span>
        <span class="font-mono text-xs">{{ allCount() }}</span>
      </button>

      <button
        type="button"
        class="rounded-md px-3 py-2 font-sans text-sm transition-colors flex items-center justify-center gap-2"
        [class.bg-white]="selected() === 'errors'"
        [class.text-academic-red]="selected() === 'errors'"
        [class.shadow-sm]="selected() === 'errors'"
        [class.text-ink-600]="selected() !== 'errors'"
        [attr.aria-pressed]="selected() === 'errors'"
        (click)="filterSelected.emit('errors')"
      >
        <lucide-icon name="circle-x" class="w-4 h-4"></lucide-icon>
        <span>Errors</span>
        <span class="font-mono text-xs">{{ errorCount() }}</span>
      </button>

      <button
        type="button"
        class="rounded-md px-3 py-2 font-sans text-sm transition-colors flex items-center justify-center gap-2"
        [class.bg-white]="selected() === 'warnings'"
        [class.text-academic-gold]="selected() === 'warnings'"
        [class.shadow-sm]="selected() === 'warnings'"
        [class.text-ink-600]="selected() !== 'warnings'"
        [attr.aria-pressed]="selected() === 'warnings'"
        (click)="filterSelected.emit('warnings')"
      >
        <lucide-icon name="triangle-alert" class="w-4 h-4"></lucide-icon>
        <span>Warnings</span>
        <span class="font-mono text-xs">{{ warningCount() }}</span>
      </button>
    </div>
  `,
})
export class ResultSeverityFilterComponent {
  readonly selected = input.required<SeverityFilter>();
  readonly allCount = input.required<number>();
  readonly errorCount = input.required<number>();
  readonly warningCount = input.required<number>();
  readonly filterSelected = output<SeverityFilter>();
}
