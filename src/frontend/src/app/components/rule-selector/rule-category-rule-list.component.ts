import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  output,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RuleCategory, ValidationRule } from '../../models/validation.models';
import {
  getRuleCategoryDisplay,
  getRuleCategoryIconClass,
} from '../../models/validation-display.models';
import { CategoryPanelHeadingComponent } from '../shared/category-panel-heading.component';
import { RuleCheckboxComponent } from './rule-checkbox.component';

@Component({
  selector: 'app-rule-category-rule-list',
  standalone: true,
  imports: [CommonModule, CategoryPanelHeadingComponent, RuleCheckboxComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="rounded-xl border border-parchment-300/60 overflow-hidden bg-white/50"
    >
      <div
        class="px-4 py-3 border-b border-parchment-200/60 bg-gradient-to-r from-parchment-100/80 to-transparent"
      >
        <app-category-panel-heading
          [title]="categoryDisplay().displayName"
          [icon]="categoryDisplay().icon"
          [iconClass]="iconClass()"
        >
          <span class="badge-info text-xs">
            {{ selectedCount() }}/{{ rules().length }}
          </span>
        </app-category-panel-heading>
      </div>

      <div class="p-2 space-y-1.5">
        @for (rule of rules(); track rule.name) {
          <app-rule-checkbox
            [rule]="rule"
            (toggle)="ruleToggled.emit($event)"
          />
        }
      </div>
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
export class RuleCategoryRuleListComponent {
  readonly category = input.required<RuleCategory>();
  readonly rules = input.required<readonly ValidationRule[]>();
  readonly selectedCount = input.required<number>();
  readonly ruleToggled = output<ValidationRule>();

  readonly categoryDisplay = computed(() =>
    getRuleCategoryDisplay(this.category()),
  );
  readonly iconClass = computed(() => getRuleCategoryIconClass(this.category()));
}
