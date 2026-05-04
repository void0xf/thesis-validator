import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-rule-selector-empty',
  standalone: true,
  imports: [LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="flex items-start gap-2 rounded-lg border border-academic-red/10 bg-academic-red/5 p-3"
    >
      <lucide-icon
        name="circle-alert"
        class="w-4 h-4 text-academic-red flex-shrink-0 mt-0.5"
      ></lucide-icon>
      <p class="font-sans text-sm text-academic-red">
        Validation rules could not be loaded.
      </p>
    </div>
  `,
})
export class RuleSelectorEmptyComponent {}
