import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  CategoryGroup,
  ValidationRule,
} from '../../models/validation.models';
import { getRuleCategoryIconClass } from '../../models/validation-display.models';
import { CategoryPanelHeadingComponent } from '../shared/category-panel-heading.component';
import { ResultIssueListComponent } from './result-issue-list.component';

@Component({
  selector: 'app-result-category-detail',
  standalone: true,
  imports: [
    CommonModule,
    CategoryPanelHeadingComponent,
    ResultIssueListComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="paper-card results-surface overflow-hidden">
      <div class="px-5 py-4 border-b border-parchment-200/60">
        <app-category-panel-heading
          [title]="group().displayName"
          [subtitle]="issueLabel()"
          [icon]="group().icon"
          [iconClass]="iconClass()"
          size="md"
        >
          <div class="flex items-center gap-2 flex-shrink-0">
            @if (group().errorCount > 0) {
              <span class="badge-error">{{ group().errorCount }}</span>
            }
            @if (group().warningCount > 0) {
              <span class="badge-warning">{{ group().warningCount }}</span>
            }
          </div>
        </app-category-panel-heading>
      </div>

      <app-result-issue-list
        [results]="group().results"
        [ruleCatalog]="ruleCatalog()"
      />
    </div>
  `,
})
export class ResultCategoryDetailComponent {
  readonly group = input.required<CategoryGroup>();
  readonly ruleCatalog = input<readonly ValidationRule[]>([]);

  readonly iconClass = computed(() =>
    getRuleCategoryIconClass(this.group().category, 'solid'),
  );
  readonly issueLabel = computed(() => {
    const count = this.group().results.length;
    return `${count} issue${count !== 1 ? 's' : ''}`;
  });
}
