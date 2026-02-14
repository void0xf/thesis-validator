import { Component, EventEmitter, Output, signal, computed, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule, Upload, FileText, X, AlertCircle, CheckCircle2 } from 'lucide-angular';

@Component({
  selector: 'app-file-upload',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  template: `
    <div class="paper-card p-6 animate-slide-up">
      <div class="flex items-center justify-between mb-5">
        <h2 class="font-display text-xl font-semibold text-ink-900">
          Upload Document
        </h2>
        @if (selectedFile()) {
          <span class="badge-success">
            <lucide-icon name="check-circle-2" class="w-3.5 h-3.5 mr-1.5"></lucide-icon>
            Ready
          </span>
        }
      </div>

      <!-- Drop zone -->
      <div
        class="relative group"
        (dragover)="onDragOver($event)"
        (dragleave)="onDragLeave($event)"
        (drop)="onDrop($event)"
      >
        <input
          type="file"
          #fileInput
          id="file-input"
          accept=".docx"
          class="absolute inset-0 w-full h-full opacity-0 cursor-pointer z-10"
          [disabled]="selectedFile() !== null"
          [class.pointer-events-none]="selectedFile() !== null"
          (change)="onFileSelected($event)"
        />

        <div
          class="relative border-2 border-dashed rounded-xl p-8 md:p-12 text-center transition-all duration-300"
          [class]="dropZoneClasses()"
        >
          <!-- Background pattern -->
          <div class="absolute inset-0 opacity-30 rounded-xl overflow-hidden pointer-events-none">
            <div class="absolute inset-0" style="
              background-image: repeating-linear-gradient(
                45deg,
                transparent,
                transparent 10px,
                currentColor 10px,
                currentColor 11px
              );
              opacity: 0.03;
            "></div>
          </div>

          @if (!selectedFile()) {
            <!-- Empty state -->
            <div class="relative">
              <div class="flex justify-center mb-4">
                <div class="w-16 h-16 rounded-2xl bg-gradient-to-br from-parchment-200 to-parchment-300 flex items-center justify-center group-hover:scale-105 transition-transform duration-300 shadow-sm">
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
                or <span class="text-academic-burgundy font-medium underline underline-offset-2">browse files</span>
              </p>
              <p class="font-mono text-xs text-ink-400 mt-4">
                Accepted format: .docx (Microsoft Word)
              </p>
            </div>
          } @else {
            <!-- File selected state -->
            <div class="relative">
              <div class="flex items-center justify-center gap-4">
                <div class="w-14 h-14 rounded-xl bg-gradient-to-br from-academic-blue/10 to-academic-blue/5 flex items-center justify-center border border-academic-blue/20">
                  <lucide-icon name="file-text" class="w-7 h-7 text-academic-blue"></lucide-icon>
                </div>
                <div class="text-left">
                  <p class="font-body text-ink-900 font-medium truncate max-w-xs">
                    {{ selectedFile()?.name }}
                  </p>
                  <p class="font-mono text-xs text-ink-500 mt-0.5">
                    {{ formatFileSize(selectedFile()?.size || 0) }}
                  </p>
                </div>
                <button
                  type="button"
                  class="ml-auto p-2 rounded-lg hover:bg-academic-red/10 text-ink-400 hover:text-academic-red transition-colors"
                  (click)="clearFile($event)"
                >
                  <lucide-icon name="x" class="w-5 h-5"></lucide-icon>
                </button>
              </div>
            </div>
          }
        </div>

        <!-- Error message -->
        @if (errorMessage()) {
          <div class="flex items-center gap-2 mt-3 px-3 py-2 rounded-lg bg-academic-red/5 border border-academic-red/10">
            <lucide-icon name="alert-circle" class="w-4 h-4 text-academic-red flex-shrink-0"></lucide-icon>
            <p class="font-sans text-sm text-academic-red">{{ errorMessage() }}</p>
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class FileUploadComponent {
  @Output() fileChange = new EventEmitter<File | null>();
  @ViewChild('fileInput') private fileInput?: ElementRef<HTMLInputElement>;

  selectedFile = signal<File | null>(null);
  isDragOver = signal(false);
  errorMessage = signal<string | null>(null);

  dropZoneClasses = computed(() => {
    const base = 'border-parchment-400/60 bg-parchment-50/50';
    const hover = 'group-hover:border-academic-burgundy/40 group-hover:bg-parchment-100/50';
    const dragOver = this.isDragOver()
      ? 'border-academic-burgundy bg-academic-burgundy/5 scale-[1.01]'
      : '';
    const hasFile = this.selectedFile()
      ? 'border-academic-blue/30 bg-academic-blue/5'
      : '';

    return `${base} ${hover} ${dragOver} ${hasFile}`;
  });

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(true);
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver.set(false);

    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.processFile(files[0]);
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.processFile(input.files[0]);
    }
  }

  private processFile(file: File): void {
    this.errorMessage.set(null);

    if (!file.name.toLowerCase().endsWith('.docx')) {
      this.errorMessage.set('Please upload a .docx file (Microsoft Word document)');
      this.selectedFile.set(null);
      this.fileChange.emit(null);
      return;
    }

    this.selectedFile.set(file);
    this.fileChange.emit(file);
  }

  clearFile(event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.selectedFile.set(null);
    this.errorMessage.set(null);
    if (this.fileInput?.nativeElement) {
      this.fileInput.nativeElement.value = '';
    }
    this.fileChange.emit(null);
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }
}
