import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { ValidationRule } from '../../models/validation.models';
import { RuleCheckboxComponent } from './rule-checkbox.component';

@Component({
  selector: 'app-rule-category-group',
  standalone: true,
  imports: [CommonModule, LucideAngularModule, RuleCheckboxComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="rounded-xl border border-parchment-300/60 overflow-hidden bg-white/50">
      <button
        type="button"
        class="w-full flex items-center justify-between px-4 py-3 bg-gradient-to-r from-parchment-100/80 to-transparent hover:from-parchment-200/80 transition-colors"
        (click)="toggleExpanded.emit()"
      >
        <div class="flex items-center gap-3">
          <div class="w-8 h-8 rounded-lg flex items-center justify-center"
               [class]="iconClass">
            <lucide-icon [name]="icon" class="w-4 h-4"></lucide-icon>
          </div>
          <span class="font-sans font-medium text-ink-800">
            {{ categoryName }}
          </span>
          <span class="badge-info text-xs">
            {{ selectedCount }}/{{ rules.length }}
          </span>
        </div>
        <lucide-icon
          name="chevron-down"
          class="w-5 h-5 text-ink-400 transition-transform duration-200"
          [class.rotate-180]="expanded"
        ></lucide-icon>
      </button>

      @if (expanded) {
        <div class="px-4 py-3 space-y-2 border-t border-parchment-200/60 animate-slide-down">
          @for (rule of rules; track rule.name) {
            <app-rule-checkbox
              [rule]="rule"
              (toggle)="ruleToggled.emit($event)"
            />
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
export class RuleCategoryGroupComponent {
  @Input({ required: true }) categoryName!: string;
  @Input({ required: true }) icon!: string;
  @Input({ required: true }) iconClass!: string;
  @Input({ required: true }) rules!: ValidationRule[];
  @Input({ required: true }) selectedCount!: number;
  @Input({ required: true }) expanded!: boolean;
  @Output() toggleExpanded = new EventEmitter<void>();
  @Output() ruleToggled = new EventEmitter<ValidationRule>();
}
