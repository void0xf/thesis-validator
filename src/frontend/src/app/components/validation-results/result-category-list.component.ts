import {
  ChangeDetectionStrategy,
  Component,
  effect,
  input,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  CategoryGroup,
  ValidationRule,
} from '../../models/validation.models';
import { CategoryTileListComponent } from '../shared/category-tile-list.component';
import { ResultCategoryDetailComponent } from './result-category-detail.component';
import { ResultCategoryListStore } from './result-category-list.store';
import { ResultFilterEmptyComponent } from './result-filter-empty.component';
import { ResultFilterToolbarComponent } from './result-filter-toolbar.component';
import { ResultListHeaderComponent } from './result-list-header.component';

@Component({
  selector: 'app-result-category-list',
  standalone: true,
  imports: [
    CommonModule,
    CategoryTileListComponent,
    ResultCategoryDetailComponent,
    ResultFilterEmptyComponent,
    ResultFilterToolbarComponent,
    ResultListHeaderComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="space-y-4">
      <app-result-list-header
        [filteredCount]="store.filteredIssueCount()"
        [totalCount]="store.totalIssueCount()"
        [hasActiveFilters]="store.hasActiveFilters()"
        (filtersCleared)="store.clearFilters()"
      />

      <app-result-filter-toolbar
        [severityFilter]="store.severityFilter()"
        [rulePanelOpen]="store.rulePanelOpen()"
        [ruleVisibleIssueCount]="store.ruleVisibleIssueCount()"
        [totalErrorCount]="store.totalErrorCount()"
        [totalWarningCount]="store.totalWarningCount()"
        [ruleOptions]="store.ruleOptions()"
        [hiddenRuleOptions]="store.hiddenRuleOptions()"
        [hiddenRuleNames]="store.hiddenRuleNames()"
        [visibleRuleCount]="store.visibleRuleCount()"
        [totalRuleFilterCount]="store.totalRuleFilterCount()"
        [hiddenRuleCount]="store.hiddenRuleCount()"
        (severityFilterSelected)="store.setSeverityFilter($event)"
        (rulePanelToggled)="store.toggleRulePanel()"
        (ruleVisibilityChanged)="store.setRuleVisibility($event)"
        (ruleShown)="store.showRule($event)"
        (allRulesShown)="store.showAllRules()"
      />

      @if (store.categoryTiles().length > 0) {
        <app-category-tile-list
          [items]="store.categoryTiles()"
          [selectedCategory]="store.selectedCategoryForView()"
          columns="four"
          size="comfortable"
          (categorySelected)="store.selectCategory($event)"
        />
      } @else {
        <app-result-filter-empty />
      }

      @if (store.activeGroup(); as activeGroup) {
        <app-result-category-detail
          [group]="activeGroup"
          [ruleCatalog]="ruleCatalog()"
        />
      }
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
export class ResultCategoryListComponent {
  readonly categoryGroups = input.required<CategoryGroup[]>();
  readonly ruleCatalog = input<readonly ValidationRule[]>([]);
  readonly store = new ResultCategoryListStore();

  private readonly bindInputs = effect(
    () => {
      this.store.setInputs(this.categoryGroups(), this.ruleCatalog());
    },
    { allowSignalWrites: true },
  );
}
