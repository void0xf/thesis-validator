import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject } from 'rxjs';
import { ValidationService } from '../../services/validation.service';
import {
  RuleCategory,
  ValidationRule,
} from '../../models/validation.models';
import {
  compareRuleCategories,
  getRuleCategoryDisplay,
  getRuleCategoryIconClass,
  toValidationRule,
} from '../../models/validation-display.models';
import { CategoryTileItem } from '../shared/category-tile-list.component';

@Injectable()
export class RuleSelectorStore {
  private readonly validationService = inject(ValidationService);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(true);
  readonly rules = signal<ValidationRule[]>([]);
  readonly activeCategory = signal<RuleCategory | null>(null);
  readonly stateChanges = new Subject<void>();

  readonly categories = computed(() => {
    const categories = [...new Set(this.rules().map((rule) => rule.category))];
    return categories.sort(compareRuleCategories);
  });
  readonly rulesByCategory = computed(() => {
    const groups = new Map<RuleCategory, ValidationRule[]>();

    for (const rule of this.rules()) {
      const categoryRules = groups.get(rule.category) ?? [];
      categoryRules.push(rule);
      groups.set(rule.category, categoryRules);
    }

    return groups;
  });
  readonly categoryTiles = computed<CategoryTileItem[]>(() =>
    this.categories().map((category) => {
      const categoryDisplay = getRuleCategoryDisplay(category);
      const rules = this.getRulesForCategory(category);

      return {
        category,
        displayName: categoryDisplay.displayName,
        icon: categoryDisplay.icon,
        iconClass: getRuleCategoryIconClass(category),
        countLabel: `${this.countEnabledRules(rules)}/${rules.length}`,
      };
    }),
  );
  readonly allSelected = computed(
    () => this.rules().length > 0 && this.rules().every((rule) => rule.enabled),
  );
  readonly selectedRules = computed(() =>
    this.rules()
      .filter((rule) => rule.enabled)
      .map((rule) => rule.name),
  );

  private pendingSelectedRuleNames: string[] | null | undefined;

  constructor() {
    this.loadRules();
  }

  getRulesForCategory(category: RuleCategory): ValidationRule[] {
    return this.rulesByCategory().get(category) ?? [];
  }

  getSelectedCountForCategory(category: RuleCategory): number {
    return this.countEnabledRules(this.getRulesForCategory(category));
  }

  setActiveCategory(category: RuleCategory): void {
    this.activeCategory.set(category);
  }

  toggleRule(rule: ValidationRule): void {
    this.rules.update((rules) =>
      rules.map((currentRule) =>
        currentRule.name === rule.name
          ? { ...currentRule, enabled: !currentRule.enabled }
          : currentRule,
      ),
    );
    this.stateChanges.next();
  }

  toggleAll(): void {
    if (this.rules().length === 0) {
      return;
    }

    const enabled = !this.allSelected();
    this.rules.update((rules) =>
      rules.map((rule) => ({ ...rule, enabled })),
    );
    this.stateChanges.next();
  }

  syncSelectionFromInput(selectedRuleNames: string[] | null): void {
    if (this.loading()) {
      this.pendingSelectedRuleNames = selectedRuleNames;
      return;
    }

    this.applySelectionFromInput(selectedRuleNames);
    this.stateChanges.next();
  }

  private loadRules(): void {
    this.validationService
      .getRules()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          const rules = Array.isArray(response.rules)
            ? response.rules.map(toValidationRule)
            : [];
          this.setLoadedRules(rules);
        },
        error: () => this.setLoadedRules([]),
      });
  }

  private setLoadedRules(rules: ValidationRule[]): void {
    this.rules.set(rules);
    this.ensureActiveCategory();
    this.loading.set(false);

    if (this.pendingSelectedRuleNames !== undefined) {
      const selectedRuleNames = this.pendingSelectedRuleNames;
      this.pendingSelectedRuleNames = undefined;
      this.applySelectionFromInput(selectedRuleNames);
    }

    this.stateChanges.next();
  }

  private ensureActiveCategory(): void {
    const categories = this.categories();
    const current = this.activeCategory();

    if (!categories.length) {
      this.activeCategory.set(null);
      return;
    }

    if (!current || !categories.includes(current)) {
      this.activeCategory.set(categories[0]);
    }
  }

  private applySelectionFromInput(selectedRuleNames: string[] | null): void {
    if (!selectedRuleNames) {
      return;
    }

    const selected = new Set(
      selectedRuleNames.map((ruleName) => ruleName.toLowerCase()),
    );
    this.rules.update((rules) =>
      rules.map((rule) => ({
        ...rule,
        enabled: selected.has(rule.name.toLowerCase()),
      })),
    );
  }

  private countEnabledRules(rules: readonly ValidationRule[]): number {
    return rules.filter((rule) => rule.enabled).length;
  }
}
