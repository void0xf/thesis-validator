import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-result-stats-grid',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="grid grid-cols-3 gap-px bg-parchment-200/50">
      <div class="bg-white/80 px-6 py-4 text-center">
        <p class="font-mono text-3xl font-bold text-ink-900">{{ errors }}</p>
        <p class="font-sans text-xs text-ink-500 uppercase tracking-wide mt-1">Errors</p>
      </div>
      <div class="bg-white/80 px-6 py-4 text-center">
        <p class="font-mono text-3xl font-bold text-ink-900">{{ warnings }}</p>
        <p class="font-sans text-xs text-ink-500 uppercase tracking-wide mt-1">Warnings</p>
      </div>
      <div class="bg-white/80 px-6 py-4 text-center">
        <p class="font-mono text-3xl font-bold text-ink-900">{{ categoryCount }}</p>
        <p class="font-sans text-xs text-ink-500 uppercase tracking-wide mt-1">Categories</p>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class ResultStatsGridComponent {
  @Input({ required: true }) errors!: number;
  @Input({ required: true }) warnings!: number;
  @Input({ required: true }) categoryCount!: number;
}
