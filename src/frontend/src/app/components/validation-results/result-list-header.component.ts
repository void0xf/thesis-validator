import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-result-list-header',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="flex flex-col gap-3 md:flex-row md:items-end md:justify-between"
    >
      <div>
        <h3 class="font-display text-xl font-semibold text-ink-900">
          Issues
        </h3>
        <p class="font-sans text-sm text-ink-500">
          {{ filteredCount() }} of {{ totalCount() }} shown
        </p>
      </div>

      @if (hasActiveFilters()) {
        <button
          type="button"
          class="btn-secondary inline-flex items-center justify-center gap-2 self-start md:self-auto"
          (click)="filtersCleared.emit()"
        >
          <lucide-icon name="rotate-ccw" class="w-4 h-4"></lucide-icon>
          Reset Filters
        </button>
      }
    </div>
  `,
})
export class ResultListHeaderComponent {
  readonly filteredCount = input.required<number>();
  readonly totalCount = input.required<number>();
  readonly hasActiveFilters = input(false);
  readonly filtersCleared = output<void>();
}
