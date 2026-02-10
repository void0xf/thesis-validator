import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { ValidationResult, RULE_METADATA } from '../../models/validation.models';

@Component({
  selector: 'app-result-item',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="px-5 py-4 border-b border-parchment-100 last:border-b-0 hover:bg-parchment-50/50 transition-colors">
      <div class="flex items-start gap-3">
        <div class="flex-shrink-0 mt-1">
          @if (result.isError) {
            <div class="w-2 h-2 rounded-full bg-academic-red"></div>
          } @else {
            <div class="w-2 h-2 rounded-full bg-academic-gold"></div>
          }
        </div>

        <div class="flex-1 min-w-0">
          <div class="flex items-start justify-between gap-4">
            <div>
              <span class="font-mono text-xs px-2 py-0.5 rounded bg-ink-100 text-ink-600">
                {{ getRuleDisplayName(result.ruleName) }}
              </span>
              <p class="font-body text-ink-800 mt-2 leading-relaxed">
                {{ result.message }}
              </p>
            </div>
          </div>

          @if (result.location) {
            <div class="mt-3 flex flex-wrap items-center gap-x-4 gap-y-2">
              <div class="flex items-center gap-1.5 text-ink-500">
                <lucide-icon name="map-pin" class="w-3.5 h-3.5"></lucide-icon>
                <span class="font-mono text-xs">{{ result.location.description }}</span>
              </div>

              @if (result.location.text) {
                <div class="flex items-center gap-1.5 text-ink-500">
                  <lucide-icon name="file-text" class="w-3.5 h-3.5"></lucide-icon>
                  <span class="font-mono text-xs truncate max-w-xs">
                    "{{ truncateText(result.location.text, 40) }}"
                  </span>
                </div>
              }
            </div>
          }
        </div>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class ResultItemComponent {
  @Input({ required: true }) result!: ValidationResult;

  getRuleDisplayName(ruleName: string): string {
    return RULE_METADATA[ruleName]?.displayName || ruleName;
  }

  truncateText(text: string, maxLength: number): string {
    if (text.length <= maxLength) return text;
    return text.substring(0, maxLength) + '...';
  }
}
