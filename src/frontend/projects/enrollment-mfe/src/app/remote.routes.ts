import { Routes } from '@angular/router';
import { Checkout } from './checkout';
import { MyEnrollments } from './my-enrollments';

export const ENROLLMENT_ROUTES: Routes = [
  { path: '', component: MyEnrollments },
  { path: ':courseId', component: Checkout },
];
