import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ValidationResult,
  RULE_METADATA,
} from '../../models/validation.models';

@Component({
  selector: 'app-result-item',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="px-5 py-4 border-b border-parchment-100 last:border-b-0 hover:bg-parchment-50/50"
    >
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
              <span
                class="font-mono text-xs px-2 py-0.5 rounded bg-ink-100 text-ink-600"
              >
                {{ ruleDisplayName }}
              </span>
              <p class="font-body text-ink-800 mt-2 leading-relaxed">
                {{ result.message }}
              </p>
            </div>
          </div>

          @if (result.location) {
            <div class="mt-3 flex flex-wrap items-center gap-x-4 gap-y-2">
              @if (result.location.section) {
                <div class="flex items-center gap-1.5 text-ink-500">
                  <svg
                    class="w-3.5 h-3.5 flex-shrink-0"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="2"
                    stroke-linecap="round"
                    stroke-linejoin="round"
                  >
                    <path
                      d="m19 21-7-4-7 4V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2v16z"
                    />
                  </svg>
                  <span class="font-mono text-xs truncate max-w-xs">
                    <button
                      type="button"
                      class="text-left hover:text-ink-700 underline-offset-2 hover:underline transition-colors"
                      [title]="result.location.section + ' • Click to copy full text'"
                      (click)="copyFullText('section', result.location.section)"
                    >
                      {{ truncatedSection }}
                    </button>
                    @if (copiedField === 'section') {
                      <span class="ml-1 text-academic-green">Copied</span>
                    }
                  </span>
                </div>
              }

              @if (result.location.text) {
                <div class="flex items-center gap-1.5 text-ink-500">
                  <svg
                    class="w-3.5 h-3.5 flex-shrink-0"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    stroke-width="2"
                    stroke-linecap="round"
                    stroke-linejoin="round"
                  >
                    <path
                      d="M15 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7Z"
                    />
                    <path d="M14 2v4a2 2 0 0 0 2 2h4" />
                    <path d="M10 9H8" />
                    <path d="M16 13H8" />
                    <path d="M16 17H8" />
                  </svg>
                  <span class="font-mono text-xs truncate max-w-xs">
                    <button
                      type="button"
                      class="text-left hover:text-ink-700 underline-offset-2 hover:underline transition-colors"
                      [title]="result.location.text + ' • Click to copy full text'"
                      (click)="copyFullText('text', result.location.text)"
                    >
                      "{{ truncatedLocationText }}"
                    </button>
                    @if (copiedField === 'text') {
                      <span class="ml-1 text-academic-green">Copied</span>
                    }
                  </span>
                </div>
              }
            </div>
          }
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        contain: layout style;
      }
    `,
  ],
})
export class ResultItemComponent {
  private _result!: ValidationResult;
  ruleDisplayName = '';
  truncatedSection = '';
  truncatedLocationText = '';
  copiedField: 'section' | 'text' | null = null;
  private copiedResetTimeout: ReturnType<typeof setTimeout> | null = null;

  @Input({ required: true })
  set result(value: ValidationResult) {
    this._result = value;
    this.ruleDisplayName =
      RULE_METADATA[value.ruleName]?.displayName || value.ruleName;
    this.truncatedSection = value.location?.section
      ? this.truncate(value.location.section, 50)
      : '';
    this.truncatedLocationText = value.location?.text
      ? this.truncate(value.location.text, 40)
      : '';
  }

  get result(): ValidationResult {
    return this._result;
  }

  async copyFullText(kind: 'section' | 'text', fullText: string): Promise<void> {
    if (!fullText) {
      return;
    }

    const copied = await this.copyToClipboard(fullText);
    if (!copied) {
      return;
    }

    this.copiedField = kind;
    if (this.copiedResetTimeout !== null) {
      clearTimeout(this.copiedResetTimeout);
    }
    this.copiedResetTimeout = setTimeout(() => {
      this.copiedField = null;
    }, 1200);
  }

  private async copyToClipboard(text: string): Promise<boolean> {
    if (typeof navigator !== 'undefined' && navigator.clipboard?.writeText) {
      try {
        await navigator.clipboard.writeText(text);
        return true;
      } catch {
      }
    }

    try {
      const textarea = document.createElement('textarea');
      textarea.value = text;
      textarea.style.position = 'fixed';
      textarea.style.opacity = '0';
      document.body.appendChild(textarea);
      textarea.focus();
      textarea.select();
      const copied = document.execCommand('copy');
      document.body.removeChild(textarea);
      return copied;
    } catch {
      return false;
    }
  }

  private truncate(text: string, maxLength: number): string {
    if (text.length <= maxLength) return text;
    return text.substring(0, maxLength) + '...';
  }
}
