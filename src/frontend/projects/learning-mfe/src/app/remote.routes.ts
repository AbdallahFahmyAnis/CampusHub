import { Routes } from '@angular/router';
import { Attendance } from './attendance';
import { Certificates } from './certificates';
import { CoursePass } from './course-pass';
import { CoursePlayer } from './course-player';
import { Inbox } from './inbox';
import { ProgressDashboard } from './progress-dashboard';

export const LEARNING_ROUTES: Routes = [
  { path: '', component: ProgressDashboard },
  { path: 'pass', component: CoursePass },
  { path: 'inbox', component: Inbox },
  { path: 'certificates', component: Certificates },
  { path: 'attendance', component: Attendance },
  { path: 'course/:courseId', component: CoursePlayer },
  { path: 'course/:courseId/:lectureId', component: CoursePlayer },
];
