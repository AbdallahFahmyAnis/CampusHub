import { Routes } from '@angular/router';
import { teacherGuard } from '../../../shell/src/app/auth.guard';
import { CourseAnalytics } from './course-analytics';
import { CourseDetail } from './course-detail';
import { CourseEditor } from './course-editor';
import { CourseGradebook } from './course-gradebook';
import { CourseList } from './course-list';
import { TeacherCourses } from './teacher-courses';

export const CATALOG_ROUTES: Routes = [
  { path: '', component: CourseList },
  { path: 'mine', component: TeacherCourses, canActivate: [teacherGuard] },
  { path: 'new', component: CourseEditor, canActivate: [teacherGuard] },
  { path: ':id/analytics', component: CourseAnalytics, canActivate: [teacherGuard] },
  { path: ':id/gradebook', component: CourseGradebook, canActivate: [teacherGuard] },
  { path: ':id/edit', component: CourseEditor, canActivate: [teacherGuard] },
  { path: ':id', component: CourseDetail },
];
