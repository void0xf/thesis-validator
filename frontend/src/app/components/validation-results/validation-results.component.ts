import { Component, Input, Output, EventEmitter, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { ValidationResponse, CategoryGroup, RuleCategory, RULE_METADATA, CATEGORY_INFO } from '../../models/validation.models';
import { ResultStatusBannerComponent } from './result-status-banner.component';
import { ResultStatsGridComponent } from './result-stats-grid.component';
import { ResultCategoryListComponent } from './result-category-list.component';
import { ResultAllClearComponent } from './result-all-clear.component';

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
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="space-y-6 animate-fade-in">
      <div class="paper-card-elevated overflow-hidden">
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
        <app-result-category-list
          [categoryGroups]="categoryGroups()"
          [allExpanded]="allExpanded()"
          (toggleExpanded)="toggleCategoryExpand($event)"
          (toggleAll)="toggleAllCategories()"
        />
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
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class ValidationResultsComponent {
  @Input({ required: true }) response!: ValidationResponse;
  @Output() onDownloadAnnotated = new EventEmitter<void>();
  @Output() onReset = new EventEmitter<void>();

  private expandedState = signal<Record<string, boolean>>({});

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
          expanded: this.expandedState()[category] !== false
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

    return Array.from(groups.values())
      .sort((a, b) => (CATEGORY_INFO[a.category]?.order || 99) - (CATEGORY_INFO[b.category]?.order || 99));
  });

  allExpanded = computed(() => this.categoryGroups().every(g => g.expanded));

  toggleCategoryExpand(group: CategoryGroup): void {
    const current = this.expandedState();
    this.expandedState.set({
      ...current,
      [group.category]: !group.expanded
    });
  }

  toggleAllCategories(): void {
    const allExpanded = this.allExpanded();
    const newState: Record<string, boolean> = {};
    for (const group of this.categoryGroups()) {
      newState[group.category] = !allExpanded;
    }
    this.expandedState.set(newState);
  }
}
