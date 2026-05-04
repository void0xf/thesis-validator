import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-category-panel-heading',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex items-center justify-between gap-2">
      <div class="flex items-center gap-2 min-w-0">
        <div
          class="rounded-lg flex items-center justify-center"
          [class.w-8]="size() === 'sm'"
          [class.h-8]="size() === 'sm'"
          [class.w-9]="size() === 'md'"
          [class.h-9]="size() === 'md'"
          [class]="iconClass()"
        >
          <lucide-icon
            [name]="icon()"
            [class.w-4]="size() === 'sm'"
            [class.h-4]="size() === 'sm'"
            [class.w-4.5]="size() === 'md'"
            [class.h-4.5]="size() === 'md'"
          ></lucide-icon>
        </div>
        <div class="min-w-0">
          <h4
            class="font-sans font-semibold text-ink-900 truncate"
            [class.text-base]="size() === 'md'"
            [class.text-sm]="size() === 'sm'"
          >
            {{ title() }}
          </h4>
          @if (subtitle()) {
            <p class="font-sans text-xs text-ink-500">
              {{ subtitle() }}
            </p>
          }
        </div>
      </div>

      <ng-content />
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
        min-width: 0;
      }
    `,
  ],
})
export class CategoryPanelHeadingComponent {
  readonly title = input.required<string>();
  readonly icon = input.required<string>();
  readonly iconClass = input.required<string>();
  readonly subtitle = input<string | null>(null);
  readonly size = input<'sm' | 'md'>('sm');
}
