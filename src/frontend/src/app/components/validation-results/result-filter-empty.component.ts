import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-result-filter-empty',
  standalone: true,
  imports: [LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="paper-card results-surface px-5 py-8 text-center">
      <lucide-icon
        name="search-x"
        class="w-8 h-8 mx-auto text-ink-400"
      ></lucide-icon>
      <p class="font-sans text-sm text-ink-600 mt-3">
        No issues match the current filters.
      </p>
    </div>
  `,
})
export class ResultFilterEmptyComponent {}
