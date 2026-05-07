import {
  CategoryGroup,
  RuleCategory,
  ValidationResponse,
  ValidationRule,
} from '../../models/validation.models';
import {
  compareRuleCategories,
  getRuleCategoryDisplay,
  normalizeRuleCategory,
} from '../../models/validation-display.models';

export function normalizeValidationResultsResponse(
  value: ValidationResponse,
): ValidationResponse {
  const results = Array.isArray(value.results) ? value.results : [];
  const totalErrors =
    value.totalErrors ?? results.filter((result) => result.isError).length;
  const totalWarnings =
    value.totalWarnings ?? results.filter((result) => !result.isError).length;

  return {
    ...value,
    results,
    totalErrors,
    totalWarnings,
    isValid: value.isValid ?? totalErrors === 0,
  };
}

export function buildCategoryGroups(
  results: ValidationResponse['results'],
  ruleLookup: ReadonlyMap<string, ValidationRule>,
): CategoryGroup[] {
  const groups: Map<RuleCategory, CategoryGroup> = new Map();

  for (const result of results) {
    const catalogRule = ruleLookup.get(result.ruleName.toLowerCase());
    const category =
      normalizeRuleCategory(result.category) ||
      catalogRule?.category ||
      'formatting';

    if (!groups.has(category)) {
      groups.set(category, createCategoryGroup(category));
    }

    addResultToGroup(groups.get(category)!, result);
  }

  return Array.from(groups.values()).sort((a, b) =>
    compareRuleCategories(a.category, b.category),
  );
}

function createCategoryGroup(category: RuleCategory): CategoryGroup {
  const categoryDisplay = getRuleCategoryDisplay(category);

  return {
    category,
    displayName: categoryDisplay.displayName,
    icon: categoryDisplay.icon,
    results: [],
    errorCount: 0,
    warningCount: 0,
  };
}

function addResultToGroup(
  group: CategoryGroup,
  result: ValidationResponse['results'][number],
): void {
  group.results.push(result);

  if (result.isError) {
    group.errorCount++;
  } else {
    group.warningCount++;
  }
}
