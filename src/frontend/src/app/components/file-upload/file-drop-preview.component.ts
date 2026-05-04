import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
} from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-file-drop-preview',
  standalone: true,
  imports: [LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="relative">
      <div class="flex items-center justify-center gap-4">
        <div
          class="w-14 h-14 rounded-xl bg-gradient-to-br from-academic-blue/10 to-academic-blue/5 flex items-center justify-center border border-academic-blue/20"
        >
          <lucide-icon
            name="file-text"
            class="w-7 h-7 text-academic-blue"
          ></lucide-icon>
        </div>
        <div class="text-left">
          <p class="font-body text-ink-900 font-medium truncate max-w-xs">
            {{ file().name }}
          </p>
          <p class="font-mono text-xs text-ink-500 mt-0.5">
            {{ formatFileSize(file().size) }}
          </p>
        </div>
        <button
          type="button"
          class="ml-auto p-2 rounded-lg hover:bg-academic-red/10 text-ink-400 hover:text-academic-red transition-colors"
          (click)="fileCleared.emit($event)"
        >
          <lucide-icon name="x" class="w-5 h-5"></lucide-icon>
        </button>
      </div>
    </div>
  `,
})
export class FileDropPreviewComponent {
  readonly file = input.required<File>();
  readonly fileCleared = output<Event>();

  formatFileSize(bytes: number): string {
    if (bytes === 0) {
      return '0 Bytes';
    }

    const unitBase = 1024;
    const units = ['Bytes', 'KB', 'MB', 'GB'];
    const unitIndex = Math.min(
      Math.floor(Math.log(bytes) / Math.log(unitBase)),
      units.length - 1,
    );

    return `${parseFloat(
      (bytes / Math.pow(unitBase, unitIndex)).toFixed(2),
    )} ${units[unitIndex]}`;
  }
}
