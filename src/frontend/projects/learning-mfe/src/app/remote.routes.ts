import { Routes } from '@angular/router';
import { Attendance } from './attendance';
import { CoursePass } from './course-pass';
import { CoursePlayer } from './course-player';
import { Inbox } from './inbox';

export const LEARNING_ROUTES: Routes = [
  { path: '', component: CoursePass },
  { path: 'inbox', component: Inbox },
  { path: 'attendance', component: Attendance },
  { path: 'course/:courseId', component: CoursePlayer },
  { path: 'course/:courseId/:lectureId', component: CoursePlayer },
];
