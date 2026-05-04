import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-rule-selector-hint',
  standalone: true,
  imports: [LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="flex items-start gap-2 mt-4 p-3 rounded-lg bg-academic-blue/5 border border-academic-blue/10"
    >
      <lucide-icon
        name="info"
        class="w-4 h-4 text-academic-blue flex-shrink-0 mt-0.5"
      ></lucide-icon>
      <p class="font-sans text-xs text-academic-blue/80 leading-relaxed">
        Selected rules will be applied during validation. Deselect rules you
        want to skip.
      </p>
    </div>
  `,
})
export class RuleSelectorHintComponent {}
