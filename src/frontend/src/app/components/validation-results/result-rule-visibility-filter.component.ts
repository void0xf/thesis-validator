import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import {
  RuleFilterOption,
  RuleVisibilityChange,
} from './result-filter.models';

@Component({
  selector: 'app-result-rule-visibility-filter',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col gap-1.5 lg:min-w-[20rem]">
      <span class="font-sans text-xs font-medium uppercase text-ink-500">
        Rules
      </span>
      <button
        type="button"
        class="input-field flex items-center justify-between gap-3 py-2.5 text-left text-sm font-sans"
        [attr.aria-expanded]="open()"
        (click)="panelToggled.emit()"
      >
        <span class="flex min-w-0 items-center gap-2">
          <lucide-icon
            name="list-filter"
            class="w-4 h-4 flex-shrink-0 text-ink-500"
          ></lucide-icon>
          <span class="truncate">
            {{ visibleRuleCount() }} of {{ totalRuleCount() }} visible
          </span>
        </span>

        <span class="flex flex-shrink-0 items-center gap-2">
          @if (hiddenRuleCount() > 0) {
            <span class="badge-info">{{ hiddenRuleCount() }} hidden</span>
          }
          <lucide-icon
            name="chevron-down"
            class="w-4 h-4 transition-transform"
            [class.rotate-180]="open()"
          ></lucide-icon>
        </span>
      </button>
    </div>

    @if (open() && rules().length > 0) {
      <div
        class="mt-3 border-t border-parchment-200/70 pt-3 animate-slide-down"
      >
        <div class="flex items-center justify-between gap-3">
          <p class="font-sans text-xs font-medium uppercase text-ink-500">
            Rule visibility
          </p>

          @if (hiddenRuleCount() > 0) {
            <button
              type="button"
              class="font-sans text-sm font-medium text-academic-burgundy hover:text-academic-red"
              (click)="allRulesShown.emit()"
            >
              Show all
            </button>
          }
        </div>

        <div
          class="mt-3 grid max-h-64 grid-cols-1 gap-2 overflow-y-auto pr-1 scrollbar-thin sm:grid-cols-2 xl:grid-cols-3"
        >
          @for (rule of rules(); track rule.ruleName) {
            <label
              class="flex min-w-0 items-center justify-between gap-3 rounded-md border px-3 py-2 font-sans text-sm transition-colors"
              [class.bg-white]="isRuleVisible(rule.ruleName)"
              [class.border-parchment-300]="isRuleVisible(rule.ruleName)"
              [class.text-ink-900]="isRuleVisible(rule.ruleName)"
              [class.bg-parchment-100]="!isRuleVisible(rule.ruleName)"
              [class.border-parchment-400]="!isRuleVisible(rule.ruleName)"
              [class.text-ink-500]="!isRuleVisible(rule.ruleName)"
            >
              <span class="flex min-w-0 items-center gap-3">
                <input
                  type="checkbox"
                  class="h-4 w-4 flex-shrink-0 rounded border-parchment-400 accent-academic-burgundy"
                  [checked]="isRuleVisible(rule.ruleName)"
                  [attr.aria-label]="'Show ' + rule.displayName"
                  (change)="emitVisibilityChange(rule.ruleName, $event)"
                />
                <span class="truncate">{{ rule.displayName }}</span>
              </span>

              <span class="font-mono text-xs text-ink-500">
                {{ rule.count }}
              </span>
            </label>
          }
        </div>
      </div>
    }

    @if (hiddenRules().length > 0) {
      <div
        class="mt-3 flex flex-wrap items-center gap-2 border-t border-parchment-200/70 pt-3"
      >
        <span class="font-sans text-xs font-medium uppercase text-ink-500">
          Hidden
        </span>
        @for (rule of hiddenRules(); track rule.ruleName) {
          <button
            type="button"
            class="inline-flex max-w-full items-center gap-1.5 rounded-full border border-parchment-300 bg-white px-2.5 py-1 font-sans text-xs text-ink-700 shadow-sm transition-colors hover:border-academic-burgundy hover:text-academic-burgundy"
            (click)="ruleShown.emit(rule.ruleName)"
          >
            <span class="truncate">{{ rule.displayName }}</span>
            <lucide-icon name="x" class="h-3.5 w-3.5"></lucide-icon>
          </button>
        }
      </div>
    }
  `,
  styles: [
    `
      :host {
        display: block;
      }
    `,
  ],
})
export class ResultRuleVisibilityFilterComponent {
  readonly open = input(false);
  readonly rules = input.required<readonly RuleFilterOption[]>();
  readonly hiddenRules = input.required<readonly RuleFilterOption[]>();
  readonly hiddenRuleNames = input.required<ReadonlySet<string>>();
  readonly visibleRuleCount = input.required<number>();
  readonly totalRuleCount = input.required<number>();
  readonly hiddenRuleCount = input.required<number>();

  readonly panelToggled = output<void>();
  readonly visibilityChanged = output<RuleVisibilityChange>();
  readonly ruleShown = output<string>();
  readonly allRulesShown = output<void>();

  isRuleVisible(ruleName: string): boolean {
    return !this.hiddenRuleNames().has(ruleName);
  }

  emitVisibilityChange(ruleName: string, event: Event): void {
    const visible = (event.target as HTMLInputElement).checked;
    this.visibilityChanged.emit({ ruleName, visible });
  }
}
