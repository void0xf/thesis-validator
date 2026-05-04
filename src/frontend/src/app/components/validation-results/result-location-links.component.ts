import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { DocumentLocation } from '../../models/validation.models';

@Component({
  selector: 'app-result-location-links',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (location(); as location) {
      <div class="mt-3 flex flex-wrap items-center gap-x-4 gap-y-2">
        @if (location.section) {
          <button
            type="button"
            class="flex min-w-0 items-center gap-1.5 text-ink-500 hover:text-ink-700 underline-offset-2 hover:underline transition-colors"
            [title]="location.section + ' - Click to copy full text'"
            (click)="copyFullText('section', location.section)"
          >
            <lucide-icon
              name="bookmark"
              class="w-3.5 h-3.5 flex-shrink-0"
            ></lucide-icon>
            <span class="font-mono text-xs truncate max-w-xs">
              {{ truncatedSection() }}
            </span>
            @if (copiedField() === 'section') {
              <span class="font-mono text-xs text-academic-green">Copied</span>
            }
          </button>
        }

        @if (location.text) {
          <button
            type="button"
            class="flex min-w-0 items-center gap-1.5 text-ink-500 hover:text-ink-700 underline-offset-2 hover:underline transition-colors"
            [title]="location.text + ' - Click to copy full text'"
            (click)="copyFullText('text', location.text)"
          >
            <lucide-icon
              name="file-text"
              class="w-3.5 h-3.5 flex-shrink-0"
            ></lucide-icon>
            <span class="font-mono text-xs truncate max-w-xs">
              "{{ truncatedLocationText() }}"
            </span>
            @if (copiedField() === 'text') {
              <span class="font-mono text-xs text-academic-green">Copied</span>
            }
          </button>
        }
      </div>
    }
  `,
})
export class ResultLocationLinksComponent {
  readonly location = input<DocumentLocation | null>(null);

  readonly copiedField = signal<'section' | 'text' | null>(null);
  readonly truncatedSection = computed(() =>
    this.truncate(this.location()?.section ?? '', 50),
  );
  readonly truncatedLocationText = computed(() =>
    this.truncate(this.location()?.text ?? '', 40),
  );

  private readonly destroyRef = inject(DestroyRef);
  private copiedResetTimeout: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    this.destroyRef.onDestroy(() => this.clearCopiedResetTimeout());
  }

  async copyFullText(
    kind: 'section' | 'text',
    fullText: string,
  ): Promise<void> {
    if (!fullText || !(await this.copyToClipboard(fullText))) {
      return;
    }

    this.copiedField.set(kind);
    this.clearCopiedResetTimeout();
    this.copiedResetTimeout = setTimeout(() => {
      this.copiedField.set(null);
      this.copiedResetTimeout = null;
    }, 1200);
  }

  private async copyToClipboard(text: string): Promise<boolean> {
    if (typeof navigator !== 'undefined' && navigator.clipboard?.writeText) {
      try {
        await navigator.clipboard.writeText(text);
        return true;
      } catch {}
    }

    return this.copyWithTextarea(text);
  }

  private copyWithTextarea(text: string): boolean {
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

  private clearCopiedResetTimeout(): void {
    if (this.copiedResetTimeout !== null) {
      clearTimeout(this.copiedResetTimeout);
      this.copiedResetTimeout = null;
    }
  }

  private truncate(text: string, maxLength: number): string {
    return text.length <= maxLength
      ? text
      : `${text.substring(0, maxLength)}...`;
  }
}
