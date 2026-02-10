import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule, GraduationCap, BookOpen, FileCheck } from 'lucide-angular';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  template: `
    <header class="relative overflow-hidden">
      <!-- Decorative background elements -->
      <div class="absolute inset-0 pointer-events-none">
        <div class="absolute -top-24 -left-24 w-96 h-96 bg-academic-gold/5 rounded-full blur-3xl"></div>
        <div class="absolute -top-12 -right-32 w-80 h-80 bg-academic-burgundy/5 rounded-full blur-3xl"></div>
      </div>

      <div class="relative max-w-5xl mx-auto px-6 py-12 md:py-16">
        <!-- Logo and title section -->
        <div class="flex items-center gap-4 mb-4">
          <div class="flex items-center justify-center w-14 h-14 rounded-xl bg-gradient-to-br from-academic-burgundy to-academic-red shadow-lg">
            <lucide-icon name="graduation-cap" class="w-7 h-7 text-white"></lucide-icon>
          </div>
          <div>
            <h1 class="font-display text-3xl md:text-4xl font-bold text-ink-950 tracking-tight">
              Thesis Validator
            </h1>
            <p class="font-sans text-sm text-ink-500 tracking-wide uppercase">
              Academic Document Verification System
            </p>
          </div>
        </div>

        <!-- Tagline -->
        <p class="max-w-xl font-body text-lg text-ink-600 leading-relaxed mt-6">
          Ensure your thesis meets all university formatting requirements before submission.
          Upload your document and receive detailed feedback instantly.
        </p>

        <!-- Feature pills -->
        <div class="flex flex-wrap gap-3 mt-6">
          <div class="inline-flex items-center gap-2 px-4 py-2 rounded-full bg-white/70 border border-parchment-300/60 shadow-sm">
            <lucide-icon name="file-check" class="w-4 h-4 text-academic-green"></lucide-icon>
            <span class="font-sans text-sm text-ink-700">Format Verification</span>
          </div>
          <div class="inline-flex items-center gap-2 px-4 py-2 rounded-full bg-white/70 border border-parchment-300/60 shadow-sm">
            <lucide-icon name="book-open" class="w-4 h-4 text-academic-blue"></lucide-icon>
            <span class="font-sans text-sm text-ink-700">Style Guidelines</span>
          </div>
        </div>
      </div>

      <!-- Bottom border decoration -->
      <div class="absolute bottom-0 left-0 right-0 h-px bg-gradient-to-r from-transparent via-parchment-400/50 to-transparent"></div>
    </header>
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class HeaderComponent {}
