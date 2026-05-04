import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ValidationResult } from '../../models/validation.models';
import { formatRuleDisplayName } from '../../models/validation-display.models';
import { ResultLocationLinksComponent } from './result-location-links.component';
import { ResultSeverityDotComponent } from './result-severity-dot.component';

@Component({
  selector: 'app-result-item',
  standalone: true,
  imports: [
    CommonModule,
    ResultLocationLinksComponent,
    ResultSeverityDotComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="px-5 py-4 border-b border-parchment-100 last:border-b-0 hover:bg-parchment-50/50"
    >
      <div class="flex items-start gap-3">
        <app-result-severity-dot [isError]="result().isError" />

        <div class="flex-1 min-w-0">
          <span
            class="font-mono text-xs px-2 py-0.5 rounded bg-ink-100 text-ink-600"
          >
            {{ resolvedRuleDisplayName() }}
          </span>
          <p class="font-body text-ink-800 mt-2 leading-relaxed">
            {{ result().message }}
          </p>

          <app-result-location-links [location]="result().location" />
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        contain: layout style;
      }
    `,
  ],
})
export class ResultItemComponent {
  readonly result = input.required<ValidationResult>();
  readonly ruleDisplayName = input<string | null>(null);

  readonly resolvedRuleDisplayName = computed(
    () =>
      this.ruleDisplayName() ||
      formatRuleDisplayName(this.result().ruleName),
  );
}
