import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-rule-selector-header',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-wrap items-start justify-between gap-3 mb-5">
      <div>
        <h2 class="font-display text-xl font-semibold text-ink-900">
          Validation Rules
        </h2>
        <p class="font-sans text-xs text-ink-500 mt-1">
          {{ selectedCount() }} of {{ totalCount() }} selected
        </p>
      </div>

      <button
        type="button"
        class="flex items-center gap-2 px-3 py-1.5 rounded-lg text-sm font-sans transition-all duration-200"
        [class]="
          allSelected()
            ? 'bg-academic-green/10 text-academic-green border border-academic-green/20'
            : 'bg-parchment-100 text-ink-600 border border-parchment-300 hover:bg-parchment-200'
        "
        [disabled]="disabled()"
        [class.opacity-50]="disabled()"
        [class.cursor-not-allowed]="disabled()"
        (click)="toggleAll.emit()"
      >
        <lucide-icon
          [name]="allSelected() ? 'check-check' : 'check'"
          class="w-4 h-4"
        ></lucide-icon>
        {{ allSelected() ? 'Deselect All' : 'Select All' }}
      </button>
    </div>
  `,
})
export class RuleSelectorHeaderComponent {
  readonly selectedCount = input.required<number>();
  readonly totalCount = input.required<number>();
  readonly allSelected = input.required<boolean>();
  readonly disabled = input(false);
  readonly toggleAll = output<void>();
}
