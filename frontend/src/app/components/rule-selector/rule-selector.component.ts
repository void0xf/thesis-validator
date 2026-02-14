import {
  Component,
  EventEmitter,
  Output,
  Input,
  signal,
  computed,
  OnInit,
  OnChanges,
  SimpleChanges,
  inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { ValidationService } from '../../services/validation.service';
import {
  ValidationRule,
  RuleCategory,
  RULE_METADATA,
  CATEGORY_INFO,
} from '../../models/validation.models';

@Component({
  selector: 'app-rule-selector',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  template: `
    <div class="paper-card p-6 animate-slide-up animate-delay-100">
      <div class="flex flex-wrap items-start justify-between gap-3 mb-5">
        <div>
          <h2 class="font-display text-xl font-semibold text-ink-900">
            Validation Rules
          </h2>
          <p class="font-sans text-xs text-ink-500 mt-1">
            {{ selectedRules().length }} of {{ rules().length }} selected
          </p>
        </div>

        <div class="flex items-center gap-2">
          <button
            type="button"
            class="flex items-center gap-2 px-3 py-1.5 rounded-lg text-sm font-sans transition-all duration-200"
            [class]="
              allSelected()
                ? 'bg-academic-green/10 text-academic-green border border-academic-green/20'
                : 'bg-parchment-100 text-ink-600 border border-parchment-300 hover:bg-parchment-200'
            "
            (click)="toggleAll()"
          >
            <lucide-icon
              [name]="allSelected() ? 'check-check' : 'check'"
              class="w-4 h-4"
            ></lucide-icon>
            {{ allSelected() ? 'Deselect All' : 'Select All' }}
          </button>
        </div>
      </div>

      @if (loading()) {
        <div class="space-y-3">
          @for (i of [1, 2, 3, 4]; track i) {
            <div
              class="h-16 bg-parchment-100/50 rounded-lg animate-pulse"
            ></div>
          }
        </div>
      } @else {
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-2 mb-4">
          @for (category of categories(); track category) {
            <button
              type="button"
              class="w-full text-left rounded-lg border px-3.5 py-3 transition-colors"
              [class.bg-parchment-100]="activeCategory() === category"
              [class.border-parchment-400]="activeCategory() === category"
              [class.text-ink-900]="activeCategory() === category"
              [class.bg-white]="activeCategory() !== category"
              [class.border-parchment-300]="activeCategory() !== category"
              [class.text-ink-700]="activeCategory() !== category"
              [class.hover:bg-parchment-50]="activeCategory() !== category"
              (click)="setActiveCategory(category)"
            >
              <div class="flex items-center justify-between gap-2">
                <div class="flex items-center gap-2 min-w-0">
                  <div
                    class="w-7 h-7 rounded-md flex items-center justify-center"
                    [class]="getCategoryIconClass(category)"
                  >
                    <lucide-icon
                      [name]="getCategoryIcon(category)"
                      class="w-4 h-4"
                    ></lucide-icon>
                  </div>
                  <span class="font-sans text-sm font-medium truncate">{{
                    getCategoryDisplayName(category)
                  }}</span>
                </div>
                <span class="font-mono text-[11px] text-ink-500">
                  {{ getSelectedCountForCategory(category) }}/{{
                    getRulesForCategory(category).length
                  }}
                </span>
              </div>
            </button>
          }
        </div>

        @if (activeCategory()) {
          <div
            class="rounded-xl border border-parchment-300/60 overflow-hidden bg-white/50"
          >
            <div
              class="px-4 py-3 border-b border-parchment-200/60 bg-gradient-to-r from-parchment-100/80 to-transparent"
            >
              <div class="flex items-center justify-between gap-2">
                <div class="flex items-center gap-2 min-w-0">
                  <div
                    class="w-8 h-8 rounded-lg flex items-center justify-center"
                    [class]="getCategoryIconClass(activeCategory()!)"
                  >
                    <lucide-icon
                      [name]="getCategoryIcon(activeCategory()!)"
                      class="w-4 h-4"
                    ></lucide-icon>
                  </div>
                  <span class="font-sans font-medium text-ink-800 truncate">
                    {{ getCategoryDisplayName(activeCategory()!) }}
                  </span>
                </div>
                <span class="badge-info text-xs">
                  {{ getSelectedCountForCategory(activeCategory()!) }}/{{
                    getRulesForCategory(activeCategory()!).length
                  }}
                </span>
              </div>
            </div>

            <div class="p-2 space-y-1.5">
              @for (
                rule of getRulesForCategory(activeCategory()!);
                track rule.name
              ) {
                <label
                  class="group flex items-start gap-3 p-3 rounded-lg cursor-pointer border transition-colors"
                  [class]="
                    rule.enabled
                      ? 'bg-academic-green/5 border-academic-green/20'
                      : 'bg-transparent border-transparent hover:bg-parchment-100/50 hover:border-parchment-300/40'
                  "
                >
                  <div class="relative flex-shrink-0 mt-0.5">
                    <input
                      type="checkbox"
                      [checked]="rule.enabled"
                      (change)="toggleRule(rule)"
                      class="sr-only"
                    />
                    <div
                      class="w-5 h-5 rounded-md border-2 transition-all duration-200 flex items-center justify-center"
                      [class]="
                        rule.enabled
                          ? 'bg-academic-green border-academic-green'
                          : 'border-ink-300 bg-white group-hover:border-ink-400'
                      "
                    >
                      @if (rule.enabled) {
                        <lucide-icon
                          name="check"
                          class="w-3.5 h-3.5 text-white"
                        ></lucide-icon>
                      }
                    </div>
                  </div>

                  <div class="flex-1 min-w-0">
                    <p
                      class="font-sans font-medium text-ink-800 text-sm truncate"
                    >
                      {{ rule.displayName }}
                    </p>
                    <p
                      class="font-body text-xs text-ink-500 mt-1 leading-relaxed"
                    >
                      {{ rule.description }}
                    </p>
                  </div>
                </label>
              }
            </div>
          </div>
        }

        <div
          class="flex items-start gap-2 mt-4 p-3 rounded-lg bg-academic-blue/5 border border-academic-blue/10"
        >
          <lucide-icon
            name="info"
            class="w-4 h-4 text-academic-blue flex-shrink-0 mt-0.5"
          ></lucide-icon>
          <p class="font-sans text-xs text-academic-blue/80 leading-relaxed">
            Selected rules will be applied during validation. Deselect rules you
            want to skip.
          </p>
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
export class RuleSelectorComponent implements OnInit, OnChanges {
  @Input() selectedRuleNames: string[] | null = null;
  @Input() syncKey = 0;
  @Output() rulesChange = new EventEmitter<string[]>();
  @Output() selectionCountChange = new EventEmitter<{
    selected: number;
    total: number;
  }>();

  private readonly validationService = inject(ValidationService);

  loading = signal(true);
  rules = signal<ValidationRule[]>([]);
  activeCategory = signal<RuleCategory | null>(null);
  private pendingApplyFromInput = false;

  categories = computed(() => {
    const cats = [...new Set(this.rules().map((r) => r.category))];
    return cats.sort((a, b) => CATEGORY_INFO[a].order - CATEGORY_INFO[b].order);
  });

  allSelected = computed(() => this.rules().every((r) => r.enabled));

  selectedRules = computed(() =>
    this.rules()
      .filter((r) => r.enabled)
      .map((r) => r.name),
  );

  ngOnInit(): void {
    this.loadRules();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['syncKey'] && !changes['syncKey'].firstChange) {
      if (this.rules().length === 0) {
        this.pendingApplyFromInput = true;
        return;
      }
      this.applySelectionFromInput();
    }
  }

  private loadRules(): void {
    this.validationService.getRules().subscribe({
      next: (response) => {
        const rules: ValidationRule[] = response.rules.map((r) => {
          const metadata = RULE_METADATA[r.name] || {
            displayName: r.name,
            description: 'Validates document formatting',
            category: 'formatting' as RuleCategory,
          };
          return {
            name: r.name,
            displayName: metadata.displayName,
            description: metadata.description,
            category: metadata.category,
            enabled: true,
          };
        });
        this.rules.set(rules);
        this.ensureActiveCategory();
        this.loading.set(false);
        if (this.pendingApplyFromInput) {
          this.pendingApplyFromInput = false;
          this.applySelectionFromInput();
        } else {
          this.emitSelectedRules();
        }
      },
      error: () => {
        // Use default rules if service is unavailable
        const defaultRules: ValidationRule[] = Object.entries(
          RULE_METADATA,
        ).map(([name, meta]) => ({
          name,
          displayName: meta.displayName,
          description: meta.description,
          category: meta.category,
          enabled: true,
        }));
        this.rules.set(defaultRules);
        this.ensureActiveCategory();
        this.loading.set(false);
        if (this.pendingApplyFromInput) {
          this.pendingApplyFromInput = false;
          this.applySelectionFromInput();
        } else {
          this.emitSelectedRules();
        }
      },
    });
  }

  getRulesForCategory(category: RuleCategory): ValidationRule[] {
    return this.rules().filter((r) => r.category === category);
  }

  getSelectedCountForCategory(category: RuleCategory): number {
    return this.getRulesForCategory(category).filter((r) => r.enabled).length;
  }

  getCategoryDisplayName(category: RuleCategory): string {
    return CATEGORY_INFO[category]?.displayName || category;
  }

  getCategoryIcon(category: RuleCategory): string {
    return CATEGORY_INFO[category]?.icon || 'check';
  }

  getCategoryIconClass(category: RuleCategory): string {
    const classes: Record<RuleCategory, string> = {
      formatting: 'bg-academic-gold/10 text-academic-gold',
      layout: 'bg-academic-blue/10 text-academic-blue',
      structure: 'bg-academic-green/10 text-academic-green',
      language: 'bg-academic-burgundy/10 text-academic-burgundy',
    };
    return classes[category] || 'bg-ink-100 text-ink-600';
  }

  setActiveCategory(category: RuleCategory): void {
    this.activeCategory.set(category);
  }

  toggleRule(rule: ValidationRule): void {
    const updated = this.rules().map((r) =>
      r.name === rule.name ? { ...r, enabled: !r.enabled } : r,
    );
    this.rules.set(updated);
    this.emitSelectedRules();
  }

  toggleAll(): void {
    const newState = !this.allSelected();
    const updated = this.rules().map((r) => ({ ...r, enabled: newState }));
    this.rules.set(updated);
    this.emitSelectedRules();
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

  private applySelectionFromInput(): void {
    if (!this.selectedRuleNames) {
      return;
    }

    const selected = new Set(this.selectedRuleNames);
    const updated = this.rules().map((rule) => ({
      ...rule,
      enabled: selected.has(rule.name),
    }));
    this.rules.set(updated);
    this.emitSelectedRules();
  }

  private emitSelectedRules(): void {
    const selected = this.selectedRules();
    this.rulesChange.emit(selected);
    this.selectionCountChange.emit({
      selected: selected.length,
      total: this.rules().length,
    });
  }
}
