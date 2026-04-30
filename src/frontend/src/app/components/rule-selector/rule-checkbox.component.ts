import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { ValidationRule } from '../../models/validation.models';

@Component({
  selector: 'app-rule-checkbox',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <label
      class="group flex items-start gap-3 p-3 rounded-lg cursor-pointer transition-all duration-200"
      [class]="rule.enabled
        ? 'bg-academic-green/5 border border-academic-green/20'
        : 'bg-transparent border border-transparent hover:bg-parchment-100/50 hover:border-parchment-300/40'"
    >
      <div class="relative flex-shrink-0 mt-0.5">
        <input
          type="checkbox"
          [checked]="rule.enabled"
          (change)="toggle.emit(rule)"
          class="sr-only peer"
        />
        <div class="w-5 h-5 rounded-md border-2 transition-all duration-200 flex items-center justify-center"
             [class]="rule.enabled
               ? 'bg-academic-green border-academic-green'
               : 'border-ink-300 bg-white group-hover:border-ink-400'">
          @if (rule.enabled) {
            <lucide-icon name="check" class="w-3.5 h-3.5 text-white"></lucide-icon>
          }
        </div>
      </div>
      <div class="flex-1 min-w-0">
        <p class="font-sans font-medium text-ink-800 text-sm">
          {{ rule.displayName }}
        </p>
        <p class="font-body text-xs text-ink-500 mt-0.5 leading-relaxed">
          {{ rule.description }}
        </p>
      </div>
    </label>
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class RuleCheckboxComponent {
  @Input({ required: true }) rule!: ValidationRule;
  @Output() toggle = new EventEmitter<ValidationRule>();
}
