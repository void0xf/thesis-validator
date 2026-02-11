import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { HeadingInfo } from '../../models/validation.models';

@Component({
  selector: 'app-result-heading-hierarchy',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (headings.length > 0) {
      <div class="paper-card overflow-hidden animate-slide-up">
        <div class="px-5 py-4 flex items-center gap-4">
          <div class="w-10 h-10 rounded-xl flex items-center justify-center bg-academic-green/15 text-academic-green">
            <lucide-icon name="list-tree" class="w-5 h-5"></lucide-icon>
          </div>
          <div>
            <h4 class="font-sans font-semibold text-ink-900">Document Structure</h4>
            <p class="font-sans text-xs text-ink-500">
              {{ headings.length }} heading{{ headings.length !== 1 ? 's' : '' }} detected
            </p>
          </div>
        </div>

        <div class="border-t border-parchment-200/60 px-5 py-4">
          <div class="space-y-1">
            @for (heading of headings; track $index) {
              <div
                class="flex items-center gap-2 py-1.5 rounded-md transition-colors hover:bg-parchment-50/80"
                [style.padding-left.rem]="(heading.level - 1) * 1.25"
              >
                <span
                  class="flex-shrink-0 font-mono text-[10px] font-bold w-6 h-6 rounded-md flex items-center justify-center"
                  [class]="getLevelClass(heading.level)"
                >
                  H{{ heading.level }}
                </span>
                <span
                  class="font-sans text-sm text-ink-800 truncate"
                  [title]="heading.text"
                >
                  {{ heading.text }}
                </span>
              </div>
            }
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class ResultHeadingHierarchyComponent {
  @Input({ required: true }) headings!: HeadingInfo[];

  getLevelClass(level: number): string {
    const classes: Record<number, string> = {
      1: 'bg-academic-blue/15 text-academic-blue',
      2: 'bg-academic-green/15 text-academic-green',
      3: 'bg-academic-gold/15 text-academic-gold',
    };
    return classes[level] || 'bg-academic-red/15 text-academic-red';
  }
}
