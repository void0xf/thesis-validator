import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import {
  LUCIDE_ICONS,
  LucideIconProvider,
  Loader2,
  GraduationCap,
  BookOpen,
  FileCheck,
  Upload,
  FileText,
  X,
  AlertCircle,
  CheckCircle2,
  Check,
  CheckCheck,
  Type,
  Layout,
  ListTree,
  SpellCheck,
  ChevronDown,
  Info,
  AlertTriangle,
  XCircle,
  FileDown,
  MapPin,
  RotateCcw,
  Bookmark,
  Settings,
} from 'lucide-angular';

const icons = {
  Loader2,
  GraduationCap,
  BookOpen,
  FileCheck,
  Upload,
  FileText,
  X,
  AlertCircle,
  CheckCircle2,
  Check,
  CheckCheck,
  Type,
  Layout,
  ListTree,
  SpellCheck,
  ChevronDown,
  Info,
  AlertTriangle,
  XCircle,
  FileDown,
  MapPin,
  RotateCcw,
  Bookmark,
  Settings,
};

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true, runCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withFetch()),
    {
      provide: LUCIDE_ICONS,
      useValue: new LucideIconProvider(icons),
    },
  ],
};
