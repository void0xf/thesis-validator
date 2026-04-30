import {
  Component,
  Input,
  ChangeDetectionStrategy,
  OnChanges,
  SimpleChanges,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { CategoryGroup } from '../../models/validation.models';
import { ResultItemComponent } from './result-item.component';

@Component({
  selector: 'app-result-category-list',
  standalone: true,
  imports: [CommonModule, LucideAngularModule, ResultItemComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="space-y-4">
      <h3 class="font-display text-xl font-semibold text-ink-900">
        Issues by Category
      </h3>

      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-2">
        @for (group of categoryGroups; track group.category) {
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

  selectedCategory: CategoryGroup['category'] | null = null;
  activeGroup: CategoryGroup | null = null;
  visibleResults: CategoryGroup['results'] = [];

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['categoryGroups']) {
      this.syncActiveCategory();
    }
  }

  selectCategory(group: CategoryGroup): void {
    if (this.selectedCategory === group.category) {
      return;
    }

    this.selectedCategory = group.category;
    this.syncActiveCategory();
  }

  private syncActiveCategory(): void {
    if (!this.categoryGroups.length) {
      this.activeGroup = null;
      this.visibleResults = [];
      return;
    }

    const selectedExists =
      this.selectedCategory !== null &&
      this.categoryGroups.some((g) => g.category === this.selectedCategory);

    if (!selectedExists) {
      this.selectedCategory = this.categoryGroups[0].category;
    }

    this.activeGroup =
      this.categoryGroups.find((g) => g.category === this.selectedCategory) ??
      this.categoryGroups[0];
    this.updateVisibleResults();
  }

  private updateVisibleResults(): void {
    this.visibleResults = this.activeGroup?.results ?? [];
  }

  readonly iconClasses: Record<string, string> = {
    formatting: 'bg-academic-gold/15 text-academic-gold',
    layout: 'bg-academic-blue/15 text-academic-blue',
    structure: 'bg-academic-green/15 text-academic-green',
    language: 'bg-academic-burgundy/15 text-academic-burgundy',
  };
}
