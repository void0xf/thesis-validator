import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-footer',
  standalone: true,
  template: `
    <footer class="border-t border-parchment-300/50 bg-white/50 backdrop-blur-sm">
      <div class="max-w-5xl mx-auto px-6 py-6">
        <div class="flex flex-col md:flex-row items-center justify-between gap-4">
          <p class="font-sans text-sm text-ink-500">
            Thesis Validator - Academic Document Verification System
          </p>
          <p class="font-mono text-xs text-ink-400">
            Powered by University Formatting Standards
          </p>
        </div>
      </div>
    </footer>
  `,
  styles: [`
    :host {
      display: block;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FooterComponent {}
