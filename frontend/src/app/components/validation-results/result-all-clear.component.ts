import { Component, ChangeDetectionStrategy } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-result-all-clear',
  standalone: true,
  imports: [LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="paper-card results-surface p-8 text-center">
      <div
        class="w-20 h-20 mx-auto mb-4 rounded-2xl bg-gradient-to-br from-academic-green/20 to-academic-green/5 flex items-center justify-center"
      >
        <lucide-icon
          name="check-circle-2"
          class="w-10 h-10 text-academic-green"
        ></lucide-icon>
      </div>
      <h3 class="font-display text-2xl font-semibold text-ink-900 mb-2">
        Document Validated Successfully
      </h3>
      <p class="font-body text-ink-600 max-w-md mx-auto">
        Your thesis meets all the selected formatting requirements. You're ready
        to submit!
      </p>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
      }
    `,
  ],
})
export class ResultAllClearComponent {}
