import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-file-drop-empty',
  standalone: true,
  imports: [LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="relative">
      <div class="flex justify-center mb-4">
        <div
          class="w-16 h-16 rounded-2xl bg-gradient-to-br from-parchment-200 to-parchment-300 flex items-center justify-center group-hover:scale-105 transition-transform duration-300 shadow-sm"
        >
          <lucide-icon
            name="upload"
            class="w-8 h-8 text-ink-500 group-hover:text-academic-burgundy transition-colors duration-300"
          ></lucide-icon>
        </div>
      </div>
      <p class="font-body text-lg text-ink-700 mb-2">
        Drag & drop your thesis here
      </p>
      <p class="font-sans text-sm text-ink-500">
        or
        <span
          class="text-academic-burgundy font-medium underline underline-offset-2"
          >browse files</span
        >
      </p>
      <p class="font-mono text-xs text-ink-400 mt-4">
        Accepted format: .docx (Microsoft Word)
      </p>
    </div>
  `,
})
export class FileDropEmptyComponent {}
