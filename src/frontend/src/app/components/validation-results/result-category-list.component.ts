import {
  Component,
  Input,
  ChangeDetectionStrategy,
  OnChanges,
  SimpleChanges,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import {
  CategoryGroup,
  RuleCategory,
  RULE_METADATA,
  ValidationResult,
} from '../../models/validation.models';
import { ResultItemComponent } from './result-item.component';

type SeverityFilter = 'all' | 'errors' | 'warnings';

interface RuleFilterOption {
  ruleName: string;
  displayName: string;
  count: number;
}

@Component({
  selector: 'app-result-category-list',
  standalone: true,
  imports: [CommonModule, LucideAngularModule, ResultItemComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="space-y-4">
      <div
        class="flex flex-col gap-3 md:flex-row md:items-end md:justify-between"
      >
        <div>
          <h3 class="font-display text-xl font-semibold text-ink-900">
            Issues
          </h3>
          <p class="font-sans text-sm text-ink-500">
            {{ filteredIssueCount }} of {{ totalIssueCount }} shown
          </p>
        </div>

        @if (hasActiveFilters) {
          <button
            type="button"
            class="btn-secondary inline-flex items-center justify-center gap-2 self-start md:self-auto"
            (click)="clearFilters()"
          >
            <lucide-icon name="rotate-ccw" class="w-4 h-4"></lucide-icon>
            Reset Filters
          </button>
        }
      </div>

      <div class="paper-card results-surface p-3">
        <div
          class="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between"
        >
          <div
            class="grid grid-cols-3 gap-1 rounded-lg border border-parchment-300/70 bg-parchment-100 p-1"
          >
            <button
              type="button"
              class="rounded-md px-3 py-2 font-sans text-sm transition-colors flex items-center justify-center gap-2"
              [class.bg-white]="severityFilter === 'all'"
              [class.text-ink-900]="severityFilter === 'all'"
              [class.shadow-sm]="severityFilter === 'all'"
              [class.text-ink-600]="severityFilter !== 'all'"
              [attr.aria-pressed]="severityFilter === 'all'"
              (click)="setSeverityFilter('all')"
            >
              <lucide-icon name="list-filter" class="w-4 h-4"></lucide-icon>
              <span>All</span>
              <span class="font-mono text-xs">{{ ruleVisibleIssueCount }}</span>
            </button>

            <button
              type="button"
              class="rounded-md px-3 py-2 font-sans text-sm transition-colors flex items-center justify-center gap-2"
              [class.bg-white]="severityFilter === 'errors'"
              [class.text-academic-red]="severityFilter === 'errors'"
              [class.shadow-sm]="severityFilter === 'errors'"
              [class.text-ink-600]="severityFilter !== 'errors'"
              [attr.aria-pressed]="severityFilter === 'errors'"
              (click)="setSeverityFilter('errors')"
            >
              <lucide-icon name="circle-x" class="w-4 h-4"></lucide-icon>
              <span>Errors</span>
              <span class="font-mono text-xs">{{ totalErrorCount }}</span>
            </button>

            <button
              type="button"
              class="rounded-md px-3 py-2 font-sans text-sm transition-colors flex items-center justify-center gap-2"
              [class.bg-white]="severityFilter === 'warnings'"
              [class.text-academic-gold]="severityFilter === 'warnings'"
              [class.shadow-sm]="severityFilter === 'warnings'"
              [class.text-ink-600]="severityFilter !== 'warnings'"
              [attr.aria-pressed]="severityFilter === 'warnings'"
              (click)="setSeverityFilter('warnings')"
            >
              <lucide-icon name="triangle-alert" class="w-4 h-4"></lucide-icon>
              <span>Warnings</span>
              <span class="font-mono text-xs">{{ totalWarningCount }}</span>
            </button>
          </div>

          <div class="flex flex-col gap-1.5 lg:min-w-[20rem]">
            <span class="font-sans text-xs font-medium uppercase text-ink-500">
              Rules
            </span>
            <button
              type="button"
              class="input-field flex items-center justify-between gap-3 py-2.5 text-left text-sm font-sans"
              [attr.aria-expanded]="rulePanelOpen"
              (click)="toggleRulePanel()"
            >
              <span class="flex min-w-0 items-center gap-2">
                <lucide-icon
                  name="list-filter"
                  class="w-4 h-4 flex-shrink-0 text-ink-500"
                ></lucide-icon>
                <span class="truncate">
                  {{ visibleRuleCount }} of {{ totalRuleFilterCount }} visible
                </span>
              </span>

              <span class="flex flex-shrink-0 items-center gap-2">
                @if (hiddenRuleCount > 0) {
                  <span class="badge-info">{{ hiddenRuleCount }} hidden</span>
                }
                <lucide-icon
                  name="chevron-down"
                  class="w-4 h-4 transition-transform"
                  [class.rotate-180]="rulePanelOpen"
                ></lucide-icon>
              </span>
            </button>
          </div>
        </div>

        @if (rulePanelOpen && ruleOptions.length > 0) {
          <div class="mt-3 border-t border-parchment-200/70 pt-3 animate-slide-down">
            <div class="flex items-center justify-between gap-3">
              <p class="font-sans text-xs font-medium uppercase text-ink-500">
                Rule visibility
              </p>

              @if (hiddenRuleCount > 0) {
                <button
                  type="button"
                  class="font-sans text-sm font-medium text-academic-burgundy hover:text-academic-red"
                  (click)="showAllRules()"
                >
                  Show all
                </button>
              }
            </div>

            <div
              class="mt-3 grid max-h-64 grid-cols-1 gap-2 overflow-y-auto pr-1 scrollbar-thin sm:grid-cols-2 xl:grid-cols-3"
            >
              @for (rule of ruleOptions; track rule.ruleName) {
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
                      (change)="setRuleVisibility(rule.ruleName, $event)"
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

        @if (hiddenRuleOptions.length > 0) {
          <div
            class="mt-3 flex flex-wrap items-center gap-2 border-t border-parchment-200/70 pt-3"
          >
            <span class="font-sans text-xs font-medium uppercase text-ink-500">
              Hidden
            </span>
            @for (rule of hiddenRuleOptions; track rule.ruleName) {
              <button
                type="button"
                class="inline-flex max-w-full items-center gap-1.5 rounded-full border border-parchment-300 bg-white px-2.5 py-1 font-sans text-xs text-ink-700 shadow-sm transition-colors hover:border-academic-burgundy hover:text-academic-burgundy"
                (click)="showRule(rule.ruleName)"
              >
                <span class="truncate">{{ rule.displayName }}</span>
                <lucide-icon name="x" class="h-3.5 w-3.5"></lucide-icon>
              </button>
            }
          </div>
        }
      </div>

      @if (filteredCategoryGroups.length > 0) {
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-2">
          @for (group of filteredCategoryGroups; track group.category) {
            <button
              type="button"
              class="w-full text-left rounded-lg border px-4 py-3 transition-colors"
              [class.bg-parchment-100]="selectedCategory === group.category"
              [class.border-parchment-400]="selectedCategory === group.category"
              [class.text-ink-900]="selectedCategory === group.category"
              [class.bg-white]="selectedCategory !== group.category"
              [class.border-parchment-300]="selectedCategory !== group.category"
              [class.text-ink-700]="selectedCategory !== group.category"
              [class.hover:bg-parchment-50]="selectedCategory !== group.category"
              (click)="selectCategory(group)"
            >
              <div class="flex items-start justify-between gap-3">
                <div class="flex items-center gap-2 min-w-0">
                  <div
                    class="w-7 h-7 rounded-md flex items-center justify-center"
                    [class]="iconClasses[group.category]"
                  >
                    <lucide-icon
                      [name]="group.icon"
                      class="w-4 h-4"
                    ></lucide-icon>
                  </div>
                  <div class="min-w-0">
                    <p class="font-sans text-sm font-medium truncate">
                      {{ group.displayName }}
                    </p>
                    <p class="font-sans text-xs text-ink-500">
                      {{ group.results.length }} issue{{
                        group.results.length !== 1 ? 's' : ''
                      }}
                    </p>
                  </div>
                </div>
              </div>
            </button>
          }
        </div>
      } @else {
        <div class="paper-card results-surface px-5 py-8 text-center">
          <lucide-icon
            name="search-x"
            class="w-8 h-8 mx-auto text-ink-400"
          ></lucide-icon>
          <p class="font-sans text-sm text-ink-600 mt-3">
            No issues match the current filters.
          </p>
        </div>
      }

      @if (activeGroup) {
        <div class="paper-card results-surface overflow-hidden">
          <div
            class="px-5 py-4 border-b border-parchment-200/60 flex items-center justify-between gap-3"
          >
            <div class="flex items-center gap-3 min-w-0">
              <div
                class="w-9 h-9 rounded-lg flex items-center justify-center"
                [class]="iconClasses[activeGroup.category]"
              >
                <lucide-icon
                  [name]="activeGroup.icon"
                  class="w-4.5 h-4.5"
                ></lucide-icon>
              </div>
              <div class="min-w-0">
                <h4 class="font-sans font-semibold text-ink-900 truncate">
                  {{ activeGroup.displayName }}
                </h4>
                <p class="font-sans text-xs text-ink-500">
                  {{ activeGroup.results.length }} issue{{
                    activeGroup.results.length !== 1 ? 's' : ''
                  }}
                </p>
              </div>
            </div>

            <div class="flex items-center gap-2 flex-shrink-0">
              @if (activeGroup.errorCount > 0) {
                <span class="badge-error">{{ activeGroup.errorCount }}</span>
              }
              @if (activeGroup.warningCount > 0) {
                <span class="badge-warning">{{
                  activeGroup.warningCount
                }}</span>
              }
            </div>
          </div>

          <div class="max-h-[34rem] overflow-y-auto">
            @for (result of visibleResults; track result) {
              <app-result-item [result]="result" />
            }
          </div>
        </div>
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
export class ResultCategoryListComponent implements OnChanges {
  @Input({ required: true }) categoryGroups!: CategoryGroup[];

  severityFilter: SeverityFilter = 'all';
  rulePanelOpen = false;
  selectedCategory: RuleCategory | null = null;

  activeGroup: CategoryGroup | null = null;
  filteredCategoryGroups: CategoryGroup[] = [];
  visibleResults: ValidationResult[] = [];
  ruleOptions: RuleFilterOption[] = [];
  hiddenRuleOptions: RuleFilterOption[] = [];

  totalIssueCount = 0;
  ruleVisibleIssueCount = 0;
  totalErrorCount = 0;
  totalWarningCount = 0;
  filteredIssueCount = 0;
  totalRuleFilterCount = 0;
  visibleRuleCount = 0;
  hiddenRuleCount = 0;

  private readonly hiddenRuleNames = new Set<string>();

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['categoryGroups']) {
      this.rebuildFilteredState();
    }
  }

  get hasActiveFilters(): boolean {
    return this.severityFilter !== 'all' || this.hiddenRuleNames.size > 0;
  }

  setSeverityFilter(filter: SeverityFilter): void {
    if (this.severityFilter === filter) {
      return;
    }

    this.severityFilter = filter;
    this.rebuildFilteredState();
  }

  toggleRulePanel(): void {
    this.rulePanelOpen = !this.rulePanelOpen;
  }

  isRuleVisible(ruleName: string): boolean {
    return !this.hiddenRuleNames.has(ruleName);
  }

  setRuleVisibility(ruleName: string, event: Event): void {
    const isVisible = (event.target as HTMLInputElement).checked;
    if (isVisible) {
      this.hiddenRuleNames.delete(ruleName);
    } else {
      this.hiddenRuleNames.add(ruleName);
    }

    this.rebuildFilteredState();
  }

  showRule(ruleName: string): void {
    if (!this.hiddenRuleNames.delete(ruleName)) {
      return;
    }

    this.rebuildFilteredState();
  }

  showAllRules(): void {
    if (!this.hiddenRuleNames.size) {
      return;
    }

    this.hiddenRuleNames.clear();
    this.rebuildFilteredState();
  }

  clearFilters(): void {
    this.severityFilter = 'all';
    this.hiddenRuleNames.clear();
    this.rebuildFilteredState();
  }

  selectCategory(group: CategoryGroup): void {
    if (this.selectedCategory === group.category) {
      return;
    }

    this.selectedCategory = group.category;
    this.syncActiveCategory();
  }

  private rebuildFilteredState(): void {
    const allResults = this.categoryGroups.flatMap((group) => group.results);
    this.totalIssueCount = allResults.length;
    this.ruleOptions = this.buildRuleOptions(allResults);
    this.pruneHiddenRules();
    this.hiddenRuleOptions = this.ruleOptions.filter((rule) =>
      this.hiddenRuleNames.has(rule.ruleName),
    );
    this.totalRuleFilterCount = this.ruleOptions.length;
    this.hiddenRuleCount = this.hiddenRuleOptions.length;
    this.visibleRuleCount = this.totalRuleFilterCount - this.hiddenRuleCount;

    const ruleVisibleResults = allResults.filter((result) =>
      this.isRuleVisible(result.ruleName),
    );
    this.ruleVisibleIssueCount = ruleVisibleResults.length;
    this.totalErrorCount = ruleVisibleResults.filter(
      (result) => result.isError,
    ).length;
    this.totalWarningCount = this.ruleVisibleIssueCount - this.totalErrorCount;

    this.filteredCategoryGroups = this.categoryGroups
      .map((group) => {
        const results = group.results.filter(
          (result) =>
            this.matchesSeverity(result) && this.isRuleVisible(result.ruleName),
        );

        return {
          ...group,
          results,
          errorCount: results.filter((result) => result.isError).length,
          warningCount: results.filter((result) => !result.isError).length,
        };
      })
      .filter((group) => group.results.length > 0);

    this.filteredIssueCount = this.filteredCategoryGroups.reduce(
      (total, group) => total + group.results.length,
      0,
    );
    this.syncActiveCategory();
  }

  private syncActiveCategory(): void {
    if (!this.filteredCategoryGroups.length) {
      this.activeGroup = null;
      this.visibleResults = [];
      return;
    }

    const selectedExists =
      this.selectedCategory !== null &&
      this.filteredCategoryGroups.some(
        (group) => group.category === this.selectedCategory,
      );

    if (!selectedExists) {
      this.selectedCategory = this.filteredCategoryGroups[0].category;
    }

    this.activeGroup =
      this.filteredCategoryGroups.find(
        (group) => group.category === this.selectedCategory,
      ) ?? this.filteredCategoryGroups[0];
    this.visibleResults = this.activeGroup.results;
  }

  private matchesSeverity(result: ValidationResult): boolean {
    if (this.severityFilter === 'errors') {
      return result.isError;
    }

    if (this.severityFilter === 'warnings') {
      return !result.isError;
    }

    return true;
  }

  private buildRuleOptions(results: ValidationResult[]): RuleFilterOption[] {
    const options = new Map<string, RuleFilterOption>();

    for (const result of results) {
      const existing = options.get(result.ruleName);
      if (existing) {
        existing.count++;
        continue;
      }

      options.set(result.ruleName, {
        ruleName: result.ruleName,
        displayName:
          RULE_METADATA[result.ruleName]?.displayName || result.ruleName,
        count: 1,
      });
    }

    return Array.from(options.values()).sort((a, b) =>
      a.displayName.localeCompare(b.displayName),
    );
  }

  private pruneHiddenRules(): void {
    const validRuleNames = new Set(
      this.ruleOptions.map((rule) => rule.ruleName),
    );

    for (const ruleName of Array.from(this.hiddenRuleNames)) {
      if (!validRuleNames.has(ruleName)) {
        this.hiddenRuleNames.delete(ruleName);
      }
    }
  }

  readonly iconClasses: Record<string, string> = {
    formatting: 'bg-academic-gold/15 text-academic-gold',
    layout: 'bg-academic-blue/15 text-academic-blue',
    structure: 'bg-academic-green/15 text-academic-green',
    language: 'bg-academic-burgundy/15 text-academic-burgundy',
  };
}
