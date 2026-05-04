import {
  Component,
  Input,
  Output,
  EventEmitter,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import {
  ValidationResponse,
  CategoryGroup,
  HeadingInfo,
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
          [downloadingAnnotated]="downloadingAnnotated"
          (downloadAnnotated)="onDownloadAnnotated.emit()"
        />
        <app-result-stats-grid
          [errors]="response.totalErrors"
          [warnings]="response.totalWarnings"
          [categoryCount]="categoryGroups.length"
        />
      </div>

      @if (categoryGroups.length > 0) {
        <app-result-category-list [categoryGroups]="categoryGroups" />
      }

      @if (headings.length) {
        <app-result-heading-hierarchy [headings]="headings" />
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
  @Input() downloadingAnnotated = false;
  @Output() onDownloadAnnotated = new EventEmitter<void>();
  @Output() onReset = new EventEmitter<void>();

  private _response!: ValidationResponse;
  categoryGroups: CategoryGroup[] = [];
  headings: HeadingInfo[] = [];

  @Input({ required: true })
  set response(value: ValidationResponse) {
    const results = Array.isArray(value.results) ? value.results : [];
    const headings = Array.isArray(value.headings) ? value.headings : [];
    const totalErrors =
      value.totalErrors ?? results.filter((result) => result.isError).length;
    const totalWarnings =
      value.totalWarnings ?? results.filter((result) => !result.isError).length;

    this._response = {
      ...value,
      results,
      headings,
      totalErrors,
      totalWarnings,
      isValid: value.isValid ?? totalErrors === 0,
    };
    this.headings = headings;
    this.categoryGroups = this.buildCategoryGroups(results);
  }

  get response(): ValidationResponse {
    return this._response;
  }

  private buildCategoryGroups(results: ValidationResponse['results']): CategoryGroup[] {
    const groups: Map<RuleCategory, CategoryGroup> = new Map();

    for (const result of results) {
      const category =
        this.normalizeCategory(result.category) ||
        RULE_METADATA[result.ruleName]?.category ||
        'formatting';

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
  }

  private normalizeCategory(category: string | undefined): RuleCategory | null {
    if (!category) {
      return null;
    }

    const normalized = category.toLowerCase() as RuleCategory;
    return normalized in CATEGORY_INFO ? normalized : null;
  }
}
