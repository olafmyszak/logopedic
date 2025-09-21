import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login/login/login.component';
import { CalendarComponent } from './features/calendar/calendar/calendar.component';

export const routes: Routes = [
    {
        path: 'login',
        component: LoginComponent
    },
    {
        path: 'calendar',
        component: CalendarComponent
    }
];
