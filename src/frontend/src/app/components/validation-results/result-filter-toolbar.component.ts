import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  RuleFilterOption,
  RuleVisibilityChange,
  SeverityFilter,
} from './result-filter.models';
import { ResultRuleVisibilityFilterComponent } from './result-rule-visibility-filter.component';
import { ResultSeverityFilterComponent } from './result-severity-filter.component';

@Component({
  selector: 'app-result-filter-toolbar',
  standalone: true,
  imports: [
    CommonModule,
    ResultRuleVisibilityFilterComponent,
    ResultSeverityFilterComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="paper-card results-surface p-3">
      <div
        class="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between"
      >
        <app-result-severity-filter
          [selected]="severityFilter()"
          [allCount]="ruleVisibleIssueCount()"
          [errorCount]="totalErrorCount()"
          [warningCount]="totalWarningCount()"
          (filterSelected)="severityFilterSelected.emit($event)"
        />

        <app-result-rule-visibility-filter
          [open]="rulePanelOpen()"
          [rules]="ruleOptions()"
          [hiddenRules]="hiddenRuleOptions()"
          [hiddenRuleNames]="hiddenRuleNames()"
          [visibleRuleCount]="visibleRuleCount()"
          [totalRuleCount]="totalRuleFilterCount()"
          [hiddenRuleCount]="hiddenRuleCount()"
          (panelToggled)="rulePanelToggled.emit()"
          (visibilityChanged)="ruleVisibilityChanged.emit($event)"
          (ruleShown)="ruleShown.emit($event)"
          (allRulesShown)="allRulesShown.emit()"
        />
      </div>
    </div>
  `,
})
export class ResultFilterToolbarComponent {
  readonly severityFilter = input.required<SeverityFilter>();
  readonly rulePanelOpen = input(false);
  readonly ruleVisibleIssueCount = input.required<number>();
  readonly totalErrorCount = input.required<number>();
  readonly totalWarningCount = input.required<number>();
  readonly ruleOptions = input.required<readonly RuleFilterOption[]>();
  readonly hiddenRuleOptions = input.required<readonly RuleFilterOption[]>();
  readonly hiddenRuleNames = input.required<ReadonlySet<string>>();
  readonly visibleRuleCount = input.required<number>();
  readonly totalRuleFilterCount = input.required<number>();
  readonly hiddenRuleCount = input.required<number>();

  readonly severityFilterSelected = output<SeverityFilter>();
  readonly rulePanelToggled = output<void>();
  readonly ruleVisibilityChanged = output<RuleVisibilityChange>();
  readonly ruleShown = output<string>();
  readonly allRulesShown = output<void>();
}
