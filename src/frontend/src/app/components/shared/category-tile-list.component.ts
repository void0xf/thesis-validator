import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { RuleCategory } from '../../models/validation.models';

export interface CategoryTileItem {
  category: RuleCategory;
  displayName: string;
  icon: string;
  iconClass: string;
  countLabel?: string;
  detail?: string;
}

@Component({
  selector: 'app-category-tile-list',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="grid grid-cols-1 gap-2"
      [class.sm:grid-cols-2]="columns() === 'two' || columns() === 'four'"
      [class.lg:grid-cols-4]="columns() === 'four'"
      [class.mb-4]="withBottomMargin()"
    >
      @for (item of items(); track item.category) {
        <button
          type="button"
          class="w-full text-left rounded-lg border transition-colors"
          [class.px-3.5]="size() === 'compact'"
          [class.py-3]="size() === 'compact'"
          [class.px-4]="size() === 'comfortable'"
          [class.py-3]="size() === 'comfortable'"
          [class.bg-parchment-100]="selectedCategory() === item.category"
          [class.border-parchment-400]="selectedCategory() === item.category"
          [class.text-ink-900]="selectedCategory() === item.category"
          [class.bg-white]="selectedCategory() !== item.category"
          [class.border-parchment-300]="selectedCategory() !== item.category"
          [class.text-ink-700]="selectedCategory() !== item.category"
          [class.hover:bg-parchment-50]="selectedCategory() !== item.category"
          (click)="categorySelected.emit(item.category)"
        >
          <div class="flex items-start justify-between gap-3 p-2">
            <div class="flex items-center gap-2 min-w-0">
              <div
                class="w-7 h-7 rounded-md flex items-center justify-center"
                [class]="item.iconClass"
              >
                <lucide-icon [name]="item.icon" class="w-4 h-4"></lucide-icon>
              </div>
              <div class="min-w-0">
                <p class="font-sans text-sm font-medium truncate">
                  {{ item.displayName }}
                </p>
                @if (item.detail) {
                  <p class="font-sans text-xs text-ink-500">
                    {{ item.detail }}
                  </p>
                }
              </div>
            </div>

            @if (item.countLabel) {
              <span class="font-mono text-[11px] text-ink-500">
                {{ item.countLabel }}
              </span>
            }
          </div>
        </button>
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
export class CategoryTileListComponent {
  readonly items = input.required<readonly CategoryTileItem[]>();
  readonly selectedCategory = input<RuleCategory | null>(null);
  readonly columns = input<'two' | 'four'>('two');
  readonly size = input<'compact' | 'comfortable'>('compact');
  readonly withBottomMargin = input(false);
  readonly categorySelected = output<RuleCategory>();
}
