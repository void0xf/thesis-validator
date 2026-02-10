import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideHttpClient, withFetch } from '@angular/common/http';
import {
  LUCIDE_ICONS, LucideIconProvider,
  Loader2, GraduationCap, BookOpen, FileCheck, Upload, FileText, X,
  AlertCircle, CheckCircle2, Check, CheckCheck, Type, Layout, ListTree,
  SpellCheck, ChevronDown, Info, AlertTriangle, XCircle, FileDown,
  MapPin, RotateCcw
} from 'lucide-angular';

const icons = {
  Loader2, GraduationCap, BookOpen, FileCheck, Upload, FileText, X,
  AlertCircle, CheckCircle2, Check, CheckCheck, Type, Layout, ListTree,
  SpellCheck, ChevronDown, Info, AlertTriangle, XCircle, FileDown,
  MapPin, RotateCcw
};

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideHttpClient(withFetch()),
    {
      provide: LUCIDE_ICONS,
      useValue: new LucideIconProvider(icons)
    }
  ]
};
