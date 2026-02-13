import {
  Component,
  Input,
  Output,
  EventEmitter,
  computed,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import {
  ValidationResponse,
  CategoryGroup,
  RuleCategory,
  RULE_METADATA,
  CATEGORY_INFO,
} from '../../models/validation.models';
import { ResultStatusBannerComponent } from './result-status-banner.component';
import { ResultStatsGridComponent } from './result-stats-grid.component';
import { ResultCategoryListComponent } from './result-category-list.component';
import { ResultAllClearComponent } from './result-all-clear.component';
import { ResultHeadingHierarchyComponent } from './result-heading-hierarchy.component';

@Component({
  selector: 'app-validation-results',
  standalone: true,
  imports: [
    CommonModule,
    LucideAngularModule,
    ResultStatusBannerComponent,
    ResultStatsGridComponent,
    ResultCategoryListComponent,
    ResultAllClearComponent,
    ResultHeadingHierarchyComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="space-y-6 animate-fade-in">
      <div class="paper-card-elevated results-surface overflow-hidden">
        <app-result-status-banner
          [response]="response"
          (downloadAnnotated)="onDownloadAnnotated.emit()"
        />
        <app-result-stats-grid
          [errors]="response.totalErrors"
          [warnings]="response.totalWarnings"
          [categoryCount]="categoryGroups().length"
        />
      </div>

      @if (categoryGroups().length > 0) {
        <app-result-category-list [categoryGroups]="categoryGroups()" />
      }

      @if (response.headings.length) {
        <app-result-heading-hierarchy [headings]="response.headings" />
      }

      @if (response.isValid) {
        <app-result-all-clear />
      }

      <div class="flex justify-center gap-4 pt-4">
        <button
          type="button"
          class="btn-secondary flex items-center gap-2"
          (click)="onReset.emit()"
        >
          <lucide-icon name="rotate-ccw" class="w-4 h-4"></lucide-icon>
          Validate Another Document
        </button>
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
export class ValidationResultsComponent {
  @Input({ required: true }) response!: ValidationResponse;
  @Output() onDownloadAnnotated = new EventEmitter<void>();
  @Output() onReset = new EventEmitter<void>();

  categoryGroups = computed(() => {
    const groups: Map<RuleCategory, CategoryGroup> = new Map();

    for (const result of this.response.results) {
      const category = RULE_METADATA[result.ruleName]?.category || 'formatting';

      if (!groups.has(category)) {
        groups.set(category, {
          category,
          displayName: CATEGORY_INFO[category]?.displayName || category,
          icon: CATEGORY_INFO[category]?.icon || 'check',
          results: [],
          errorCount: 0,
          warningCount: 0,
        });
      }

      const group = groups.get(category)!;
      group.results.push(result);
      if (result.isError) {
        group.errorCount++;
      } else {
        group.warningCount++;
      }
    }

    return Array.from(groups.values()).sort(
      (a, b) =>
        (CATEGORY_INFO[a.category]?.order || 99) -
        (CATEGORY_INFO[b.category]?.order || 99),
    );
  });
}
