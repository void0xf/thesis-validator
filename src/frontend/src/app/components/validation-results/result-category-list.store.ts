import { computed, signal } from '@angular/core';
import {
  CategoryGroup,
  RuleCategory,
  ValidationResult,
  ValidationRule,
} from '../../models/validation.models';
import {
  buildRuleLookup,
  getRuleCategoryIconClass,
  getRuleDisplayName,
} from '../../models/validation-display.models';
import { CategoryTileItem } from '../shared/category-tile-list.component';
import {
  RuleFilterOption,
  RuleVisibilityChange,
  SeverityFilter,
} from './result-filter.models';

export class ResultCategoryListStore {
  readonly categoryGroups = signal<readonly CategoryGroup[]>([]);
  readonly ruleCatalog = signal<readonly ValidationRule[]>([]);
  readonly severityFilter = signal<SeverityFilter>('all');
  readonly rulePanelOpen = signal(false);
  readonly selectedCategory = signal<RuleCategory | null>(null);
  readonly hiddenRuleNames = signal<ReadonlySet<string>>(new Set());

  private readonly ruleLookup = computed(() =>
    buildRuleLookup(this.ruleCatalog()),
  );

  readonly allResults = computed(() =>
    this.categoryGroups().flatMap((group) => group.results),
  );
  readonly totalIssueCount = computed(() => this.allResults().length);
  readonly ruleOptions = computed(() =>
    this.buildRuleOptions(this.allResults()),
  );
  readonly hiddenRuleOptions = computed(() =>
    this.ruleOptions().filter((rule) =>
      this.hiddenRuleNames().has(rule.ruleName),
    ),
  );
  readonly totalRuleFilterCount = computed(() => this.ruleOptions().length);
  readonly hiddenRuleCount = computed(() => this.hiddenRuleOptions().length);
  readonly visibleRuleCount = computed(
    () => this.totalRuleFilterCount() - this.hiddenRuleCount(),
  );
  readonly ruleVisibleResults = computed(() =>
    this.allResults().filter((result) => this.isRuleVisible(result.ruleName)),
  );
  readonly ruleVisibleIssueCount = computed(
    () => this.ruleVisibleResults().length,
  );
  readonly totalErrorCount = computed(
    () =>
      this.ruleVisibleResults().filter((result) => result.isError).length,
  );
  readonly totalWarningCount = computed(
    () => this.ruleVisibleIssueCount() - this.totalErrorCount(),
  );
  readonly filteredCategoryGroups = computed(() =>
    this.categoryGroups()
      .map((group) => this.filterCategoryGroup(group))
      .filter((group) => group.results.length > 0),
  );
  readonly filteredIssueCount = computed(() =>
    this.filteredCategoryGroups().reduce(
      (total, group) => total + group.results.length,
      0,
    ),
  );
  readonly activeGroup = computed(() => {
    const groups = this.filteredCategoryGroups();

    if (!groups.length) {
      return null;
    }

    return (
      groups.find((group) => group.category === this.selectedCategory()) ??
      groups[0]
    );
  });
  readonly selectedCategoryForView = computed(
    () => this.activeGroup()?.category ?? this.selectedCategory(),
  );
  readonly categoryTiles = computed<CategoryTileItem[]>(() =>
    this.filteredCategoryGroups().map((group) => ({
      category: group.category,
      displayName: group.displayName,
      icon: group.icon,
      iconClass: getRuleCategoryIconClass(group.category, 'solid'),
      detail: `${group.results.length} issue${
        group.results.length !== 1 ? 's' : ''
      }`,
    })),
  );
  readonly hasActiveFilters = computed(
    () => this.severityFilter() !== 'all' || this.hiddenRuleCount() > 0,
  );

  setInputs(
    categoryGroups: readonly CategoryGroup[],
    ruleCatalog: readonly ValidationRule[],
  ): void {
    this.categoryGroups.set(categoryGroups);
    this.ruleCatalog.set(ruleCatalog);
  }

  setSeverityFilter(filter: SeverityFilter): void {
    if (this.severityFilter() !== filter) {
      this.severityFilter.set(filter);
    }
  }

  toggleRulePanel(): void {
    this.rulePanelOpen.update((isOpen) => !isOpen);
  }

  setRuleVisibility(change: RuleVisibilityChange): void {
    this.updateHiddenRules((hiddenRules) => {
      if (change.visible) {
        hiddenRules.delete(change.ruleName);
      } else {
        hiddenRules.add(change.ruleName);
      }
    });
  }

  showRule(ruleName: string): void {
    this.updateHiddenRules((hiddenRules) => hiddenRules.delete(ruleName));
  }

  showAllRules(): void {
    this.hiddenRuleNames.set(new Set());
  }

  clearFilters(): void {
    this.severityFilter.set('all');
    this.hiddenRuleNames.set(new Set());
  }

  selectCategory(category: RuleCategory): void {
    this.selectedCategory.set(category);
  }

  private isRuleVisible(ruleName: string): boolean {
    return !this.hiddenRuleNames().has(ruleName);
  }

  private updateHiddenRules(
    update: (hiddenRules: Set<string>) => void,
  ): void {
    this.hiddenRuleNames.update((current) => {
      const next = new Set(current);
      update(next);
      return next;
    });
  }

  private filterCategoryGroup(group: CategoryGroup): CategoryGroup {
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
  }

  private matchesSeverity(result: ValidationResult): boolean {
    if (this.severityFilter() === 'errors') {
      return result.isError;
    }

    if (this.severityFilter() === 'warnings') {
      return !result.isError;
    }

    return true;
  }

  private buildRuleOptions(
    results: readonly ValidationResult[],
  ): RuleFilterOption[] {
    const options = new Map<string, RuleFilterOption>();

    for (const result of results) {
      const existing = options.get(result.ruleName);
      if (existing) {
        existing.count++;
        continue;
      }

      options.set(result.ruleName, {
        ruleName: result.ruleName,
        displayName: getRuleDisplayName(result.ruleName, this.ruleLookup()),
        count: 1,
      });
    }

    return Array.from(options.values()).sort((a, b) =>
      a.displayName.localeCompare(b.displayName),
    );
  }
}
