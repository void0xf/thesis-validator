import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { CategoryGroup } from '../../models/validation.models';
import { ResultItemComponent } from './result-item.component';

@Component({
  selector: 'app-result-category-group',
  standalone: true,
  imports: [CommonModule, LucideAngularModule, ResultItemComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="paper-card overflow-hidden animate-slide-up"
      [style.animation-delay]="animationDelay"
    >
      <button
        type="button"
        class="w-full flex items-center justify-between px-5 py-4 hover:bg-parchment-100/50 transition-colors"
        (click)="toggleExpanded.emit(group)"
      >
        <div class="flex items-center gap-4">
          <div class="w-10 h-10 rounded-xl flex items-center justify-center"
               [class]="iconClass">
            <lucide-icon [name]="group.icon" class="w-5 h-5"></lucide-icon>
          </div>
          <div class="text-left">
            <h4 class="font-sans font-semibold text-ink-900">
              {{ group.displayName }}
            </h4>
            <p class="font-sans text-xs text-ink-500">
              {{ group.results.length }} issue{{ group.results.length !== 1 ? 's' : '' }} found
            </p>
          </div>
        </div>

        <div class="flex items-center gap-3">
          @if (group.errorCount > 0) {
            <span class="badge-error">
              {{ group.errorCount }} error{{ group.errorCount !== 1 ? 's' : '' }}
            </span>
          }
          @if (group.warningCount > 0) {
            <span class="badge-warning">
              {{ group.warningCount }} warning{{ group.warningCount !== 1 ? 's' : '' }}
            </span>
          }
          <lucide-icon
            name="chevron-down"
            class="w-5 h-5 text-ink-400 transition-transform duration-200"
            [class.rotate-180]="group.expanded"
          ></lucide-icon>
        </div>
      </button>

      @if (group.expanded) {
        <div class="border-t border-parchment-200/60 animate-slide-down">
          @for (result of group.results; track $index) {
            <app-result-item [result]="result" />
          }
        </div>
      }
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class ResultCategoryGroupComponent {
  @Input({ required: true }) group!: CategoryGroup;
  @Input({ required: true }) iconClass!: string;
  @Input() animationDelay = '0ms';
  @Output() toggleExpanded = new EventEmitter<CategoryGroup>();
}
